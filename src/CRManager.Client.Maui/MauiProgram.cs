using CRManager.Client.Maui.Services;
using CRManager.Shared;
using CRManager.Shared.Services;
using Microsoft.Extensions.Logging;

namespace CRManager.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Persistent preference token storage for Desktop & Mobile
        builder.Services.AddSingleton<ITokenStorage, MauiTokenStorage>();

        // Dynamic API endpoint provider with user settings and Termux/Emulator/Localhost detection
        builder.Services.AddSingleton<IApiEndpointProvider, MauiApiEndpointProvider>();

        builder.Services.AddCRManagerUI();

        return builder.Build();
    }
}
