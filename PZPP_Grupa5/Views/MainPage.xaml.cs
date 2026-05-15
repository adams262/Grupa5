using PZPP_Grupa5.ViewModels;

namespace PZPP_Grupa5.Views;

public partial class MainPage : ContentPage
{
    
    private const uint AnimationDuration = 250;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    
    private async void OnHamburgerClicked(object sender, EventArgs e)
    {
        MenuOverlay.IsVisible = true;

        await Task.WhenAll(
            SidebarMenu.TranslateTo(0, 0, AnimationDuration, Easing.CubicOut),
            MenuOverlay.FadeTo(0.6, AnimationDuration, Easing.CubicOut)
        );
    }

    
    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await Task.WhenAll(
            SidebarMenu.TranslateTo(-300, 0, AnimationDuration, Easing.CubicIn),
            MenuOverlay.FadeTo(0, AnimationDuration, Easing.CubicIn)
        );

        MenuOverlay.IsVisible = false;
    }
}