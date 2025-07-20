using Microsoft.Maui.Graphics;

namespace ControleDeContas.Models
{
    public class CategoriaChartItem
    {
        public string Categoria { get; set; }
        public decimal Valor { get; set; }
        public string ValorFormatado => Valor.ToString("C");
        public double Percentagem { get; set; }
        public string PercentagemFormatada => $"{Percentagem:F1}%";
        public Color Cor { get; set; }
    }
}