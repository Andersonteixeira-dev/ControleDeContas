using SQLite;

namespace ControleDeContas.Models
{
    public class ContaUnica
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal Valor { get; set; }
        public DateTime Vencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Categoria { get; set; }
    }
}