using System;
using System.Threading.Tasks;

namespace CRManager.Shared.Services;

public interface IApiEndpointProvider
{
    event Action? OnEndpointChanged;
    string GetBaseUrl();
    Task SetBaseUrlAsync(string url);
}

public class DefaultApiEndpointProvider : IApiEndpointProvider
{
    private string _currentUrl;
    public event Action? OnEndpointChanged;

    public DefaultApiEndpointProvider(string defaultUrl = null)
    {
        _currentUrl = EnsureTrailingSlash(defaultUrl ?? ApiConstants.HostedApiUrl);
    }

    public string GetBaseUrl() => _currentUrl;

    public Task SetBaseUrlAsync(string url)
    {
        _currentUrl = EnsureTrailingSlash(url);
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
