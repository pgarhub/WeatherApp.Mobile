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

    public bool IsWeatherLoading // <-- WŁAŚCIWOŚĆ DLA LOKALNEGO KRĘCIOŁKA W KAFELKU
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
            var weather = await _weatherService.GetCurrentWeatherAsync("Nowy Sącz");

            if (weather != null)
            {
                CurrentWeather = weather;
            }
            else
            {
                CurrentWeather = new WeatherData
                {
                    CityName = "Nowy Sącz",
                    Temperature = 0.0,
                    ConditionText = "Brak połączenia",
                    Icon = "🔌"
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd pobierania pogody: {ex.Message}");
            CurrentWeather = new WeatherData
            {
                CityName = "Nowy Sącz",
                Temperature = 0.0,
                ConditionText = "Błąd sieci",
                Icon = "⚠️"
            };
        }
        finally
        {
            IsWeatherLoading = false; // Zdejmuje ładowanie z kafelka
            IsLoading = false;        // Zdejmuje ewentualną globalną nakładkę
        }
    }
    // Dopisaj te właściwości w klasie MainViewModel:
    private string _gpsStatusText = "Pobierz lokalizację GPS";
    private double _currentLatitude = 49.62; // Domyślnie Nowy Sącz
    private double _currentLongitude = 20.69;

    public string GpsStatusText
    {
        get => _gpsStatusText;
        set { if (_gpsStatusText != value) { _gpsStatusText = value; OnPropertyChanged(); } }
    }

    public double CurrentLatitude
    {
        get => _currentLatitude;
        set { if (_currentLatitude != value) { _currentLatitude = value; OnPropertyChanged(); } }
    }

    public double CurrentLongitude
    {
        get => _currentLongitude;
        set { if (_currentLongitude != value) { _currentLongitude = value; OnPropertyChanged(); } }
    }

    public ICommand GetUserLocationCommand => new Command(async () => await GetUserLocationAsync());

    public async Task GetUserLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return;
            }

            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
            if (location != null)
            {
                CurrentLatitude = location.Latitude;
                CurrentLongitude = location.Longitude;
                GpsStatusText = $"{location.Latitude:F2}, {location.Longitude:F2}";
            }
        }
        catch (Exception)
        {
            GpsStatusText = "Błąd GPS";
        }
    }
}