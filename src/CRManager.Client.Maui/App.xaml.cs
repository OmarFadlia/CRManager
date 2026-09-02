using CRManager.Client.Maui.Services;

namespace CRManager.Client.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Automatically launch API child process if not already running
        //BackendProcessManager.StartBackendApiIfRequired();

        MainPage = new MainPage();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "CRManager";
        window.Width = 1360;
        window.Height = 880;

        window.Destroying += (s, e) =>
        {
            //BackendProcessManager.StopBackendApi();
        };

        return window;
    }
}
