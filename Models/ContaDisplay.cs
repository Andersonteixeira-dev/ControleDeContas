using CommunityToolkit.Mvvm.ComponentModel;

namespace ControleDeContas.Models
{
    public partial class ContaDisplay : ObservableObject
    {
        public int Id { get; set; }
        public string Tipo { get; set; }
        public DateTime Vencimento { get; set; }
        public string Categoria { get; set; }

        [ObservableProperty] private string _nome;
        [ObservableProperty] private decimal _valor;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(AcaoRealizada), nameof(IsPendente), nameof(IsRecorrentePendente), nameof(StatusDetalhado))] private string _status;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(StatusDetalhado))] private DateTime? _dataPagamento;

        // Propriedades calculadas para controlar a visibilidade dos botões
        public bool AcaoRealizada => Status == "Pago" || Status == "NA";
        public bool IsPendente => Status == "Pendente";
        public bool IsUnicaPendente => Tipo == "Unica" && IsPendente;
        public bool IsRecorrentePendente => Tipo == "Recorrente" && IsPendente;

        public string StatusDetalhado
        {
            get
            {
                if (Status == "Pago" && DataPagamento.HasValue) return $"✓ Pago em {DataPagamento.Value:dd/MM/yyyy}";
                if (Status == "NA") return "Não se aplica este mês";
                return string.Empty;
            }
        }
    }
}