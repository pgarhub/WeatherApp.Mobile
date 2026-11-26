using System.Globalization;
using WeatherApp.Mobile.ViewModels;

namespace WeatherApp.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isMapRendered;
    private bool _hasRealGpsFix;
    private bool _isFullScreen;
    private IDispatcherTimer? _gpsTimer;

    private double _userLatitude = 49.6218;
    private double _userLongitude = 20.6970;

    // Domyślne współrzędne (Nowy Sącz), gdy GPS nie zwrócił jeszcze pozycji
    private const double DefaultLat = 49.6218;
    private const double DefaultLon = 20.6970;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        ConfigureWebViewTouchForAndroid();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentLatitude) ||
                e.PropertyName == nameof(MainViewModel.CurrentLongitude))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateMapPosition(_viewModel.CurrentLatitude, _viewModel.CurrentLongitude);
                });
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isMapRendered)
        {
            _isMapRendered = true;
            await InitializeLocationAndMapAsync();
        }
        else
        {
            _gpsTimer?.Start();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gpsTimer?.Stop();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_isFullScreen)
        {
            MainThread.BeginInvokeOnMainThread(async () => await CloseFullScreenMapAsync());
            return true;
        }

        return base.OnBackButtonPressed();
    }

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

                e.Handled = false;
            };
        });
#endif
    }

    private async Task InitializeLocationAndMapAsync()
    {
        await UpdateUserCoordinatesFromGpsAsync();

        _viewModel.CurrentLatitude = _userLatitude;
        _viewModel.CurrentLongitude = _userLongitude;

        RenderMap(_userLatitude, _userLongitude);

        _gpsTimer = Dispatcher.CreateTimer();
        _gpsTimer.Interval = TimeSpan.FromSeconds(4);
        _gpsTimer.Tick += async (s, e) => await RefreshLiveDotAsync();
        _gpsTimer.Start();
    }

    private async Task<bool> UpdateUserCoordinatesFromGpsAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
                return false;

            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                           ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

            if (location != null)
            {
                _userLatitude = location.Latitude;
                _userLongitude = location.Longitude;
                _hasRealGpsFix = true;
                return true;
            }
        }
        catch
        {
            // Ignorujemy błędy GPS (np. wyłączona lokalizacja) i zostajemy przy domyślnych współrzędnych
        }

        return false;
    }

    private async Task RefreshLiveDotAsync()
    {
        if (await UpdateUserCoordinatesFromGpsAsync())
        {
            string latStr = _userLatitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = _userLongitude.ToString(CultureInfo.InvariantCulture);
            await MapWebView.EvaluateJavaScriptAsync($"if(typeof updateUserDot === 'function') updateUserDot({latStr}, {lonStr});");
        }
    }

    private async void UpdateMapPosition(double lat, double lon)
    {
        if (!_isMapRendered) return;

        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);
        await MapWebView.EvaluateJavaScriptAsync($"if(typeof updateMapCenter === 'function') updateMapCenter({latStr}, {lonStr});");
    }

    private void RenderMap(double lat, double lon)
    {
        double targetLat = (lat == 0 && lon == 0) ? DefaultLat : lat;
        double targetLon = (lat == 0 && lon == 0) ? DefaultLon : lon;

        string latStr = targetLat.ToString(CultureInfo.InvariantCulture);
        string lonStr = targetLon.ToString(CultureInfo.InvariantCulture);

        var htmlSource = new HtmlWebViewSource
        {
            Html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8' />
                <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
                <style>
                    body, html {{ margin: 0; padding: 0; height: 100%; background: #1E293B; }}
                    #map {{ width: 100%; height: 100%; touch-action: pan-x pan-y; }}
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script>
                    var isFullScreen = false;
                    var userLat = {latStr};
                    var userLon = {lonStr};

                    var map = L.map('map', {{
                        zoomControl: false,
                        attributionControl: false,
                        dragging: true,
                        tap: true
                    }}).setView([userLat, userLon], 14);

                    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                        maxZoom: 19
                    }}).addTo(map);

                    var marker = L.marker([userLat, userLon]).addTo(map);
                    var userDot = L.circleMarker([userLat, userLon], {{
                        radius: 6,
                        fillColor: '#3B82F6',
                        color: '#FFFFFF',
                        weight: 2,
                        fillOpacity: 1
                    }}).addTo(map);

                    map.on('click', function(e) {{
                        if (!isFullScreen) {{
                            window.location.href = 'maui://fullscreen';
                        }} else {{
                            marker.setLatLng(e.latlng);
                            window.location.href = 'maui://mapclick?lat=' + e.latlng.lat + '&lon=' + e.latlng.lng;
                        }}
                    }});

                    function setFullScreenMode(full) {{
                        isFullScreen = full;
                        setTimeout(function() {{ map.invalidateSize(); }}, 150);
                    }}

                    function updateMapCenter(lat, lon) {{
                        map.setView([lat, lon], map.getZoom());
                        marker.setLatLng([lat, lon]);
                    }}

                    function updateUserDot(lat, lon) {{
                        userLat = lat;
                        userLon = lon;
                        userDot.setLatLng([lat, lon]);
                    }}

                    function resetToUserLocation() {{
                        updateMapCenter(userLat, userLon);
                    }}
                </script>
            </body>
            </html>"
        };

        MapWebView.Source = htmlSource;
    }

    private async Task OpenFullScreenMapAsync()
    {
        if (_isFullScreen) return;
        _isFullScreen = true;

        SmallMapContainer.Content = null;
        MapWebView.HeightRequest = -1;
        FullScreenMapContainer.Content = MapWebView;
        FullScreenMapOverlay.IsVisible = true;

        await Task.Delay(100);
        await MapWebView.EvaluateJavaScriptAsync("setFullScreenMode(true);");
    }

    private async Task CloseFullScreenMapAsync()
    {
        if (!_isFullScreen) return;
        _isFullScreen = false;

        FullScreenMapOverlay.IsVisible = false;
        FullScreenMapContainer.Content = null;
        MapWebView.HeightRequest = 260;
        SmallMapContainer.Content = MapWebView;

        await Task.Delay(100);
        await MapWebView.EvaluateJavaScriptAsync("setFullScreenMode(false);");
    }

    private async void OnCloseFullScreenClicked(object? sender, EventArgs e)
    {
        await CloseFullScreenMapAsync();
    }

    private async void OnResetMapButtonClicked(object? sender, EventArgs e)
    {
        _viewModel.CurrentLatitude = _userLatitude;
        _viewModel.CurrentLongitude = _userLongitude;

        await MapWebView.EvaluateJavaScriptAsync("resetToUserLocation();");
    }

    private async void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("maui://fullscreen"))
        {
            e.Cancel = true;
            await OpenFullScreenMapAsync();
            return;
        }

        if (e.Url.StartsWith("maui://mapclick"))
        {
            e.Cancel = true;

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
            }
        }
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new SettingsPage());
    }
}