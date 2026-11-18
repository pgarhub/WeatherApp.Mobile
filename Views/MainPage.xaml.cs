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

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                if (location == null)
                {
                    location = await Geolocation.Default.GetLastKnownLocationAsync();
                }

                if (location != null)
                {
                    _userLatitude = location.Latitude;
                    _userLongitude = location.Longitude;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd GPS: {ex.Message}");
        }

        return false;
    }

    private async Task RefreshLiveDotAsync()
    {
        bool gotFix = await UpdateUserCoordinatesFromGpsAsync();
        if (!gotFix) return;

        bool isFirstFix = !_hasRealGpsFix;
        _hasRealGpsFix = true;

        string latStr = _userLatitude.ToString(CultureInfo.InvariantCulture);
        string lonStr = _userLongitude.ToString(CultureInfo.InvariantCulture);
        string centerParam = isFirstFix ? "true" : "false";

        await MapWebView.EvaluateJavaScriptAsync($"updateLiveUserLocation({latStr}, {lonStr}, {centerParam});");
    }

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
                    body, html {{ margin: 0; padding: 0; height: 100%; background: #0F172A; }}
                    #map {{ width: 100%; height: 100%; }}
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script>
                    var homeLat = {latStr};
                    var homeLon = {lonStr};
                    var isFullScreen = false;

                    var map = L.map('map', {{
                        zoomControl: true,
                        attributionControl: false,
                        dragging: true,
                        touchZoom: true
                    }}).setView([homeLat, homeLon], 15);

                    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                        maxZoom: 19
                    }}).addTo(map);

                    var userCircle = L.circle([homeLat, homeLon], {{
                        color: '#38BDF8',
                        fillColor: '#38BDF8',
                        fillOpacity: 0.15,
                        radius: 120,
                        weight: 1
                    }}).addTo(map);

                    var userDot = L.circleMarker([homeLat, homeLon], {{
                        radius: 9,
                        fillColor: '#2563EB',
                        color: '#FFFFFF',
                        weight: 3,
                        opacity: 1,
                        fillOpacity: 1
                    }}).addTo(map);

                    var clickedPin = null;

                    map.on('click', function(e) {{
                        if (!isFullScreen) {{
                            window.location.href = 'maui://fullscreen';
                            return;
                        }}

                        var newLat = e.latlng.lat.toFixed(5);
                        var newLon = e.latlng.lng.toFixed(5);

                        if (clickedPin === null) {{
                            clickedPin = L.marker(e.latlng).addTo(map);
                        }} else {{
                            clickedPin.setLatLng(e.latlng);
                        }}

                        window.location.href = 'maui://mapclick?lat=' + newLat + '&lon=' + newLon;
                    }});

                    function setFullScreenMode(enabled) {{
                        isFullScreen = enabled;
                        setTimeout(function() {{
                            map.invalidateSize();
                        }}, 150);
                    }}

                    function updateLiveUserLocation(newLat, newLon, shouldCenter) {{
                        homeLat = newLat;
                        homeLon = newLon;
                        userDot.setLatLng([newLat, newLon]);
                        userCircle.setLatLng([newLat, newLon]);

                        if (shouldCenter) {{
                            map.flyTo([newLat, newLon], 15, {{ duration: 0.8 }});
                        }}
                    }}

                    function resetToUserLocation() {{
                        if (clickedPin !== null) {{
                            map.removeLayer(clickedPin);
                            clickedPin = null;
                        }}
                        map.flyTo([homeLat, homeLon], 15, {{ duration: 0.8 }});
                    }}
                </script>
            </body>
            </html>"
        };
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
}