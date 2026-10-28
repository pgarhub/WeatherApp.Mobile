namespace WeatherApp.Mobile.Models;

public class WeatherData
{
    public string CityName { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Icon { get; set; } = "☀️";
    public string ConditionText { get; set; } = string.Empty;
}


