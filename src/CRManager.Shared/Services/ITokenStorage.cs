using System.Threading.Tasks;

namespace CRManager.Shared.Services;

public interface ITokenStorage
{
    Task<string?> GetTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string? refreshToken = null);
    Task ClearTokensAsync();
}
