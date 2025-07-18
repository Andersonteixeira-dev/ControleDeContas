using ControleDeContas.ViewModels;
using ControleDeContas.Views;
using Microcharts.Maui;

namespace ControleDeContas;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMicrocharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registo de todos os componentes
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<AddEditContaViewModel>();
        builder.Services.AddTransient<AddEditContaPage>();
        builder.Services.AddTransient<GraficosViewModel>();
        builder.Services.AddTransient<GraficosPage>();
       

        return builder.Build();
    }
}