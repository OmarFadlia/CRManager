using System;
using System.Net.Http;
using CRManager.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRManager.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCRManagerUI(this IServiceCollection services, string apiBaseUrl = null)
    {
        services.TryAddScoped<ITokenStorage, InMemoryTokenStorage>();
        services.TryAddSingleton<IApiEndpointProvider>(_ => new DefaultApiEndpointProvider(apiBaseUrl ?? ApiConstants.HostedApiUrl));
        services.AddScoped<AuthHeaderHandler>();

        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthHeaderHandler>();
            handler.InnerHandler = new HttpClientHandler();

            var endpointProvider = sp.GetRequiredService<IApiEndpointProvider>();
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(endpointProvider.GetBaseUrl()),
                Timeout = TimeSpan.FromSeconds(5)
            };
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICardApiService, CardApiService>();

        return services;
    }
}
