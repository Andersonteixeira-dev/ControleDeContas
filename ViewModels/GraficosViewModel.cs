using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using Microcharts;
using SkiaSharp;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeContas.ViewModels
{
    public partial class GraficosViewModel : ObservableObject
    {
        [ObservableProperty] private Chart _gastosPorCategoriaChart;
        [ObservableProperty] private Chart _historicoGastosChart;
        [ObservableProperty] private DateTime _dataCorrente = DateTime.Now;

        public GraficosViewModel() { }

        [RelayCommand]
        private async Task CarregarDadosGraficoAsync()
        {
            string mesAnoAtual = DataCorrente.ToString("yyyy-MM");

            var todasContasUnicas = await DatabaseService.GetContasUnicasAsync();
            var todasContasRecorrentes = await DatabaseService.GetContasRecorrentesAsync();
            var todosPagamentos = await DatabaseService.GetAllPagamentosRecorrentesAsync();
            var todosOverrides = await DatabaseService.GetAllOverridesRecorrentesAsync();

            // Gráfico de Donut
            var contasPagasNoMes = new List<ContaDisplay>();
            var unicasPagas = todasContasUnicas.Where(c => c.DataPagamento.HasValue && c.Vencimento.ToString("yyyy-MM") == mesAnoAtual);
            foreach (var conta in unicasPagas) contasPagasNoMes.Add(new ContaDisplay { Categoria = conta.Categoria, Valor = conta.Valor });
            var recorrentesPagas = todosPagamentos.Where(p => p.Status == "Pago" && p.MesAno == mesAnoAtual);
            foreach (var pagamento in recorrentesPagas)
            {
                var contaBase = todasContasRecorrentes.FirstOrDefault(c => c.Id == pagamento.ContaRecorrenteId);
                if (contaBase != null)
                {
                    var overrideInfo = todosOverrides.FirstOrDefault(o => o.ContaRecorrenteId == contaBase.Id && o.MesAno == mesAnoAtual);
                    contasPagasNoMes.Add(new ContaDisplay { Categoria = contaBase.Categoria, Valor = overrideInfo?.ValorOverride ?? contaBase.ValorPadrao });
                }
            }

            var donutEntries = contasPagasNoMes.Where(c => c.Valor > 0).GroupBy(c => c.Categoria ?? "Outros").Select(grupo => new ChartEntry((float)grupo.Sum(c => c.Valor)) { Label = grupo.Key, ValueLabel = grupo.Sum(c => c.Valor).ToString("C"), Color = SKColor.Parse(GetNextColor()), TextColor = SKColor.Parse("#000000") }).ToList();
            GastosPorCategoriaChart = new DonutChart { Entries = donutEntries, LabelTextSize = 24f, LabelMode = LabelMode.RightOnly, IsAnimated = true };

            // Gráfico de Barras
            var historicoEntries = new List<ChartEntry>();
            for (int i = 5; i >= 0; i--)
            {
                var dataMes = DataCorrente.AddMonths(-i);
                string mesAnoHistorico = dataMes.ToString("yyyy-MM");
                decimal totalPagoNoMes = 0;
                totalPagoNoMes += todasContasUnicas.Where(c => c.DataPagamento.HasValue && c.Vencimento.ToString("yyyy-MM") == mesAnoHistorico).Sum(c => c.Valor);
                var pagamentosDoMesHistorico = todosPagamentos.Where(p => p.Status == "Pago" && p.MesAno == mesAnoHistorico);
                foreach (var pagamento in pagamentosDoMesHistorico)
                {
                    var contaBase = todasContasRecorrentes.FirstOrDefault(c => c.Id == pagamento.ContaRecorrenteId);
                    if (contaBase != null)
                    {
                        var overrideInfo = todosOverrides.FirstOrDefault(o => o.ContaRecorrenteId == contaBase.Id && o.MesAno == mesAnoHistorico);
                        totalPagoNoMes += overrideInfo?.ValorOverride ?? contaBase.ValorPadrao;
                    }
                }
                historicoEntries.Add(new ChartEntry((float)totalPagoNoMes) { Label = dataMes.ToString("MMM/yy"), ValueLabel = totalPagoNoMes.ToString("C"), Color = SKColor.Parse("#007AFF") });
            }
            HistoricoGastosChart = new BarChart { Entries = historicoEntries, LabelTextSize = 24f, IsAnimated = true };
        }

        private readonly string[] _colors = { "#007AFF", "#34C759", "#FF9500", "#FF3B30", "#AF52DE", "#5AC8FA", "#8E8E93" };
        private int _colorIndex = 0;
        private string GetNextColor()
        {
            var color = _colors[_colorIndex];
            _colorIndex = (_colorIndex + 1) % _colors.Length;
            return color;
        }
    }
}