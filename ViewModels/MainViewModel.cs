using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using ControleDeContas.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeContas.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<ContaDisplay> ContasDoMes { get; set; } = new();
        [ObservableProperty] private DateTime _dataCorrente = DateTime.Now;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotBusy))] private bool _isBusy;
        public bool IsNotBusy => !IsBusy;
        [ObservableProperty] private decimal _totalPendente;
        [ObservableProperty] private decimal _totalPago;
        [ObservableProperty] private decimal _totalNA;

        public MainViewModel() { }

        [RelayCommand]
        public async Task CarregarContasAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                ContasDoMes.Clear();
                string mesAno = DataCorrente.ToString("yyyy-MM");

                var contasUnicas = await DatabaseService.GetContasUnicasAsync();
                var todasContasRecorrentes = await DatabaseService.GetContasRecorrentesAsync();
                var pagamentosDoMes = await DatabaseService.GetPagamentosDoMesAsync(mesAno);
                var overridesDoMes = await DatabaseService.GetOverridesDoMesAsync(mesAno);

                var unicasDoMes = contasUnicas.Where(c => c.Vencimento.ToString("yyyy-MM") == mesAno);
                foreach (var conta in unicasDoMes) ContasDoMes.Add(new ContaDisplay { Id = conta.Id, Categoria = conta.Categoria, Tipo = "Unica", Nome = conta.Nome, Valor = conta.Valor, Vencimento = conta.Vencimento, Status = conta.DataPagamento.HasValue ? "Pago" : "Pendente", DataPagamento = conta.DataPagamento });

                var primeiroDiaDoMesCorrente = new DateTime(DataCorrente.Year, DataCorrente.Month, 1);
                foreach (var conta in todasContasRecorrentes)
                {
                    var primeiroDiaCriacao = new DateTime(conta.DataCriacao.Year, conta.DataCriacao.Month, 1);
                    var pagamentoInfo = pagamentosDoMes.FirstOrDefault(p => p.ContaRecorrenteId == conta.Id);

                    // LÓGICA CORRIGIDA: Mostra a conta se (ela estiver ativa E o mês atual for >= o mês de criação) OU se houver um pagamento registado no passado
                    if ((conta.Ativa && primeiroDiaDoMesCorrente >= primeiroDiaCriacao) || pagamentoInfo != null)
                    {
                        var overrideInfo = overridesDoMes.FirstOrDefault(o => o.ContaRecorrenteId == conta.Id);
                        ContasDoMes.Add(new ContaDisplay { Id = conta.Id, Categoria = conta.Categoria, Tipo = "Recorrente", Nome = overrideInfo?.NomeOverride ?? conta.Nome, Valor = overrideInfo?.ValorOverride ?? conta.ValorPadrao, Vencimento = new DateTime(DataCorrente.Year, DataCorrente.Month, conta.DiaVencimento), Status = pagamentoInfo?.Status ?? "Pendente", DataPagamento = pagamentoInfo?.DataPagamento });
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

        private async Task SalvarStatusRecorrente(int contaId, string mesAno, string novoStatus, DateTime? dataPagamento = null)
        {
            var pagamentoExistente = await DatabaseService.GetPagamentoAsync(contaId, mesAno);
            if (pagamentoExistente != null) await DatabaseService.DeletePagamentoRecorrenteAsync(pagamentoExistente);

            if (!string.IsNullOrEmpty(novoStatus))
            {
                await DatabaseService.SavePagamentoRecorrenteAsync(new PagamentoRecorrente { ContaRecorrenteId = contaId, MesAno = mesAno, Status = novoStatus, DataPagamento = dataPagamento });
            }
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
                if (contaDb != null) { contaDb.DataPagamento = conta.DataPagamento; await DatabaseService.SaveContaUnicaAsync(contaDb); }
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
                if (contaDb != null) { contaDb.DataPagamento = null; await DatabaseService.SaveContaUnicaAsync(contaDb); }
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

            if (conta.Tipo == "Unica")
            {
                if (!await Shell.Current.DisplayAlert("Excluir Conta", $"Deseja excluir permanentemente a conta '{conta.Nome}'?", "Sim, Excluir", "Cancelar")) return;
                ContasDoMes.Remove(conta);
                await DatabaseService.DeleteContaUnicaAsync(new ContaUnica { Id = conta.Id });
            }
            else
            {
                if (!await Shell.Current.DisplayAlert("Inativar Conta Recorrente", $"Deseja inativar a conta recorrente '{conta.Nome}'? Ela não aparecerá mais nos meses futuros, mas o histórico será mantido.", "Sim, Inativar", "Cancelar")) return;
                ContasDoMes.Remove(conta);
                var contaDb = await DatabaseService.GetContaRecorrenteByIdAsync(conta.Id);
                if (contaDb != null)
                {
                    contaDb.Ativa = false;
                    await DatabaseService.SaveContaRecorrenteAsync(contaDb);
                }
            }
            CalcularResumo();
        }

        [RelayCommand]
        private async Task GoToEditarContaAsync(ContaDisplay conta)
        {
            if (conta == null) return;
            if (conta.Tipo == "Unica")
            {
                await Shell.Current.GoToAsync($"{nameof(AddEditContaPage)}?Id={conta.Id}&Tipo={conta.Tipo}");
            }
            else
            {
                string acao = await Shell.Current.DisplayActionSheet("Que tipo de edição deseja fazer?", "Cancelar", null, "Ajustar valor apenas para este mês", "Atualizar dados base (valor, nome, etc.)");
                if (acao == "Ajustar valor apenas para este mês")
                {
                    await AjustarValorRecorrenteAsync(conta);
                }
                else if (acao == "Atualizar dados base (valor, nome, etc.)")
                {
                    await Shell.Current.GoToAsync($"{nameof(AddEditContaPage)}?Id={conta.Id}&Tipo={conta.Tipo}");
                }
            }
        }

        private async Task AjustarValorRecorrenteAsync(ContaDisplay conta)
        {
            var novoValorStr = await Shell.Current.DisplayPromptAsync("Ajustar Valor Mensal", $"Qual o valor para a conta '{conta.Nome}' neste mês?", "Salvar", "Cancelar", conta.Valor.ToString(CultureInfo.InvariantCulture), keyboard: Keyboard.Default);
            if (!string.IsNullOrWhiteSpace(novoValorStr) && decimal.TryParse(novoValorStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal novoValor))
            {
                conta.Valor = novoValor;
                CalcularResumo();

                string mesAno = DataCorrente.ToString("yyyy-MM");
                var overrideExistente = (await DatabaseService.GetOverridesDoMesAsync(mesAno)).FirstOrDefault(o => o.ContaRecorrenteId == conta.Id) ?? new OverrideRecorrente { ContaRecorrenteId = conta.Id, MesAno = mesAno };
                overrideExistente.ValorOverride = novoValor;
                await DatabaseService.SaveOverrideRecorrenteAsync(overrideExistente);
            }
        }

        [RelayCommand]
        private async Task GoToAdicionarContaAsync() => await Shell.Current.GoToAsync($"{nameof(AddEditContaPage)}?Tipo=Unica");

        [RelayCommand]
        private async Task GoToMesSeguinteAsync() { DataCorrente = DataCorrente.AddMonths(1); await CarregarContasAsync(); }

        [RelayCommand]
        private async Task GoToMesAnteriorAsync() { DataCorrente = DataCorrente.AddMonths(-1); await CarregarContasAsync(); }
    }
}