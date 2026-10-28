using Microsoft.Extensions.Logging;
using WeatherApp.Mobile.Services;
using WeatherApp.Mobile.ViewModels;
using WeatherApp.Mobile.Views;

namespace WeatherApp.Mobile;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Rejestracja Serwisów
        builder.Services.AddSingleton<WeatherApiService>();

        // Rejestracja ViewModeli i Widoków
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}