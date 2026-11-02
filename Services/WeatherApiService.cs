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

    public async Task<WeatherData> GetCurrentWeatherAsync(string cityName)
    {
        try
        {
            // Współrzędne dla Nowego Sącza (szerokość: 49.62, długość: 20.69)
            string url = "https://api.open-meteo.com/v1/forecast?latitude=49.62&longitude=20.69&current=temperature_2m,weather_code";

            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);

            if (response?.Current != null)
            {
                // Tłumaczymy kod pogody na polski tekst i ikonkę
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

        // Awaryjne dane, gdyby internet siadł
        return new WeatherData
        {
            CityName = cityName,
            Temperature = 20.0,
            ConditionText = "Błąd pobierania",
            Icon = "⚠️"
        };
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

// Modele pomocnicze do odczytu JSON z Open-Meteo
public class OpenMeteoResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("current")]
    public CurrentWeatherDto? Current { get; set; }
}

public class CurrentWeatherDto
{
    [System.Text.Json.Serialization.JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
}