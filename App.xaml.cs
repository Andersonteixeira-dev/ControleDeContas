using ControleDeContas.Services;

namespace ControleDeContas;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        DatabaseService.InitializeAsync();
        MainPage = new AppShell();
    }
}