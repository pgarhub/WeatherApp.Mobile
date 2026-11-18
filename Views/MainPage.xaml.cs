using System.Globalization;
using WeatherApp.Mobile.ViewModels;

namespace WeatherApp.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isMapInitialized;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        ConfigureWebViewTouchForAndroid();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isMapInitialized)
        {
            _isMapInitialized = true;
            await InitializeUserLocationAndMapAsync();
        }
    }

    // 1. Naprawia przesuwanie mapy palcem wewnątrz ScrollView na Androidzie
    private void ConfigureWebViewTouchForAndroid()
    {
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("AllowMapPanning", (handler, view) =>
        {
            handler.PlatformView.Touch += (s, e) =>
            {
                if (e.Event?.Action == Android.Views.MotionEventActions.Down ||
                    e.Event?.Action == Android.Views.MotionEventActions.Move)
                {
                    handler.PlatformView.Parent?.RequestDisallowInterceptTouchEvent(true);
                }
                else if (e.Event?.Action == Android.Views.MotionEventActions.Up ||
                         e.Event?.Action == Android.Views.MotionEventActions.Cancel)
                {
                    handler.PlatformView.Parent?.RequestDisallowInterceptTouchEvent(false);
                }

                // False pozwala WebView normalnie obsłużyć kliknięcie i przesunięcie mapy
                e.Handled = false;
            };
        });
#endif
    }

    // 2. Pobiera aktualną lokalizację GPS urządzenia
    private async Task InitializeUserLocationAndMapAsync()
    {
        double lat = _viewModel.CurrentLatitude;
        double lon = _viewModel.CurrentLongitude;

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    lat = location.Latitude;
                    lon = location.Longitude;
                    _viewModel.CurrentLatitude = lat;
                    _viewModel.CurrentLongitude = lon;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd GPS: {ex.Message}");
        }

        // Domyślnie Nowy Sącz, jeśli GPS i ViewModel zwrócą 0
        if (lat == 0 && lon == 0)
        {
            lat = 49.6218;
            lon = 20.6970;
        }

        RenderMap(lat, lon);
    }

    // 3. Renderuje interaktywną mapę Leaflet
    private void RenderMap(double lat, double lon)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        MapWebView.Source = new HtmlWebViewSource
        {
            Html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8' />
                <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
                <style>
                    body, html {{ margin: 0; padding: 0; height: 100%; background: #161E2E; }}
                    #map {{ width: 100%; height: 100%; }}
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script>
                    var map = L.map('map', {{
                        zoomControl: true,
                        attributionControl: false,
                        dragging: true,
                        touchZoom: true
                    }}).setView([{latStr}, {lonStr}], 13);

                    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                        maxZoom: 19
                    }}).addTo(map);

                    var marker = L.marker([{latStr}, {lonStr}]).addTo(map)
                        .bindPopup('Twoja lokalizacja').openPopup();

                    // Obsługa kliknięcia w dowolne miejsce na mapie
                    map.on('click', function(e) {{
                        var newLat = e.latlng.lat.toFixed(5);
                        var newLon = e.latlng.lng.toFixed(5);

                        marker.setLatLng(e.latlng)
                              .setPopupContent('Wybrano: ' + newLat + ', ' + newLon)
                              .openPopup();

                        // Przekazanie współrzędnych do C#
                        window.location.href = 'maui://mapclick?lat=' + newLat + '&lon=' + newLon;
                    }});
                </script>
            </body>
            </html>"
        };
    }

    // 4. Odbiera kliknięcie z mapy w kodzie C#
    private void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("maui://mapclick"))
        {
            e.Cancel = true; // Blokuje próbę otwarcia adresu jako strony WWW

            var uri = new Uri(e.Url);
            var queryParams = uri.Query.TrimStart('?').Split('&');

            double? clickedLat = null;
            double? clickedLon = null;

            foreach (var param in queryParams)
            {
                var parts = param.Split('=');
                if (parts.Length == 2)
                {
                    if (parts[0] == "lat" && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                        clickedLat = lat;
                    if (parts[0] == "lon" && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        clickedLon = lon;
                }
            }

            if (clickedLat.HasValue && clickedLon.HasValue)
            {
                _viewModel.CurrentLatitude = clickedLat.Value;
                _viewModel.CurrentLongitude = clickedLon.Value;

                // Opcjonalnie: odśwież od razu pogodę dla klikniętego punktu na mapie:
                // _ = _viewModel.RefreshWeatherCommand.ExecuteAsync(null);
            }
        }
    }
}