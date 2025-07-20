namespace ControleDeContas.Models
{
    public class ResumoCategoriaMes
    {
        public string Categoria { get; set; } = "";
        public decimal Total { get; set; }
        public decimal Pago { get; set; }
        public decimal Pendente { get; set; }
        public decimal NA { get; set; }
    }
}
