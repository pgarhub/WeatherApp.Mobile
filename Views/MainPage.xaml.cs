using System.Globalization;
using WeatherApp.Mobile.ViewModels;

namespace WeatherApp.Mobile.Views;

public partial class MainPage : ContentPage
{
    private bool _isMapInitialized = false;
    private readonly MainViewModel _viewModel;

    // Domyślne współrzędne (Nowy Sącz), gdy GPS nie zwrócił jeszcze pozycji
    private const double DefaultLat = 49.6218;
    private const double DefaultLon = 20.6970;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

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

        if (!_isMapInitialized)
        {
            _isMapInitialized = true;
            await Task.Delay(150);
            UpdateMapPosition(_viewModel.CurrentLatitude, _viewModel.CurrentLongitude);
        }
    }

    private void UpdateMapPosition(double lat, double lon)
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

            MapWebView.Source = htmlSource;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd mapy: {ex.Message}");
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