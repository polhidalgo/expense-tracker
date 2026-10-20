using System.Text;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Receipts;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.UnitTests.Receipts;

public class ReceiptFileCheckTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];
    private static readonly byte[] WebP = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8];
    private static readonly byte[] Gif = [.. "GIF89a"u8, 0, 0];

    [Fact]
    public void GetImageType_DetectsJpegPngAndWebP()
    {
        Assert.Equal("image/png", ReceiptFileCheck.GetImageType(Png));
        Assert.Equal("image/jpeg", ReceiptFileCheck.GetImageType(Jpeg));
        Assert.Equal("image/webp", ReceiptFileCheck.GetImageType(WebP));
    }

    // The type comes from the file content, so a text file or a GIF is rejected whatever its name or declared type.
    [Fact]
    public void GetImageType_TextOrGif_IsRejected()
    {
        var text = Encoding.UTF8.GetBytes("this is not an image");

        Assert.Contains("file", Assert.Throws<DomainValidationException>(() => ReceiptFileCheck.GetImageType(text)).Errors.Keys);
        Assert.Contains("file", Assert.Throws<DomainValidationException>(() => ReceiptFileCheck.GetImageType(Gif)).Errors.Keys);
        Assert.Throws<DomainValidationException>(() => ReceiptFileCheck.GetImageType([]));
    }

    [Fact]
    public void GetImageType_OneByteOverTheLimit_IsTooLarge()
    {
        var file = new byte[ReceiptFileCheck.MaxBytes + 1];
        Png.CopyTo(file, 0);

        Assert.Throws<FileTooLargeException>(() => ReceiptFileCheck.GetImageType(file));
    }

    [Fact]
    public void GetImageType_ExactlyAtTheLimit_IsAccepted()
    {
        var file = new byte[ReceiptFileCheck.MaxBytes];
        Png.CopyTo(file, 0);

        Assert.Equal("image/png", ReceiptFileCheck.GetImageType(file));
    }
}
