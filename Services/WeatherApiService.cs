using WeatherApp.Mobile.Models;

namespace WeatherApp.Mobile.Services;

public class WeatherApiService
{
    public async Task<WeatherData> GetCurrentWeatherAsync(string city)
    {
        // Symulacja opóźnienia sieciowego (asynchroniczność)
        await Task.Delay(500);

        // Zwracanie mockowych danych (docelowo strzał HTTP do REST API)
        return new WeatherData
        {
            CityName = city,
            Temperature = 21.5,
            Icon = "☀️",
            ConditionText = "Słonecznie"
        };
    }
}