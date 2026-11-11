using WeatherApp.Mobile.ViewModels;
using Microsoft.Maui.Platform;

namespace WeatherApp.Mobile.Views;

public partial class MainPage : ContentPage
{
    private bool _isMapInitialized = false;
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Nasłuchiwanie zmian współrzędnych GPS z ViewModelu
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentLatitude) ||
                e.PropertyName == nameof(MainViewModel.CurrentLongitude))
            {
                UpdateMapPosition(_viewModel.CurrentLatitude, _viewModel.CurrentLongitude);
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
            var htmlSource = new HtmlWebViewSource();
            htmlSource.Html = $@"
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
                    }}).setView([{lat}, {lon}], 14);

                    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                        maxZoom: 19
                    }}).addTo(map);

                    L.marker([{lat}, {lon}]).addTo(map);
                </script>
            </body>
            </html>";

            MapWebView.Source = htmlSource;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd mapy: {ex.Message}");
        }
    }
}