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
    private bool _isLoading = true;

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
        IsLoading = true; // Włączamy ekran ładowania

        try
        {
            // Pobieramy faktyczną pogodę z Nowego Sącza przez API Open-Meteo
            CurrentWeather = await _weatherService.GetCurrentWeatherAsync("Nowy Sącz");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd pobierania pogody: {ex.Message}");
        }
        finally
        {
            IsLoading = false; // Wyłączamy ekran ładowania bez względu na wynik
        }
    }
}