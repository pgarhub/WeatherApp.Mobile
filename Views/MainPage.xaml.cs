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
            double targetLat = (lat == 0 && lon == 0) ? DefaultLat : lat;
            double targetLon = (lat == 0 && lon == 0) ? DefaultLon : lon;

            // InvariantCulture wymusza kropkę zamiast przecinka w liczbach (wymagane przez JS)
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
                        var map = L.map('map', {{
                            zoomControl: false,
                            attributionControl: false,
                            dragging: true,
                            tap: true
                        }}).setView([{latStr}, {lonStr}], 14);

                        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                            maxZoom: 19
                        }}).addTo(map);

                        L.marker([{latStr}, {lonStr}]).addTo(map);

                        map.on('click', function() {{
                            window.location.href = 'app://expandmap';
                        }});
                    </script>
                </body>
                </html>"
            };

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

    private void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("app://expandmap"))
        {
            e.Cancel = true;

            if (!FullScreenMapOverlay.IsVisible)
            {
                SmallMapContainer.Content = null;
                FullScreenMapContainer.Content = MapWebView;
                FullScreenMapOverlay.IsVisible = true;
            }
        }
    }

    private void OnCloseFullScreenClicked(object? sender, EventArgs e)
    {
        FullScreenMapOverlay.IsVisible = false;
        FullScreenMapContainer.Content = null;
        SmallMapContainer.Content = MapWebView;
    }

    private void OnResetMapButtonClicked(object? sender, EventArgs e)
    {
        UpdateMapPosition(_viewModel.CurrentLatitude, _viewModel.CurrentLongitude);
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new SettingsPage());
    }
}