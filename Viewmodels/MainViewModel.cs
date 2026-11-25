using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
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
    private double _currentLatitude;
    private double _currentLongitude;

    // Pola dla Kafelka 3 (Alerty)
    private string _alertTitle = "Brak ostrzeżeń";
    private string _alertDescription = "Pogoda bezpieczna";
    private Color _alertBadgeBackground = Color.FromArgb("#064E3B");
    private Color _alertBadgeBorder = Color.FromArgb("#10B981");
    private Color _alertBadgeText = Color.FromArgb("#34D399");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string AlertTitle
    {
        get => _alertTitle;
        set { if (_alertTitle != value) { _alertTitle = value; OnPropertyChanged(); } }
    }

    public string AlertDescription
    {
        get => _alertDescription;
        set { if (_alertDescription != value) { _alertDescription = value; OnPropertyChanged(); } }
    }

    public Color AlertBadgeBackground
    {
        get => _alertBadgeBackground;
        set { if (_alertBadgeBackground != value) { _alertBadgeBackground = value; OnPropertyChanged(); } }
    }

    public Color AlertBadgeBorder
    {
        get => _alertBadgeBorder;
        set { if (_alertBadgeBorder != value) { _alertBadgeBorder = value; OnPropertyChanged(); } }
    }

    public Color AlertBadgeText
    {
        get => _alertBadgeText;
        set { if (_alertBadgeText != value) { _alertBadgeText = value; OnPropertyChanged(); } }
    }

    public double CurrentLatitude
    {
        get => _currentLatitude;
        set
        {
            if (_currentLatitude != value)
            {
                _currentLatitude = value;
                OnPropertyChanged();
            }
        }
    }

    public double CurrentLongitude
    {
        get => _currentLongitude;
        set
        {
            if (_currentLongitude != value)
            {
                _currentLongitude = value;
                OnPropertyChanged();
            }
        }
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
                UpdateWeatherAlerts(); // Automatyczna aktualizacja alertów przy zmianie pogody
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
            IsWeatherLoading = false;
            IsLoading = false;
        }
    }

    private void UpdateWeatherAlerts()
    {
        if (CurrentWeather == null) return;

        var condition = CurrentWeather.ConditionText?.ToLowerInvariant() ?? "";
        var icon = CurrentWeather.Icon ?? "";

        // Ignoruj fallback przy braku sieci/timeout, żeby nie wywalało fałszywego mrozu dla 0.0°C
        if (condition.Contains("brak") || condition.Contains("błąd") || condition.Contains("timeout"))
        {
            AlertTitle = "Brak danych";
            AlertDescription = "Nie udało się zweryfikować pogody";
            AlertBadgeBackground = Color.FromArgb("#334155");
            AlertBadgeBorder = Color.FromArgb("#64748B");
            AlertBadgeText = Color.FromArgb("#CBD5E1");
            return;
        }

        bool isRaining = condition.Contains("deszcz") ||
                         condition.Contains("mżawka") ||
                         condition.Contains("ulewa") ||
                         condition.Contains("burza") ||
                         condition.Contains("rain") ||
                         icon.Contains("🌧") ||
                         icon.Contains("🌦") ||
                         icon.Contains("⛈");

        bool isFreezing = CurrentWeather.Temperature < 1.0;

        if (isRaining && isFreezing)
        {
            AlertTitle = "Gołoledź i deszcz!";
            AlertDescription = $"Opady przy {CurrentWeather.Temperature:F1}°C — ślisko";
            AlertBadgeBackground = Color.FromArgb("#7F1D1D");
            AlertBadgeBorder = Color.FromArgb("#EF4444");
            AlertBadgeText = Color.FromArgb("#FCA5A5");
        }
        else if (isFreezing)
        {
            AlertTitle = "Uwaga na mróz";
            AlertDescription = $"Temperatura spadła do {CurrentWeather.Temperature:F1}°C";
            AlertBadgeBackground = Color.FromArgb("#0C4A6E");
            AlertBadgeBorder = Color.FromArgb("#38BDF8");
            AlertBadgeText = Color.FromArgb("#7DD3FC");
        }
        else if (isRaining)
        {
            AlertTitle = "Opady deszczu";
            AlertDescription = "Weź parasol, mokra nawierzchnia";
            AlertBadgeBackground = Color.FromArgb("#78350F");
            AlertBadgeBorder = Color.FromArgb("#F59E0B");
            AlertBadgeText = Color.FromArgb("#FCD34D");
        }
        else
        {
            AlertTitle = "Brak ostrzeżeń";
            AlertDescription = "Pogoda bezpieczna";
            AlertBadgeBackground = Color.FromArgb("#064E3B");
            AlertBadgeBorder = Color.FromArgb("#10B981");
            AlertBadgeText = Color.FromArgb("#34D399");
        }
    }


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