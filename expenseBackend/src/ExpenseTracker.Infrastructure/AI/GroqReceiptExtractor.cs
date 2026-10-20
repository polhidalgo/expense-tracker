using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.AI;

// Sends the image to Groq and reads the fields it found. Never logs the image, the key or the answer.
public sealed class GroqReceiptExtractor(
    HttpClient http,
    SecretStore secrets,
    IConfiguration configuration,
    ILogger<GroqReceiptExtractor> logger)
{
    private static readonly string Prompt =
        "Extract the expense from this receipt or payment screenshot. " +
        "Reply with only a JSON object with these keys: " +
        "merchant (string), amount (number, the total paid, as a positive number even if shown with a minus sign), " +
        "currency (ISO 4217 code), " +
        "date (YYYY-MM-DD), category (one of: " + string.Join(", ", CategorySeed.All.Select(c => c.Name)) + "). " +
        "Use null for any value that is not clearly visible. Do not guess.";

    public async Task<RawReceipt> ExtractAsync(byte[] image, string contentType, CancellationToken cancellationToken)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);

        var body = new
        {
            model = configuration["Groq:Model"] ?? throw new InvalidOperationException("Groq:Model is not configured."),
            temperature = 0,
            response_format = new { type = "json_object" },
            reasoning_format = "hidden",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = Prompt },
                        new { type = "image_url", image_url = new { url = $"data:{contentType};base64,{Convert.ToBase64String(image)}" } }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw Fail(ReceiptFailure.Unavailable, "network error");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw Fail(ReceiptFailure.Unavailable, "timeout");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw Fail(ToFailure(response.StatusCode), $"HTTP {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ReadAnswer(ReadContent(json));
        }
    }

    // Reads the JSON the model wrote. Fields that are missing or of the wrong type stay null.
    public static RawReceipt ReadAnswer(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new ReceiptExtractionException(ReceiptFailure.InvalidResponse);

            return new RawReceipt(
                Text(root, "merchant"),
                Amount(root),
                Text(root, "currency"),
                Text(root, "date"),
                Text(root, "category"));
        }
        catch (JsonException)
        {
            throw new ReceiptExtractionException(ReceiptFailure.InvalidResponse);
        }
    }

    public static ReceiptFailure ToFailure(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests ? ReceiptFailure.RateLimited : ReceiptFailure.Unavailable;

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        string? key;
        try
        {
            key = await secrets.GetAsync(SecretStore.GroqApiKey, cancellationToken);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            throw Fail(ReceiptFailure.Unavailable, "Groq key cannot be decrypted");
        }

        return string.IsNullOrEmpty(key) ? throw Fail(ReceiptFailure.Unavailable, "Groq key not configured") : key;
    }

    // Groq's response wraps the model's text in choices[0].message.content.
    private static string ReadContent(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? throw new ReceiptExtractionException(ReceiptFailure.InvalidResponse);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new ReceiptExtractionException(ReceiptFailure.InvalidResponse);
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? Amount(JsonElement root)
    {
        if (!root.TryGetProperty("amount", out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    private ReceiptExtractionException Fail(ReceiptFailure reason, string detail)
    {
        logger.LogWarning("Groq request failed: {Reason} ({Detail})", reason, detail);
        return new ReceiptExtractionException(reason);
    }
}
