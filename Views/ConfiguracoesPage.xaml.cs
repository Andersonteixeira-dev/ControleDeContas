namespace ControleDeContas.Views;

public partial class ConfiguracoesPage : ContentPage
{
    public ConfiguracoesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Carrega a prefer�ncia salva e marca o RadioButton correto
        var savedTheme = Preferences.Get("app_theme", "System");
        if (savedTheme == "Light")
            LightThemeRadioButton.IsChecked = true;
        else if (savedTheme == "Dark")
            DarkThemeRadioButton.IsChecked = true;
        else
            SystemThemeRadioButton.IsChecked = true;
    }

    private void ThemeRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) // S� executa a l�gica quando um bot�o � MARCADO
            return;

        var selectedTheme = (sender as RadioButton)?.Content as string;

        string themeToSet = "System";
        if (selectedTheme == "Tema Claro")
            themeToSet = "Light";
        else if (selectedTheme == "Tema Escuro")
            themeToSet = "Dark";

        // Chama o nosso m�todo est�tico para mudar o tema e salvar a prefer�ncia
        App.SetTheme(themeToSet);
    }
}