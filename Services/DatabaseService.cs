using ControleDeContas.Models;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ControleDeContas.Services
{
    public static class DatabaseService
    {
        private static SQLiteAsyncConnection _db;

        public static async Task InitializeAsync()
        {
            if (_db != null) return;
            var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Contas.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<ContaUnica>();
            await _db.CreateTableAsync<ContaRecorrente>();
            await _db.CreateTableAsync<PagamentoRecorrente>();
            await _db.CreateTableAsync<OverrideRecorrente>();
        }

        public static Task<List<ContaUnica>> GetContasUnicasAsync() => _db.Table<ContaUnica>().ToListAsync();
        public static Task<ContaUnica> GetContaUnicaByIdAsync(int id) => _db.Table<ContaUnica>().Where(c => c.Id == id).FirstOrDefaultAsync();
        public static Task<int> SaveContaUnicaAsync(ContaUnica conta) => conta.Id != 0 ? _db.UpdateAsync(conta) : _db.InsertAsync(conta);
        public static Task<int> DeleteContaUnicaAsync(ContaUnica conta) => _db.DeleteAsync(conta);

        public static Task<List<ContaRecorrente>> GetContasRecorrentesAsync() => _db.Table<ContaRecorrente>().ToListAsync();
        public static Task<ContaRecorrente> GetContaRecorrenteByIdAsync(int id) => _db.Table<ContaRecorrente>().Where(c => c.Id == id).FirstOrDefaultAsync();
        public static Task<int> SaveContaRecorrenteAsync(ContaRecorrente conta) => conta.Id != 0 ? _db.UpdateAsync(conta) : _db.InsertAsync(conta);
        public static Task<int> DeleteContaRecorrenteAsync(ContaRecorrente conta) => _db.DeleteAsync(conta);

        public static Task<List<PagamentoRecorrente>> GetPagamentosDoMesAsync(string mesAno) => _db.Table<PagamentoRecorrente>().Where(p => p.MesAno == mesAno).ToListAsync();
        public static Task<PagamentoRecorrente> GetPagamentoAsync(int contaId, string mesAno) => _db.Table<PagamentoRecorrente>().Where(p => p.ContaRecorrenteId == contaId && p.MesAno == mesAno).FirstOrDefaultAsync();
        public static Task<int> SavePagamentoRecorrenteAsync(PagamentoRecorrente pagamento) => _db.InsertAsync(pagamento);
        public static Task<int> DeletePagamentoRecorrenteAsync(PagamentoRecorrente pagamento) => _db.DeleteAsync(pagamento);
        public static Task<List<PagamentoRecorrente>> GetAllPagamentosRecorrentesAsync() => _db.Table<PagamentoRecorrente>().ToListAsync();

        public static Task<List<OverrideRecorrente>> GetOverridesDoMesAsync(string mesAno) => _db.Table<OverrideRecorrente>().Where(o => o.MesAno == mesAno).ToListAsync();
        public static Task<int> SaveOverrideRecorrenteAsync(OverrideRecorrente ov) => _db.InsertOrReplaceAsync(ov);
        public static Task<int> DeleteOverrideRecorrenteAsync(OverrideRecorrente ov) => _db.DeleteAsync(ov);
        public static Task<List<OverrideRecorrente>> GetAllOverridesRecorrentesAsync() => _db.Table<OverrideRecorrente>().ToListAsync();

        public static Task<int> DeleteFutureOverridesAsync(int contaId, string mesAno)
        {
            // Usamos SQL direto para garantir a fiabilidade
            return _db.ExecuteAsync("DELETE FROM OverrideRecorrente WHERE ContaRecorrenteId = ? AND MesAno >= ?", contaId, mesAno);
        }
    }
}