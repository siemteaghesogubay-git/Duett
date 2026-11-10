using Duett.API.Models;
using Microsoft.AspNetCore.Http;

namespace Duett.API.Services.IServices;

public interface IProfilePhotoProcessor
{
    Task<ProcessedProfilePhoto> ProcessAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);
}