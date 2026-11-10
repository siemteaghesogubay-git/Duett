namespace Duett.API.Models;

public class ProcessedProfilePhoto
{
    public byte[] Content { get; init; } = Array.Empty<byte>();

    public string ContentType { get; init; } = "image/jpeg";

    public long SizeBytes => Content.LongLength;
}