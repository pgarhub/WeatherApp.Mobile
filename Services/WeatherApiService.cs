using System.Net.Http.Json;
using WeatherApp.Mobile.Models;

namespace WeatherApp.Mobile.Services;

public class WeatherApiService
{
    private readonly HttpClient _httpClient;

    public WeatherApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(string cityName)
    {
        try
        {
            string url = "https://api.open-meteo.com/v1/forecast?latitude=49.62&longitude=20.69&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m";

            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);

            if (response?.Current != null)
            {
                var (condition, icon) = MapWeatherCode(response.Current.WeatherCode);

                return new WeatherData
                {
                    CityName = cityName,
                    Temperature = response.Current.Temperature2m,
                    ConditionText = condition,
                    Icon = icon
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd pobierania pogody: {ex.Message}");
        }

        // Zwracamy null w razie awarii, żeby ViewModel obsłużył brak sieci
        return null;
    }

    private (string Text, string Icon) MapWeatherCode(int code)
    {
        return code switch
        {
            0 => ("Słonecznie", "☀️"),
            1 or 2 => ("Częściowe zachmurzenie", "⛅"),
            3 => ("Pochmurnie", "☁️"),
            45 or 48 => ("Mgła", "🌫️"),
            51 or 53 or 55 => ("Mżawka", "🌧️"),
            61 or 63 or 65 => ("Deszcz", "🌧️"),
            71 or 73 or 75 => ("Śnieg", "🌨️"),
            95 or 96 or 99 => ("Burza", "⚡"),
            _ => ("Pochmurno", "☁️")
        };
    }
}

// Modele pomocnicze do deserializacji odpowiedzi JSON z Open-Meteo
public class OpenMeteoResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("current")]
    public CurrentWeatherDto? Current { get; set; }
}

public class CurrentWeatherDto
{
    [System.Text.Json.Serialization.JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
}