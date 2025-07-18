using ControleDeContas.ViewModels;

namespace ControleDeContas.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.CarregarContasCommand.CanExecute(null))
        {
            _viewModel.CarregarContasCommand.Execute(null);
        }
    }

    // O método OnBackButtonPressed foi removido para restaurar o comportamento padrão.
}