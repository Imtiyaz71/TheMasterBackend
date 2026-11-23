using System.Threading.Tasks;
using TheMaster.Application.DTOs;

namespace TheMaster.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> GetProfileAsync(int userId, string? userEmail = null);
    }
}
