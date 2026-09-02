using System.Threading.Tasks;

namespace CRManager.Shared.Services;

public class InMemoryTokenStorage : ITokenStorage
{
    private string? _accessToken;
    private string? _refreshToken;

    public Task<string?> GetTokenAsync() => Task.FromResult(_accessToken);

    public Task<string?> GetRefreshTokenAsync() => Task.FromResult(_refreshToken);

    public Task SetTokensAsync(string accessToken, string? refreshToken = null)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        return Task.CompletedTask;
    }
}
