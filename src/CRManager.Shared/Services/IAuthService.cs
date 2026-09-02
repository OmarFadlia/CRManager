using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRManager.Shared.DTOs;

namespace CRManager.Shared.Services;

public interface IAuthService
{
    event Action? OnAuthStateChanged;
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetCurrentUserEmailAsync();
    Task<bool> TryRefreshTokenAsync();
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password);
    Task LogoutAsync();
}

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthResult Success() => new() { Succeeded = true };
    public static AuthResult Failed(string error) => new() { Succeeded = false, ErrorMessage = error };
}

public class AuthService(HttpClient httpClient, ITokenStorage tokenStorage) : IAuthService
{
    public event Action? OnAuthStateChanged;

    private string? _cachedUserEmail;

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await tokenStorage.GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetCurrentUserEmailAsync()
    {
        if (!await IsAuthenticatedAsync())
            return null;

        if (!string.IsNullOrEmpty(_cachedUserEmail))
            return _cachedUserEmail;

        try
        {
            var response = await httpClient.GetAsync("manage/info");
            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoResponseDto>();
                _cachedUserEmail = userInfo?.Email;
                return _cachedUserEmail;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshed = await TryRefreshTokenAsync();
                if (refreshed)
                {
                    var retryResponse = await httpClient.GetAsync("manage/info");
                    if (retryResponse.IsSuccessStatusCode)
                    {
                        var userInfo = await retryResponse.Content.ReadFromJsonAsync<UserInfoResponseDto>();
                        _cachedUserEmail = userInfo?.Email;
                        return _cachedUserEmail;
                    }
                }
                await LogoutAsync();
                return null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
        {
            await LogoutAsync();
            return false;
        }

        try
        {
            var response = await httpClient.PostAsJsonAsync("refresh", new RefreshRequestDto
            {
                RefreshToken = refreshToken
            });

            if (response.IsSuccessStatusCode)
            {
                var tokenData = await response.Content.ReadFromJsonAsync<AccessTokenResponseDto>();
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    await tokenStorage.SetTokensAsync(tokenData.AccessToken, tokenData.RefreshToken);
                    return true;
                }
            }

            await LogoutAsync();
            return false;
        }
        catch
        {
            await LogoutAsync();
            return false;
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("login", new LoginRequestDto
            {
                Email = email,
                Password = password
            });

            if (response.IsSuccessStatusCode)
            {
                var tokenData = await response.Content.ReadFromJsonAsync<AccessTokenResponseDto>();
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    await tokenStorage.SetTokensAsync(tokenData.AccessToken, tokenData.RefreshToken);
                    _cachedUserEmail = email;
                    OnAuthStateChanged?.Invoke();
                    return AuthResult.Success();
                }
            }

            var errorDetails = await response.Content.ReadAsStringAsync();
            return AuthResult.Failed(string.IsNullOrWhiteSpace(errorDetails) ? "Invalid email or password." : "Login failed.");
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("register", new RegisterRequestDto
            {
                Email = email,
                Password = password
            });

            if (response.IsSuccessStatusCode)
            {
                // Auto login after successful registration
                return await LoginAsync(email, password);
            }

            var errorDetails = await response.Content.ReadAsStringAsync();
            return AuthResult.Failed(string.IsNullOrWhiteSpace(errorDetails) ? "Registration failed." : errorDetails);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"Network error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await tokenStorage.ClearTokensAsync();
        _cachedUserEmail = null;
        OnAuthStateChanged?.Invoke();
    }
}
