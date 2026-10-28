using WeatherApp.Mobile.ViewModels;

namespace WeatherApp.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}