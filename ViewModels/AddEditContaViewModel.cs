using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ControleDeContas.ViewModels
{
    [QueryProperty(nameof(ContaId), "Id")]
    [QueryProperty(nameof(TipoConta), "Tipo")]
    [QueryProperty(nameof(DataContexto), "DataContexto")]
    public partial class AddEditContaViewModel : ObservableObject
    {
        [ObservableProperty] private int _contaId;
        [ObservableProperty] private string _tipoConta;
        [ObservableProperty] private DateTime _dataContexto = DateTime.Now;
        private ContaUnica _contaUnicaEdit;
        private ContaRecorrente _contaRecorrenteEdit;

        [ObservableProperty] private string _nome;
        [ObservableProperty] private decimal _valor;
        [ObservableProperty] private DateTime _vencimento = DateTime.Now;
        [ObservableProperty] private int _diaVencimento = 1;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsContaUnica))] private bool _isRecorrente;
        [ObservableProperty] private string _categoriaSelecionada;

        public bool IsContaUnica => !IsRecorrente;
        public ObservableCollection<string> Categorias { get; }

        public AddEditContaViewModel()
        {
            Categorias = new ObservableCollection<string> { "Moradia", "Transporte", "Alimentação", "Saúde", "Lazer", "Educação", "Dívidas", "Investimentos", "Outros" };
        }

        public async Task CarregarContaAsync()
        {
            if (ContaId == 0) { IsRecorrente = TipoConta == "Recorrente"; return; }

            IsRecorrente = TipoConta == "Recorrente";
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
                await Shell.Current.DisplayAlert("Erro", "Nome e categoria são obrigatórios.", "OK");
                return;
            }

            if (ContaId == 0) // Criar
            {
                if (IsRecorrente)
                {
                    var dataCriacaoNormalizada = new DateTime(DataContexto.Year, DataContexto.Month, 1);
                    await DatabaseService.SaveContaRecorrenteAsync(new ContaRecorrente { Nome = Nome, ValorPadrao = Valor, DiaVencimento = DiaVencimento, Categoria = CategoriaSelecionada, Ativa = true, DataCriacao = dataCriacaoNormalizada });
                }
                else
                {
                    await DatabaseService.SaveContaUnicaAsync(new ContaUnica { Nome = Nome, Valor = Valor, Vencimento = Vencimento, Categoria = CategoriaSelecionada });
                }
            }
            else // Editar
            {
                bool tipoOriginalEraRecorrente = (TipoConta == "Recorrente");
                if (tipoOriginalEraRecorrente && IsRecorrente)
                {
                    string acao = await Shell.Current.DisplayActionSheet(
                        "Como aplicar esta alteração?",
                        "Cancelar",
                        null,
                        "Alterar só a partir deste mês",
                        "Corrigir para todos os meses");

                    if (acao == "Alterar só a partir deste mês")
                    {
                        var primeiroDiaDoMesContexto = new DateTime(DataContexto.Year, DataContexto.Month, 1);
                        _contaRecorrenteEdit.DataEncerramento = primeiroDiaDoMesContexto.AddDays(-1);
                        _contaRecorrenteEdit.Ativa = false;
                        await DatabaseService.SaveContaRecorrenteAsync(_contaRecorrenteEdit);

                        var novaConta = new ContaRecorrente { Nome = Nome, ValorPadrao = Valor, DiaVencimento = DiaVencimento, Categoria = CategoriaSelecionada, Ativa = true, DataCriacao = primeiroDiaDoMesContexto };
                        await DatabaseService.SaveContaRecorrenteAsync(novaConta);
                    }
                    else if (acao == "Corrigir para todos os meses")
                    {
                        _contaRecorrenteEdit.Nome = Nome;
                        _contaRecorrenteEdit.ValorPadrao = Valor;
                        _contaRecorrenteEdit.DiaVencimento = DiaVencimento;
                        _contaRecorrenteEdit.Categoria = CategoriaSelecionada;
                        await DatabaseService.SaveContaRecorrenteAsync(_contaRecorrenteEdit);
                    }
                    else { return; }
                }
                else if (_contaUnicaEdit != null)
                {
                    _contaUnicaEdit.Nome = Nome;
                    _contaUnicaEdit.Valor = Valor;
                    _contaUnicaEdit.Vencimento = Vencimento;
                    _contaUnicaEdit.Categoria = CategoriaSelecionada;
                    await DatabaseService.SaveContaUnicaAsync(_contaUnicaEdit);
                }
            }
            await Shell.Current.GoToAsync("..");
        }
    }
}