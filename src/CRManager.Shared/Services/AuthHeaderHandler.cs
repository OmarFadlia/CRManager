using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CRManager.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace CRManager.Shared.Services;

public class AuthHeaderHandler(
    ITokenStorage tokenStorage, 
    IApiEndpointProvider endpointProvider, 
    IServiceProvider serviceProvider) : DelegatingHandler
{
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sentToken = await tokenStorage.GetTokenAsync();
        if (!string.IsNullOrEmpty(sentToken) && request.Headers.Authorization == null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sentToken);
        }

        // Dynamically point request to current API endpoint base URL
        var baseUrl = new Uri(endpointProvider.GetBaseUrl());
        if (request.RequestUri == null)
        {
            request.RequestUri = baseUrl;
        }
        else if (!request.RequestUri.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(baseUrl, request.RequestUri.ToString().TrimStart('/'));
        }
        else
        {
            var relativePath = request.RequestUri.PathAndQuery;
            request.RequestUri = new Uri(baseUrl, relativePath.TrimStart('/'));
        }

        // If request has content, buffer it into memory so it can be cloned and resent on retry if needed
        byte[]? contentBytes = null;
        MediaTypeHeaderValue? contentType = null;
        if (request.Content != null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType;
            request.Content = new ByteArrayContent(contentBytes);
            if (contentType != null)
            {
                request.Content.Headers.ContentType = contentType;
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !IsAuthEndpoint(request.RequestUri))
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                var currentToken = await tokenStorage.GetTokenAsync();

                // If another thread already refreshed the token, retry with that new token
                if (!string.IsNullOrEmpty(currentToken) && currentToken != sentToken)
                {
                    var retryRequest = CloneHttpRequestMessage(request, currentToken, contentBytes, contentType);
                    response.Dispose();
                    return await base.SendAsync(retryRequest, cancellationToken);
                }

                // Attempt to refresh the token using refresh token
                var refreshToken = await tokenStorage.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshUri = new Uri(baseUrl, "refresh");
                    var refreshMessage = new HttpRequestMessage(HttpMethod.Post, refreshUri)
                    {
                        Content = JsonContent.Create(new RefreshRequestDto { RefreshToken = refreshToken })
                    };

                    var refreshResponse = await base.SendAsync(refreshMessage, cancellationToken);
                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var tokenData = await refreshResponse.Content.ReadFromJsonAsync<AccessTokenResponseDto>(cancellationToken: cancellationToken);
                        if (tokenData != null && !string.IsNullOrEmpty(tokenData.AccessToken))
                        {
                            await tokenStorage.SetTokensAsync(tokenData.AccessToken, tokenData.RefreshToken);

                            var retryRequest = CloneHttpRequestMessage(request, tokenData.AccessToken, contentBytes, contentType);
                            response.Dispose();
                            return await base.SendAsync(retryRequest, cancellationToken);
                        }
                    }
                }

                // Refresh failed (or no refresh token found)
                await tokenStorage.ClearTokensAsync();
                try
                {
                    var authService = serviceProvider.GetService<IAuthService>();
                    if (authService != null)
                    {
                        await authService.LogoutAsync();
                    }
                }
                catch { }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return response;
    }

    private static bool IsAuthEndpoint(Uri? uri)
    {
        if (uri == null) return false;
        var path = uri.AbsolutePath.ToLowerInvariant();
        return path.EndsWith("/login") || path.EndsWith("/register") || path.EndsWith("/refresh");
    }

    private static HttpRequestMessage CloneHttpRequestMessage(
        HttpRequestMessage req, 
        string? newBearerToken, 
        byte[]? contentBytes, 
        MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri)
        {
            Version = req.Version,
            VersionPolicy = req.VersionPolicy
        };

        if (contentBytes != null)
        {
            clone.Content = new ByteArrayContent(contentBytes);
            if (contentType != null)
            {
                clone.Content.Headers.ContentType = contentType;
            }
        }

        foreach (var header in req.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(newBearerToken))
            {
                continue;
            }
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!string.IsNullOrEmpty(newBearerToken))
        {
            clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newBearerToken);
        }

        foreach (var prop in req.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
        }

        return clone;
    }
}
