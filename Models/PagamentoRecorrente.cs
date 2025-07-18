using SQLite;

namespace ControleDeContas.Models
{
    public class PagamentoRecorrente
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ContaRecorrenteId { get; set; }
        public string MesAno { get; set; } // Formato "YYYY-MM"
        public string Status { get; set; } // "Pago" ou "NA"
        public DateTime? DataPagamento { get; set; }
    }
}