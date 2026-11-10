using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Duett.API.Dtos;

public class UploadProfilePhotoDto
{
    [Required(ErrorMessage = "Välj en profilbild.")]
    public IFormFile? File { get; set; }
}

// profile photo response dto

public class ProfilePhotoResponseDto
{
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; }
}