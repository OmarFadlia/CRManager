using CRManager.Client.Web.Components;
using CRManager.Shared;
using CRManager.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register CRManager UI Services & HttpClient pointing to hosted API
builder.Services.AddCRManagerUI(ApiConstants.HostedApiUrl);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(CRManager.Shared.Pages.DashboardView).Assembly);

app.Run();
