namespace CRManager.Shared.Services;

/// <summary>
/// Centralized API configuration constants for the CRManager application.
/// Update these values to change API endpoints for all clients (Web and MAUI).
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// The primary hosted API URL.
    /// </summary>
    public const string HostedApiUrl = "https://creditmanager.runasp.net/";
    ///https://creditmanager.runasp.net/
    /// <summary>
    /// The local development API URL.
    /// </summary>
    public const string LocalApiUrl = "http://localhost:5283/";

    /// <summary>
    /// The local development API URL for Android (via adb reverse or Termux).
    /// </summary>
    public const string LocalAndroidApiUrl = "http://127.0.0.1:5283/";
}
