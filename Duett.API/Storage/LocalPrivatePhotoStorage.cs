using Duett.API.Storage.IStorage;
using Duett.API.Storage;
namespace Duett.API.Storage;

public class LocalPrivatePhotoStorage : IPrivatePhotoStorage
{
    private readonly string _directory;

    public LocalPrivatePhotoStorage(IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Privat bildlagring för produktionsmiljön måste konfigureras.");
        }

        _directory = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "PrivatePhotos");

        Directory.CreateDirectory(_directory);
    }

    public async Task<string> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "Bildströmmen måste kunna läsas.",
                nameof(content));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Använd aldrig användarens filnamn som lagringsnyckel.
        var storageKey = Guid.NewGuid().ToString("N");
        var filePath = GetFilePath(storageKey);

        await using var destination = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous);

        try
        {
            await content.CopyToAsync(
                destination,
                cancellationToken);

            await destination.FlushAsync(cancellationToken);

            return storageKey;
        }
        catch
        {
            // Stäng filen innan en ofullständig uppladdning tas bort.
            await destination.DisposeAsync();

            try
            {
                File.Delete(filePath);
            }
            catch (IOException)
            {
                // Bevara det ursprungliga felet.
            }
            catch (UnauthorizedAccessException)
            {
                // Bevara det ursprungliga felet.
            }

            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = GetFilePath(storageKey);

        try
        {
            Stream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options:
                    FileOptions.Asynchronous |
                    FileOptions.SequentialScan);

            return Task.FromResult<Stream?>(stream);
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
        catch (DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = GetFilePath(storageKey);

        // File.Delete accepterar att filen redan saknas.
        File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string GetFilePath(string storageKey)
    {
        // Acceptera endast våra egna nycklar.
        // Sökvägar och användarvalda filnamn är inte tillåtna.
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length != 32 ||
            !Guid.TryParseExact(storageKey, "N", out var id))
        {
            throw new ArgumentException(
                "Ogiltig lagringsnyckel.",
                nameof(storageKey));
        }

        return Path.Combine(_directory, id.ToString("N"));
    }
}