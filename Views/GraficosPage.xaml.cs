using ControleDeContas.ViewModels;

namespace ControleDeContas.Views;

public partial class GraficosPage : ContentPage
{
    private readonly GraficosViewModel _viewModel;

    public GraficosPage(GraficosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.CarregarDadosGraficoCommand.CanExecute(null))
        {
            _viewModel.CarregarDadosGraficoCommand.Execute(null);
        }
    }
}