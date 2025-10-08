using Blazored.LocalStorage;
using FileManager.Maui.Services;
using FileManager.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FileManager.Maui;

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

        builder.Services.AddScoped(sp => new HttpClient 
        { 
            BaseAddress = new Uri("http://127.0.0.1:5001")  
        });

        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<ApiService>();
        builder.Services.AddScoped<IFolderSyncService, MauiFolderSyncService>();

        return builder.Build();
    }
}