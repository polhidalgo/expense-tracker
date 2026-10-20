using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Application.Receipts;

public static class ReceiptFileCheck
{
    // Groq accepts at most 4 MB per request, and base64 makes the image about a third bigger.
    public const int MaxBytes = 3 * 1024 * 1024;

    // Returns the image type, detected from the first bytes of the file, not from what the client says.
    public static string GetImageType(byte[] file)
    {
        if (file.Length > MaxBytes)
            throw new FileTooLargeException($"The image must be at most {MaxBytes / 1024 / 1024} MB.");

        if (StartsWith(file, 0xFF, 0xD8, 0xFF))
            return "image/jpeg";

        if (StartsWith(file, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            return "image/png";

        // WebP: "RIFF" + 4 bytes of size + "WEBP".
        if (StartsWith(file, 0x52, 0x49, 0x46, 0x46) && file.Length >= 12
            && file[8] == 0x57 && file[9] == 0x45 && file[10] == 0x42 && file[11] == 0x50)
            return "image/webp";

        throw new DomainValidationException(new Dictionary<string, string[]>
        {
            ["file"] = ["The file must be a JPEG, PNG or WebP image."]
        });
    }

    private static bool StartsWith(byte[] file, params byte[] header) =>
        file.Length >= header.Length && file.AsSpan(0, header.Length).SequenceEqual(header);
}
