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
        // Carrega a preferência salva e marca o RadioButton correto
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
        if (!e.Value) // Só executa a lógica quando um botão é MARCADO
            return;

        var selectedTheme = (sender as RadioButton)?.Content as string;

        string themeToSet = "System";
        if (selectedTheme == "Tema Claro")
            themeToSet = "Light";
        else if (selectedTheme == "Tema Escuro")
            themeToSet = "Dark";

        // Chama o nosso método estático para mudar o tema e salvar a preferência
        App.SetTheme(themeToSet);
    }
}