using ControleDeContas.Models;
using SQLite;
using System;
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

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Contas.db3");

            _db = new SQLiteAsyncConnection(dbPath);

            await _db.CreateTableAsync<ContaUnica>();
            await _db.CreateTableAsync<ContaRecorrente>();
            await _db.CreateTableAsync<PagamentoRecorrente>();
            await _db.CreateTableAsync<OverrideRecorrente>();
        }

        // -------- Contas Únicas --------
        public static async Task<List<ContaUnica>> GetContasUnicasAsync()
        {
            await InitializeAsync();
            return await _db.Table<ContaUnica>().ToListAsync();
        }

        public static async Task<ContaUnica> GetContaUnicaByIdAsync(int id)
        {
            await InitializeAsync();
            return await _db.Table<ContaUnica>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public static async Task<int> SaveContaUnicaAsync(ContaUnica conta)
        {
            await InitializeAsync();
            return conta.Id != 0 ? await _db.UpdateAsync(conta) : await _db.InsertAsync(conta);
        }

        public static async Task<int> DeleteContaUnicaAsync(ContaUnica conta)
        {
            await InitializeAsync();
            return await _db.DeleteAsync(conta);
        }

        // -------- Contas Recorrentes --------
        public static async Task<List<ContaRecorrente>> GetContasRecorrentesAsync()
        {
            await InitializeAsync();
            return await _db.Table<ContaRecorrente>().ToListAsync();
        }

        public static async Task<ContaRecorrente> GetContaRecorrenteByIdAsync(int id)
        {
            await InitializeAsync();
            return await _db.Table<ContaRecorrente>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public static async Task<int> SaveContaRecorrenteAsync(ContaRecorrente conta)
        {
            await InitializeAsync();
            return conta.Id != 0 ? await _db.UpdateAsync(conta) : await _db.InsertAsync(conta);
        }

        public static async Task<int> DeleteContaRecorrenteAsync(ContaRecorrente conta)
        {
            await InitializeAsync();
            return await _db.DeleteAsync(conta);
        }

        // -------- Pagamentos Recorrentes --------
        public static async Task<List<PagamentoRecorrente>> GetPagamentosDoMesAsync(string mesAno)
        {
            await InitializeAsync();
            return await _db.Table<PagamentoRecorrente>().Where(p => p.MesAno == mesAno).ToListAsync();
        }

        public static async Task<PagamentoRecorrente> GetPagamentoAsync(int contaId, string mesAno)
        {
            await InitializeAsync();
            return await _db.Table<PagamentoRecorrente>()
                .Where(p => p.ContaRecorrenteId == contaId && p.MesAno == mesAno)
                .FirstOrDefaultAsync();
        }

        public static async Task<int> SavePagamentoRecorrenteAsync(PagamentoRecorrente pagamento)
        {
            await InitializeAsync();
            // Esta lógica de update/insert é importante para o "Desfazer"
            return pagamento.Id != 0 ? await _db.UpdateAsync(pagamento) : await _db.InsertAsync(pagamento);
        }

        public static async Task<int> DeletePagamentoRecorrenteAsync(PagamentoRecorrente pagamento)
        {
            await InitializeAsync();
            return await _db.DeleteAsync(pagamento);
        }

        public static async Task<List<PagamentoRecorrente>> GetAllPagamentosRecorrentesAsync()
        {
            await InitializeAsync();
            return await _db.Table<PagamentoRecorrente>().ToListAsync();
        }

        // -------- Overrides Recorrentes --------
        public static async Task<List<OverrideRecorrente>> GetOverridesDoMesAsync(string mesAno)
        {
            await InitializeAsync();
            return await _db.Table<OverrideRecorrente>().Where(o => o.MesAno == mesAno).ToListAsync();
        }

        public static async Task<int> SaveOverrideRecorrenteAsync(OverrideRecorrente ov)
        {
            await InitializeAsync();
            return await _db.InsertOrReplaceAsync(ov);
        }

        public static async Task<List<OverrideRecorrente>> GetAllOverridesRecorrentesAsync()
        {
            await InitializeAsync();
            return await _db.Table<OverrideRecorrente>().ToListAsync();
        }
    }
}