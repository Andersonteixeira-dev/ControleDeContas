using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleDeContas.Models;
using ControleDeContas.Services;
using Microcharts;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ControleDeContas.ViewModels
{
    public partial class GraficosViewModel : ObservableObject
    {
        [ObservableProperty] private Chart _pizzaChart;
        [ObservableProperty] private Chart _barChart;
        [ObservableProperty] private DateTime _dataCorrente = DateTime.Now;
        [ObservableProperty] private decimal _totalGastoNoMes;
        [ObservableProperty] private bool _temDadosParaExibir;
        public ObservableCollection<CategoriaChartItem> LegendaPizza { get; } = new();

        private readonly Color[] _coresGrafico = { Color.FromArgb("#2ECC71"), Color.FromArgb("#3498DB"), Color.FromArgb("#9B59B6"), Color.FromArgb("#F1C40F"), Color.FromArgb("#E67E22"), Color.FromArgb("#E74C3C"), Color.FromArgb("#95A5A6"), Color.FromArgb("#1ABC9C") };

        [RelayCommand]
        private async Task CarregarDadosGraficoAsync()
        {
            // Lógica do Gráfico de Pizza (Focada no Mês Corrente)
            var contasPagasPizza = await ObterContasPagasDoMes(DataCorrente);
            TotalGastoNoMes = contasPagasPizza.Sum(c => c.Valor);
            TemDadosParaExibir = TotalGastoNoMes > 0;

            LegendaPizza.Clear();
            var entriesPizza = new List<ChartEntry>();
            if (TemDadosParaExibir)
            {
                var dadosPorCategoria = contasPagasPizza.GroupBy(c => c.Categoria ?? "Outros").Select(g => new { Categoria = g.Key, Total = g.Sum(c => c.Valor) }).OrderByDescending(g => g.Total).ToList();
                int corIndex = 0;
                foreach (var dado in dadosPorCategoria)
                {
                    var cor = _coresGrafico[corIndex % _coresGrafico.Length];
                    entriesPizza.Add(new ChartEntry((float)dado.Total) { Color = cor.ToSKColor() });
                    LegendaPizza.Add(new CategoriaChartItem { Categoria = dado.Categoria, Valor = dado.Total, Percentagem = TotalGastoNoMes > 0 ? (double)(dado.Total / TotalGastoNoMes * 100) : 0, Cor = cor });
                    corIndex++;
                }
            }
            PizzaChart = new DonutChart { Entries = entriesPizza, IsAnimated = entriesPizza.Count > 1, HoleRadius = 0.5f, BackgroundColor = SKColors.Transparent };

            // Lógica do Gráfico de Barras (Dinâmica)
            var historicoEntries = new List<ChartEntry>();
            for (int i = 5; i >= 0; i--)
            {
                var dataMes = DataCorrente.AddMonths(-i);
                var contasPagasMes = await ObterContasPagasDoMes(dataMes);
                var totalPagoNoMes = contasPagasMes.Sum(c => c.Valor);
                historicoEntries.Add(new ChartEntry((float)totalPagoNoMes) { Label = dataMes.ToString("MMM/yy"), ValueLabel = totalPagoNoMes.ToString("C"), Color = SKColor.Parse("#3498DB") });
            }
            BarChart = new BarChart { Entries = historicoEntries, IsAnimated = true, LabelTextSize = 24f, ValueLabelOrientation = Orientation.Horizontal };
        }

        private async Task<List<ContaDisplay>> ObterContasPagasDoMes(DateTime dataReferencia)
        {
            string mesAno = dataReferencia.ToString("yyyy-MM");
            var contasUnicas = await DatabaseService.GetContasUnicasAsync();
            var contasRecorrentes = await DatabaseService.GetContasRecorrentesAsync();
            var todosPagamentos = await DatabaseService.GetAllPagamentosRecorrentesAsync();
            var todosOverrides = await DatabaseService.GetAllOverridesRecorrentesAsync();

            var contasPagas = new List<ContaDisplay>();
            contasPagas.AddRange(contasUnicas.Where(c => c.DataPagamento.HasValue && c.Vencimento.Year == dataReferencia.Year && c.Vencimento.Month == dataReferencia.Month).Select(c => new ContaDisplay { Categoria = c.Categoria, Valor = c.Valor }));
            var recorrentesPagas = todosPagamentos.Where(p => p.Status == "Pago" && p.MesAno == mesAno);
            foreach (var pagamento in recorrentesPagas)
            {
                var contaBase = contasRecorrentes.FirstOrDefault(c => c.Id == pagamento.ContaRecorrenteId);
                if (contaBase != null)
                {
                    var overrideInfo = todosOverrides.FirstOrDefault(o => o.ContaRecorrenteId == contaBase.Id && o.MesAno == mesAno);
                    contasPagas.Add(new ContaDisplay { Categoria = contaBase.Categoria, Valor = overrideInfo?.ValorOverride ?? contaBase.ValorPadrao });
                }
            }
            return contasPagas;
        }

        [RelayCommand]
        private async Task GoToMesAnteriorAsync() { DataCorrente = DataCorrente.AddMonths(-1); await CarregarDadosGraficoAsync(); }
        [RelayCommand]
        private async Task GoToMesSeguinteAsync() { DataCorrente = DataCorrente.AddMonths(1); await CarregarDadosGraficoAsync(); }
    }
}