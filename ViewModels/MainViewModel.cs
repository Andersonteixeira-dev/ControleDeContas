using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using ControleDeContas.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace ControleDeContas.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private DateTime _dataCorrente = DateTime.Now;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotBusy))] private bool _isBusy;
        [ObservableProperty] private decimal _totalPendente;
        [ObservableProperty] private decimal _totalPago;
        [ObservableProperty] private decimal _totalNA;

        public ObservableCollection<ContaDisplay> ContasDoMes { get; } = new();
        public bool IsNotBusy => !IsBusy;

        [RelayCommand]
        public async Task CarregarContasAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                ContasDoMes.Clear();
                var mesAno = DataCorrente.ToString("yyyy-MM");
                var primeiroDiaDoMesCorrente = new DateTime(DataCorrente.Year, DataCorrente.Month, 1);

                var contasUnicas = await DatabaseService.GetContasUnicasAsync();
                var todasContasRecorrentes = await DatabaseService.GetContasRecorrentesAsync();
                var pagamentosDoMes = await DatabaseService.GetPagamentosDoMesAsync(mesAno);
                var overridesDoMes = await DatabaseService.GetOverridesDoMesAsync(mesAno);

                foreach (var conta in contasUnicas.Where(c => c.Vencimento.Year == DataCorrente.Year && c.Vencimento.Month == DataCorrente.Month))
                {
                    ContasDoMes.Add(new ContaDisplay { Id = conta.Id, Categoria = conta.Categoria, Tipo = "Unica", Nome = conta.Nome, Valor = conta.Valor, Vencimento = conta.Vencimento, Status = conta.DataPagamento.HasValue ? "Pago" : "Pendente", DataPagamento = conta.DataPagamento });
                }

                foreach (var conta in todasContasRecorrentes)
                {
                    var dataInicio = new DateTime(conta.DataCriacao.Year, conta.DataCriacao.Month, 1);
                    var dataFim = conta.DataEncerramento ?? DateTime.MaxValue;

                    if (primeiroDiaDoMesCorrente >= dataInicio && primeiroDiaDoMesCorrente <= dataFim)
                    {
                        var overrideInfo = overridesDoMes.FirstOrDefault(o => o.ContaRecorrenteId == conta.Id);
                        var pagamentoInfo = pagamentosDoMes.FirstOrDefault(p => p.ContaRecorrenteId == conta.Id);
                        var diaVencimento = Math.Min(conta.DiaVencimento, DateTime.DaysInMonth(DataCorrente.Year, DataCorrente.Month));
                        ContasDoMes.Add(new ContaDisplay
                        {
                            Id = conta.Id,
                            Categoria = conta.Categoria,
                            Tipo = "Recorrente",
                            Nome = overrideInfo?.NomeOverride ?? conta.Nome,
                            Valor = overrideInfo?.ValorOverride ?? conta.ValorPadrao,
                            Vencimento = new DateTime(DataCorrente.Year, DataCorrente.Month, diaVencimento),
                            Status = pagamentoInfo?.Status ?? "Pendente",
                            DataPagamento = pagamentoInfo?.DataPagamento
                        });
                    }
                }
                CalcularResumo();
            }
            finally { IsBusy = false; }
        }

        private void CalcularResumo()
        {
            TotalPago = ContasDoMes.Where(c => c.Status == "Pago").Sum(c => c.Valor);
            TotalPendente = ContasDoMes.Where(c => c.Status == "Pendente").Sum(c => c.Valor);
            TotalNA = ContasDoMes.Where(c => c.Status == "NA").Sum(c => c.Valor);
        }

        [RelayCommand]
        private async Task PagarContaAsync(ContaDisplay conta)
        {
            if (conta == null || conta.Status == "NA") return;

            conta.Status = "Pago";
            conta.DataPagamento = DateTime.Now;
            CalcularResumo();

            if (conta.Tipo == "Unica")
            {
                var contaDb = await DatabaseService.GetContaUnicaByIdAsync(conta.Id);
                if (contaDb != null)
                {
                    contaDb.DataPagamento = conta.DataPagamento;
                    await DatabaseService.SaveContaUnicaAsync(contaDb);
                }
            }
            else
            {
                await SalvarStatusRecorrente(conta.Id, conta.Vencimento.ToString("yyyy-MM"), "Pago", conta.DataPagamento);
            }
        }

        [RelayCommand]
        private async Task MarcarNaoAplicarAsync(ContaDisplay conta)
        {
            if (conta == null || conta.Tipo != "Recorrente" || conta.Status == "Pago") return;

            conta.Status = "NA";
            conta.DataPagamento = null;
            CalcularResumo();

            await SalvarStatusRecorrente(conta.Id, conta.Vencimento.ToString("yyyy-MM"), "NA");
        }

        [RelayCommand]
        private async Task DesfazerAcaoAsync(ContaDisplay conta)
        {
            if (conta == null) return;

            conta.Status = "Pendente";
            conta.DataPagamento = null;
            CalcularResumo();

            if (conta.Tipo == "Unica")
            {
                var contaDb = await DatabaseService.GetContaUnicaByIdAsync(conta.Id);
                if (contaDb != null)
                {
                    contaDb.DataPagamento = null;
                    await DatabaseService.SaveContaUnicaAsync(contaDb);
                }
            }
            else
            {
                await SalvarStatusRecorrente(conta.Id, conta.Vencimento.ToString("yyyy-MM"), null);
            }
        }

        [RelayCommand]
        private async Task ExcluirContaAsync(ContaDisplay conta)
        {
            if (conta == null) return;

            string alertTitle;
            string alertMessage;

            if (conta.Tipo == "Unica")
            {
                alertTitle = "Excluir Conta";
                alertMessage = $"Deseja excluir permanentemente a conta '{conta.Nome}'?";
            }
            else
            {
                alertTitle = "Inativar Conta Recorrente";
                alertMessage = $"Deseja inativar a conta recorrente '{conta.Nome}'? Ela não aparecerá mais nos meses futuros, mas o histórico será mantido.";
            }

            if (!await Shell.Current.DisplayAlert(alertTitle, alertMessage, "Sim", "Cancelar")) return;

            if (conta.Tipo == "Unica")
            {
                await DatabaseService.DeleteContaUnicaAsync(new ContaUnica { Id = conta.Id });
            }
            else
            {
                var contaDb = await DatabaseService.GetContaRecorrenteByIdAsync(conta.Id);
                if (contaDb != null)
                {
                    contaDb.Ativa = false;
                    contaDb.DataEncerramento = DateTime.Now;
                    await DatabaseService.SaveContaRecorrenteAsync(contaDb);
                }
            }

            ContasDoMes.Remove(conta);
            CalcularResumo();
        }

        private async Task SalvarStatusRecorrente(int contaId, string mesAno, string? novoStatus, DateTime? dataPagamento = null)
        {
            var pagamentoExistente = await DatabaseService.GetPagamentoAsync(contaId, mesAno);
            if (pagamentoExistente != null)
            {
                await DatabaseService.DeletePagamentoRecorrenteAsync(pagamentoExistente);
            }

            if (!string.IsNullOrEmpty(novoStatus))
            {
                await DatabaseService.SavePagamentoRecorrenteAsync(new PagamentoRecorrente
                {
                    ContaRecorrenteId = contaId,
                    MesAno = mesAno,
                    Status = novoStatus,
                    DataPagamento = dataPagamento
                });
            }
        }

        [RelayCommand]
        private async Task GoToEditarContaAsync(ContaDisplay conta)
        {
            if (conta == null) return;
            var query = new ShellNavigationQueryParameters
            {
                { "Id", conta.Id },
                { "Tipo", conta.Tipo },
                { "DataContexto", DataCorrente }
            };
            await Shell.Current.GoToAsync(nameof(AddEditContaPage), query);
        }

        [RelayCommand]
        private async Task GoToAdicionarContaAsync()
        {
            var query = new ShellNavigationQueryParameters
            {
                { "DataContexto", DataCorrente }
            };
            await Shell.Current.GoToAsync(nameof(AddEditContaPage), query);
        }

        [RelayCommand]
        private async Task GoToMesSeguinteAsync() { DataCorrente = DataCorrente.AddMonths(1); await CarregarContasAsync(); }
        [RelayCommand]
        private async Task GoToMesAnteriorAsync() { DataCorrente = DataCorrente.AddMonths(-1); await CarregarContasAsync(); }
    }
}