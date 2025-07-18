using ControleDeContas.Views;

namespace ControleDeContas;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(AddEditContaPage), typeof(AddEditContaPage));
    }
}