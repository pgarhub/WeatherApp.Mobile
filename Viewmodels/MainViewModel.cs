using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WeatherApp.Mobile.Models;
using WeatherApp.Mobile.Services;

namespace WeatherApp.Mobile.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly WeatherApiService _weatherService;
    private WeatherData _currentWeather = new();
    private int _stepCount = 5420;
    private string _biometricStatus = "Zablokowane";

    public WeatherData CurrentWeather
    {
        get => _currentWeather;
        set { _currentWeather = value; OnPropertyChanged(); }
    }

    public int StepCount
    {
        get => _stepCount;
        set { _stepCount = value; OnPropertyChanged(); }
    }

    public string BiometricStatus
    {
        get => _biometricStatus;
        set { _biometricStatus = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }

    public MainViewModel(WeatherApiService weatherService)
    {
        _weatherService = weatherService;
        RefreshCommand = new Command(async () => await FetchWeatherAsync());

        // Inicjalne pobranie danych
        _ = FetchWeatherAsync();
    }

    public async Task FetchWeatherAsync()
    {
        CurrentWeather = await _weatherService.GetCurrentWeatherAsync("Nowy Sącz");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}