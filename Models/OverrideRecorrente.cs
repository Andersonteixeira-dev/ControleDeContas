using SQLite;

namespace ControleDeContas.Models
{
    public class OverrideRecorrente
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ContaRecorrenteId { get; set; }
        public string MesAno { get; set; } // "YYYY-MM"
        public decimal? ValorOverride { get; set; }
        public string NomeOverride { get; set; }
    }
}