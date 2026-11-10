namespace Duett.API.Models;

public class ProfilePhotoFile
{
    public Stream Content { get; init; } = Stream.Null;

    public string ContentType { get; init; } = string.Empty;
}