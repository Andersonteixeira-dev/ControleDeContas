using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ControleDeContas.ViewModels
{
    [QueryProperty(nameof(ContaId), "Id")]
    [QueryProperty(nameof(TipoConta), "Tipo")]
    public partial class AddEditContaViewModel : ObservableObject
    {
        [ObservableProperty] private int _contaId;
        [ObservableProperty] private string _tipoConta;
        private ContaUnica _contaUnicaEdit;
        private ContaRecorrente _contaRecorrenteEdit;

        [ObservableProperty] private string _nome;
        [ObservableProperty] private decimal _valor;
        [ObservableProperty] private DateTime _vencimento = DateTime.Now;
        [ObservableProperty] private int _diaVencimento = 1;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsContaUnica))] private bool _isRecorrente;
        public bool IsContaUnica => !IsRecorrente;
        public ObservableCollection<string> Categorias { get; }
        [ObservableProperty] private string _categoriaSelecionada;

        public AddEditContaViewModel()
        {
            Categorias = new ObservableCollection<string> { "Moradia", "Transporte", "Alimentação", "Saúde", "Lazer", "Educação", "Dívidas", "Investimentos", "Outros" };
        }

        public async Task CarregarContaAsync()
        {
            if (ContaId == 0) { IsRecorrente = (TipoConta == "Recorrente"); return; }

            IsRecorrente = (TipoConta == "Recorrente");
            if (IsRecorrente)
            {
                _contaRecorrenteEdit = await DatabaseService.GetContaRecorrenteByIdAsync(ContaId);
                if (_contaRecorrenteEdit != null)
                {
                    Nome = _contaRecorrenteEdit.Nome;
                    Valor = _contaRecorrenteEdit.ValorPadrao;
                    DiaVencimento = _contaRecorrenteEdit.DiaVencimento;
                    CategoriaSelecionada = _contaRecorrenteEdit.Categoria;
                }
            }
            else
            {
                _contaUnicaEdit = await DatabaseService.GetContaUnicaByIdAsync(ContaId);
                if (_contaUnicaEdit != null)
                {
                    Nome = _contaUnicaEdit.Nome;
                    Valor = _contaUnicaEdit.Valor;
                    Vencimento = _contaUnicaEdit.Vencimento;
                    CategoriaSelecionada = _contaUnicaEdit.Categoria;
                }
            }
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(CategoriaSelecionada))
            {
                await Shell.Current.DisplayAlert("Erro", "O nome e a categoria da conta são obrigatórios.", "OK");
                return;
            }

            if (ContaId == 0) // Criar
            {
                if (IsRecorrente)
                    await DatabaseService.SaveContaRecorrenteAsync(new ContaRecorrente { Nome = Nome, ValorPadrao = Valor, DiaVencimento = DiaVencimento, Categoria = CategoriaSelecionada, Ativa = true, DataCriacao = DateTime.Now });
                else
                    await DatabaseService.SaveContaUnicaAsync(new ContaUnica { Nome = Nome, Valor = Valor, Vencimento = Vencimento, Categoria = CategoriaSelecionada });
            }
            else // Editar
            {
                bool tipoOriginalEraRecorrente = (TipoConta == "Recorrente");
                if (tipoOriginalEraRecorrente == IsRecorrente) // O tipo não mudou
                {
                    if (IsRecorrente)
                    {
                        _contaRecorrenteEdit.Nome = Nome;
                        _contaRecorrenteEdit.ValorPadrao = Valor; // Altera o valor base
                        _contaRecorrenteEdit.DiaVencimento = DiaVencimento;
                        _contaRecorrenteEdit.Categoria = CategoriaSelecionada;
                        await DatabaseService.SaveContaRecorrenteAsync(_contaRecorrenteEdit);

                        // LÓGICA CORRIGIDA: Apaga os overrides futuros para que o novo valor base seja aplicado
                        string mesAnoDaEdicao = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM");
                        await DatabaseService.DeleteFutureOverridesAsync(_contaRecorrenteEdit.Id, mesAnoDaEdicao);
                    }
                    else
                    {
                        _contaUnicaEdit.Nome = Nome;
                        _contaUnicaEdit.Valor = Valor;
                        _contaUnicaEdit.Vencimento = Vencimento;
                        _contaUnicaEdit.Categoria = CategoriaSelecionada;
                        await DatabaseService.SaveContaUnicaAsync(_contaUnicaEdit);
                    }
                }
                else // O tipo mudou
                {
                    if (IsRecorrente)
                    {
                        // Cria uma nova conta recorrente
                        await DatabaseService.SaveContaRecorrenteAsync(new ContaRecorrente { Nome = Nome, ValorPadrao = Valor, DiaVencimento = DiaVencimento, Categoria = CategoriaSelecionada, Ativa = true, DataCriacao = DateTime.Now });
                        // Apaga a conta única original
                        if (_contaUnicaEdit != null) await DatabaseService.DeleteContaUnicaAsync(_contaUnicaEdit);
                    }
                    else
                    {
                        // Cria uma nova conta única
                        await DatabaseService.SaveContaUnicaAsync(new ContaUnica { Nome = Nome, Valor = Valor, Vencimento = Vencimento, Categoria = CategoriaSelecionada });
                        // Inativa a conta recorrente original para preservar o histórico
                        if (_contaRecorrenteEdit != null)
                        {
                            _contaRecorrenteEdit.Ativa = false;
                            await DatabaseService.SaveContaRecorrenteAsync(_contaRecorrenteEdit);
                        }
                    }
                }
            }
            await Shell.Current.GoToAsync("..");
        }
    }
}