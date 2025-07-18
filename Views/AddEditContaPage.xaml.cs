using ControleDeContas.ViewModels;

namespace ControleDeContas.Views;

public partial class AddEditContaPage : ContentPage
{
    private readonly AddEditContaViewModel _viewModel;

    public AddEditContaPage(AddEditContaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarContaAsync();
    }
}