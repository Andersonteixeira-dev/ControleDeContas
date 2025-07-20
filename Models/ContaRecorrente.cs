using SQLite;
using System; // Adicionado para usar o DateTime

namespace ControleDeContas.Models
{
    public class ContaRecorrente
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal ValorPadrao { get; set; }
        public int DiaVencimento { get; set; }
        public string Categoria { get; set; }
        public bool Ativa { get; set; } = true;
        public DateTime DataCriacao { get; set; }

        // --- PROPRIEDADE QUE FALTAVA ---
        public DateTime? DataEncerramento { get; set; } // Marca o fim da vigência
    }
}