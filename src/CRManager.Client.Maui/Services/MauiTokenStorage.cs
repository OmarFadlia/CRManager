using System.Threading.Tasks;
using CRManager.Shared.Services;
using Microsoft.Maui.Storage;

namespace CRManager.Client.Maui.Services;

public class MauiTokenStorage : ITokenStorage
{
    private const string AccessTokenKey = "crmanager_auth_access_token";
    private const string RefreshTokenKey = "crmanager_auth_refresh_token";

    public Task<string?> GetTokenAsync()
    {
        var token = Preferences.Default.Get<string?>(AccessTokenKey, null);
        return Task.FromResult(token);
    }

    public Task<string?> GetRefreshTokenAsync()
    {
        var token = Preferences.Default.Get<string?>(RefreshTokenKey, null);
        return Task.FromResult(token);
    }

    public Task SetTokensAsync(string accessToken, string? refreshToken = null)
    {
        Preferences.Default.Set(AccessTokenKey, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            Preferences.Default.Set(RefreshTokenKey, refreshToken);
        }
        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        Preferences.Default.Remove(AccessTokenKey);
        Preferences.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}
