using System.ComponentModel.DataAnnotations;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace Duett.API.Services;

public class ProfilePhotoProcessor : IProfilePhotoProcessor
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const int MaxInputDimension = 6000;
    private const long MaxInputPixels = 12_000_000;
    private const int MaxOutputDimension = 1200;

    public async Task<ProcessedProfilePhoto> ProcessAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ValidationException(
                "Välj en bildfil som inte är tom.");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new ValidationException(
                "Bilden får vara högst 5 MB.");
        }

        // Kontrollera även antalet byte som faktiskt läses.
        using var buffer = new MemoryStream();
        await using var input = file.OpenReadStream();

        var chunk = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await input.ReadAsync(
                chunk.AsMemory(),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;

            if (totalBytes > MaxFileBytes)
            {
                throw new ValidationException(
                    "Bilden får vara högst 5 MB.");
            }

            await buffer.WriteAsync(
                chunk.AsMemory(0, bytesRead),
                cancellationToken);
        }

        if (totalBytes == 0)
        {
            throw new ValidationException(
                "Välj en bildfil som inte är tom.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var data = SKData.CreateCopy(buffer.ToArray());
        using var codec = SKCodec.Create(data);

        if (codec is null)
        {
            throw new ValidationException(
                "Filen kunde inte läsas som en bild.");
        }

        // Kontrollera innehållet, inte filnamnet eller ContentType.
        if (codec.EncodedFormat != SKEncodedImageFormat.Jpeg &&
            codec.EncodedFormat != SKEncodedImageFormat.Png)
        {
            throw new ValidationException(
                "Endast JPEG och PNG är tillåtna.");
        }

        if (codec.FrameCount > 1)
        {
            throw new ValidationException(
                "Animerade bilder är inte tillåtna.");
        }

        var width = codec.Info.Width;
        var height = codec.Info.Height;

        if (width <= 0 ||
            height <= 0 ||
            width > MaxInputDimension ||
            height > MaxInputDimension ||
            (long)width * height > MaxInputPixels)
        {
            throw new ValidationException(
                "Bilden får vara högst 6 000 pixlar per sida " +
                "och innehålla högst 12 miljoner pixlar.");
        }

        // Avkoda till ett bestämt pixelformat och färgrymd.
        using var colorSpace = SKColorSpace.CreateSrgb();

        var decodeInfo = new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            colorSpace);

        using var decoded = new SKBitmap(decodeInfo);

        if (decoded.GetPixels() == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Minne för bildbearbetningen kunde inte reserveras.");
        }

        var decodeResult = codec.GetPixels(
            decodeInfo,
            decoded.GetPixels());

        if (decodeResult != SKCodecResult.Success)
        {
            throw new ValidationException(
                "Bilden är skadad eller kunde inte läsas fullständigt.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var origin = codec.EncodedOrigin;

        var swapsDimensions =
            origin == SKEncodedOrigin.LeftTop ||
            origin == SKEncodedOrigin.RightTop ||
            origin == SKEncodedOrigin.RightBottom ||
            origin == SKEncodedOrigin.LeftBottom;

        var orientedWidth = swapsDimensions ? height : width;
        var orientedHeight = swapsDimensions ? width : height;

        var scale = Math.Min(
            1.0,
            (double)MaxOutputDimension /
            Math.Max(orientedWidth, orientedHeight));

        var outputWidth = Math.Max(
            1,
            (int)Math.Round(orientedWidth * scale));

        var outputHeight = Math.Max(
            1,
            (int)Math.Round(orientedHeight * scale));

        var outputInfo = new SKImageInfo(
            outputWidth,
            outputHeight,
            SKColorType.Rgba8888,
            SKAlphaType.Opaque,
            colorSpace);

        using var output = new SKBitmap(outputInfo);

        if (output.GetPixels() == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Minne för bildbearbetningen kunde inte reserveras.");
        }

        using (var canvas = new SKCanvas(output))
        {
            // Transparenta delar av PNG-bilder får vit bakgrund.
            canvas.Clear(SKColors.White);

            canvas.Scale(
                (float)outputWidth / orientedWidth,
                (float)outputHeight / orientedHeight);

            ApplyOrientation(canvas, origin, width, height);

            using var sourceImage = SKImage.FromBitmap(decoded);

            canvas.DrawImage(
                sourceImage,
                new SKRect(0, 0, width, height),
                new SKSamplingOptions(SKFilterMode.Linear));

            canvas.Flush();
        }

        cancellationToken.ThrowIfCancellationRequested();

        // En ny bild skapas från pixlarna.
        // Originalets EXIF-, GPS- och textmetadata kopieras inte.
        using var outputImage = SKImage.FromBitmap(output);
        using var encoded = outputImage.Encode(
            SKEncodedImageFormat.Jpeg,
            85);

        if (encoded is null)
        {
            throw new InvalidOperationException(
                "Den bearbetade bilden kunde inte skapas.");
        }

        if (encoded.Size > MaxFileBytes)
        {
            throw new ValidationException(
                "Den bearbetade bilden är för stor.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new ProcessedProfilePhoto
        {
            Content = encoded.ToArray(),
            ContentType = "image/jpeg"
        };
    }

    private static void ApplyOrientation(
        SKCanvas canvas,
        SKEncodedOrigin origin,
        int width,
        int height)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopLeft:
                break;

            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;

            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;

            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;

            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;

            case SKEncodedOrigin.RightTop:
                canvas.Translate(height, 0);
                canvas.RotateDegrees(90);
                break;

            case SKEncodedOrigin.RightBottom:
                canvas.Translate(height, width);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;

            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, width);
                canvas.RotateDegrees(270);
                break;

            default:
                throw new ValidationException(
                    "Bildens orientering stöds inte.");
        }
    }
}