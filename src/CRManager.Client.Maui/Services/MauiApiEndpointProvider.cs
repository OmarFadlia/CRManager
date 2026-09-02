using System;
using System.Threading.Tasks;
using CRManager.Shared.Services;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace CRManager.Client.Maui.Services;

public class MauiApiEndpointProvider : IApiEndpointProvider
{
    private const string ApiUrlKey = "crmanager_custom_api_url";
    public event Action? OnEndpointChanged;

    public string GetBaseUrl()
    {
        var saved = Preferences.Default.Get<string?>(ApiUrlKey, null);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            return EnsureTrailingSlash(saved);
        }

        // Default: Use hosted API URL. For local development:
        // - Android: 127.0.0.1:5283 (via adb reverse or Termux)
        // - Other: localhost:5283
        // Users can override via SetBaseUrlAsync() to use a custom URL
        return ApiConstants.HostedApiUrl;
    }

    public Task SetBaseUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Preferences.Default.Remove(ApiUrlKey);
        }
        else
        {
            Preferences.Default.Set(ApiUrlKey, EnsureTrailingSlash(url));
        }

        OnEndpointChanged?.Invoke();
        return Task.CompletedTask;
    }

    private static string EnsureTrailingSlash(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ApiConstants.HostedApiUrl;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }
        if (!url.EndsWith("/"))
        {
            url += "/";
        }
        return url;
    }
}
