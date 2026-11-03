using Duett.API.Dtos;
using Duett.API.Models;

namespace Duett.API.Services.IServices;

public interface ITokenService
{
    Task<LoginResponseDto> CreateTokenAsync(ApplicationUser user);

}