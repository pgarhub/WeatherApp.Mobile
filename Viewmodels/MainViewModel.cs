using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    private bool _isLoading = true;
    private bool _isWeatherLoading = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public WeatherData CurrentWeather
    {
        get => _currentWeather;
        set
        {
            if (_currentWeather != value)
            {
                _currentWeather = value;
                OnPropertyChanged();
            }
        }
    }

    public int StepCount
    {
        get => _stepCount;
        set
        {
            if (_stepCount != value)
            {
                _stepCount = value;
                OnPropertyChanged();
            }
        }
    }

    public string BiometricStatus
    {
        get => _biometricStatus;
        set
        {
            if (_biometricStatus != value)
            {
                _biometricStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsWeatherLoading
    {
        get => _isWeatherLoading;
        set
        {
            if (_isWeatherLoading != value)
            {
                _isWeatherLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand RefreshCommand { get; }

    public MainViewModel(WeatherApiService weatherService)
    {
        _weatherService = weatherService;
        RefreshCommand = new Command(async () => await FetchWeatherAsync());

        // Inicjalne pobranie danych po uruchomieniu
        _ = FetchWeatherAsync();
    }

    public async Task FetchWeatherAsync()
    {
        IsWeatherLoading = true;

        try
        {
            // Limit czasu 5 sekund na odpowiedź z sieci
            var weatherTask = _weatherService.GetCurrentWeatherAsync("Nowy Sącz");
            var timeoutTask = Task.Delay(5000);

            var completedTask = await Task.WhenAny(weatherTask, timeoutTask);

            if (completedTask == weatherTask)
            {
                var weather = await weatherTask;
                CurrentWeather = weather ?? GetFallbackWeatherData("Brak danych");
            }
            else
            {
                // Przekroczono limit czasu (np. brak sieci w emulatorze)
                System.Diagnostics.Debug.WriteLine("Przekroczono czas oczekiwania na API.");
                CurrentWeather = GetFallbackWeatherData("Brak odpowiedzi (Timeout)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd pobierania pogody: {ex.Message}");
            CurrentWeather = GetFallbackWeatherData("Błąd sieci");
        }
        finally
        {
            // Gwarancja ukrycia wskaźników ładowania
            IsWeatherLoading = false;
            IsLoading = false;
        }
    }

    // Pomocnicza metoda na wypadek błędu
    private WeatherData GetFallbackWeatherData(string statusText)
    {
        return new WeatherData
        {
            CityName = "Nowy Sącz",
            Temperature = 0.0,
            ConditionText = statusText,
            Icon = "⚠️"
        };
    }
}