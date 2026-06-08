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

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Task.WhenAll(
            ResultView.FadeTo(0, 200, Easing.Linear),
            ResultView.TranslateTo(0, 50, 200, Easing.CubicIn)
        );

        if (BindingContext is MainViewModel vm)
        {
            vm.BackToInputCommand.Execute(null);
        }

        InputView.TranslationY = -50;
        InputView.Opacity = 0;
        await Task.WhenAll(
            InputView.FadeTo(1, 300, Easing.CubicOut),
            InputView.TranslateTo(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnElementPressed(object sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            await element.ScaleTo(0.92, 100, Easing.CubicOut);
        }
    }

    private async void OnElementReleased(object sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            await element.ScaleTo(1.0, 100, Easing.CubicIn);
        }
    }

    private async void OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is VisualElement element)
        {
            await Task.WhenAll(
                element.ScaleTo(1.02, 150, Easing.CubicOut),
                element.RotateTo(0.5, 150, Easing.CubicOut)
            );
        }
    }

    private async void OnPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is VisualElement element)
        {
            await Task.WhenAll(
                element.ScaleTo(1.0, 150, Easing.CubicIn),
                element.RotateTo(0, 150, Easing.CubicIn)
            );
        }
    }

    private async void OnHistoryItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is BindableObject clickedElement && clickedElement.BindingContext is Models.ChatHistoryItem historyItem)
        {
            OnOverlayTapped(sender, e);

            if (BindingContext is MainViewModel vm)
            {
                vm.WczytajHistorieCommand.Execute(historyItem);

                ResultView.Opacity = 0;
                await ResultView.FadeTo(1, 400, Easing.CubicOut);
            }
        }
    }

    private void OnDeleteHistoryItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Models.ChatHistoryItem itemDoUsuniecia)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.UsunHistorieCommand.Execute(itemDoUsuniecia);
            }
        }
    }

    private void OnProcessVideoClicked(object sender, EventArgs e)
    {
        ResultView.Opacity = 1;
        ResultView.TranslationY = 0;
    }
}