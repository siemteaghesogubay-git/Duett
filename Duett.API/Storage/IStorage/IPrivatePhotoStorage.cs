namespace Duett.API.Storage.IStorage;

public interface IPrivatePhotoStorage
{
    // Sparar en redan validerad bild.
    // Returnerar en slumpmässig intern lagringsnyckel.
    Task<string> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default);

    // Returnerar null om filen saknas.
    // Anroparen ansvarar för att stänga strömmen.
    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    // Att ta bort en fil som redan saknas ska vara tillåtet.
    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}