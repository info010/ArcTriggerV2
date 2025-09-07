using ArcTriggerV2.Core.Abstractions;
using ArcTriggerV2.Core.Models;
using System.Collections.Concurrent;
using IBApi;

namespace ArcTriggerV2.Core.Services
{


    public sealed class ContractService : BaseService, IContractService
    {
        private int _nextReqId = 1;
        private int NextId() => Interlocked.Increment(ref _nextReqId);
        // reqId -> TCS ve buffer’lar
        private readonly ConcurrentDictionary<int, TaskCompletionSource<IReadOnlyList<SymbolMatch>>> _symTcs = new();
        private readonly ConcurrentDictionary<int, (TaskCompletionSource<IReadOnlyList<ContractInfo>> tcs, List<ContractInfo> buf)> _cdTcs = new();
        private readonly ConcurrentDictionary<int, (TaskCompletionSource<IReadOnlyList<OptionChainParams>> tcs, List<OptionChainParams> buf)> _optTcs = new();

        // ---- Public API

        public Task<IReadOnlyList<SymbolMatch>> SearchSymbolsAsync(string query, CancellationToken ct = default)
        {
            var reqId = NextId();
            var tcs = new TaskCompletionSource<IReadOnlyList<SymbolMatch>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _symTcs[reqId] = tcs;
            ct.Register(() => tcs.TrySetCanceled(ct));
            Client.reqMatchingSymbols(reqId, query);
            return tcs.Task;
        }

        public Task<IReadOnlyList<ContractInfo>> GetContractDetailsAsync(Contract key, CancellationToken ct = default)
        {
            var reqId = NextId();
            var tcs = new TaskCompletionSource<IReadOnlyList<ContractInfo>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _cdTcs[reqId] = (tcs, new List<ContractInfo>(8));
            ct.Register(() => tcs.TrySetCanceled(ct));
            Client.reqContractDetails(reqId, key);
            return tcs.Task;
        }

        public Task<IReadOnlyList<OptionChainParams>> GetOptionParamsAsync(
            int underlyingConId, string symbol, string underlyingSecType = "STK", string futFopExchange = "", CancellationToken ct = default)
        {
            var reqId = NextId();
            var tcs = new TaskCompletionSource<IReadOnlyList<OptionChainParams>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _optTcs[reqId] = (tcs, new List<OptionChainParams>(4));
            ct.Register(() => tcs.TrySetCanceled(ct));

            Client.reqSecDefOptParams(reqId, symbol, futFopExchange, underlyingSecType, underlyingConId);
            return tcs.Task;
        }


        public async Task<int> ResolveOptionConidAsync(
            string symbol, string exchange, string right, string yyyymmdd, double strike,
            string? tradingClass = null, string? multiplier = null, CancellationToken ct = default)
        {
            var c = new Contract
            {
                Symbol = symbol,
                SecType = "OPT",
                Right = right,                                 // "C" | "P"
                LastTradeDateOrContractMonth = yyyymmdd,       // "YYYYMMDD" veya "YYYYMM"
                Strike = strike,
                Exchange = exchange,
                TradingClass = tradingClass,
                Multiplier = multiplier
            };

            var list = await GetContractDetailsAsync(c, ct);
            var hit = list.FirstOrDefault() ?? throw new InvalidOperationException("Tekilleşmedi: contract bulunamadı.");
            return hit.ConId;
        }

        // ---- EWrapper overrides (yalnızca gerekenler)

        public override void symbolSamples(int reqId, ContractDescription[] descs)
        {
            if (_symTcs.TryRemove(reqId, out var tcs))
            {
                var list = descs.Select(d => new SymbolMatch(
                    d.Contract.Symbol,
                    d.Contract.SecType,
                    d.Contract.PrimaryExch,
                    d.Contract.ConId,
                    (d.DerivativeSecTypes ?? Array.Empty<string>()).ToList()
                )).ToList();
                tcs.TrySetResult(list);
            }
        }

        public override void contractDetails(int reqId, ContractDetails cd)
        {
            if (_cdTcs.TryGetValue(reqId, out var box))
            {
                box.buf.Add(new ContractInfo(
                    cd.Contract.ConId,
                    cd.Contract.Symbol,
                    cd.Contract.SecType,
                    cd.Contract.Exchange,
                    cd.Contract.Currency,
                    cd.Contract.LocalSymbol,
                    cd.UnderConId == 0 ? null : cd.UnderConId,
                    cd.Contract.TradingClass,
                    cd.Contract.Multiplier,
                    cd.LongName,
                    cd.Contract.PrimaryExch
                ));
            }
        }

        public override void contractDetailsEnd(int reqId)
        {
            if (_cdTcs.TryRemove(reqId, out var box))
                box.tcs.TrySetResult(box.buf);
        }

        public override void securityDefinitionOptionParameter(
            int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier,
            HashSet<string> expirations, HashSet<double> strikes)
        {
            if (_optTcs.TryGetValue(reqId, out var box))
            {
                box.buf.Add(new OptionChainParams(
                    exchange,
                    underlyingConId,
                    tradingClass,
                    multiplier,
                    expirations ?? new HashSet<string>(),
                    strikes ?? new HashSet<double>()));
            }
        }


        public override void securityDefinitionOptionParameterEnd(int reqId)
        {
            if (_optTcs.TryRemove(reqId, out var box))
                box.tcs.TrySetResult(box.buf);
        }


        public override void error(int id, int errorCode, string errorMsg)
        {
            // İlgili TCS’e hatayı yansıt. Bulunamazsa BaseService’in genel error işleyişi çalışır.
            if (_symTcs.Remove(id, out var t1)) { t1.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            if (_cdTcs.Remove(id, out var t2)) { t2.tcs.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            if (_optTcs.Remove(id, out var t3)) { t3.tcs.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            base.error(id, errorCode, errorMsg);
        }

        // IAsyncDisposable BaseService’de. Ek kaynak yok.
    }
}