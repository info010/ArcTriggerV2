using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using ArcTriggerV2.Core.Abstractions; // SymbolMatch, ContractInfo, OptionChainParams modellerin buradaysa
using ArcTriggerV2.Core.Models;

namespace ArcTriggerV2.Core.Services
{
    public sealed class TwsService : BaseService, IContractService
    {
        // ---- reqId/Task eşlemesi
        private int _nextReqId = 0;
        private int NextReqId() => Interlocked.Increment(ref _nextReqId);

        private readonly ConcurrentDictionary<int, TaskCompletionSource<IReadOnlyList<SymbolMatch>>> _symTcs = new();
        private readonly ConcurrentDictionary<int, (TaskCompletionSource<IReadOnlyList<ContractInfo>> tcs, List<ContractInfo> buf)> _cdTcs = new();
        private readonly ConcurrentDictionary<int, (TaskCompletionSource<IReadOnlyList<OptionChainParams>> tcs, List<OptionChainParams> buf)> _optTcs = new();

        // ---- orderId/ACK yönetimi
        private volatile int _nextOrderId;
        private TaskCompletionSource<int>? _nextOrderIdTcs;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _orderAck = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _cancelAck = new();

        // ---- Bağlantı
        public async Task ConnectAsync(string host, int port, int clientId, CancellationToken ct = default)
        {
            _nextOrderIdTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Connect(host, port, clientId);
            using var _ = ct.Register(() => _nextOrderIdTcs.TrySetCanceled(ct));
            await _nextOrderIdTcs.Task.ConfigureAwait(false); // nextValidId bekle
        }

        public override void nextValidId(int orderId)
        {
            _nextOrderId = orderId;
            _nextOrderIdTcs?.TrySetResult(orderId);
        }

        // =======================
        // CONTRACT API (async)
        // =======================

        public Task<IReadOnlyList<SymbolMatch>> SearchSymbolsAsync(string query, CancellationToken ct = default)
        {
            var reqId = NextReqId();
            var tcs = new TaskCompletionSource<IReadOnlyList<SymbolMatch>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _symTcs[reqId] = tcs;
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
            Client.reqMatchingSymbols(reqId, query);
            return tcs.Task;
        }

        public Task<IReadOnlyList<ContractInfo>> GetContractDetailsAsync(Contract key, CancellationToken ct = default)
        {
            var reqId = NextReqId();
            var tcs = new TaskCompletionSource<IReadOnlyList<ContractInfo>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _cdTcs[reqId] = (tcs, new List<ContractInfo>(8));
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
            Client.reqContractDetails(reqId, key);
            return tcs.Task;
        }

        public Task<IReadOnlyList<OptionChainParams>> GetOptionParamsAsync(
            int underlyingConId, string symbol, string underlyingSecType = "STK", string futFopExchange = "", CancellationToken ct = default)
        {
            var reqId = NextReqId();
            var tcs = new TaskCompletionSource<IReadOnlyList<OptionChainParams>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _optTcs[reqId] = (tcs, new List<OptionChainParams>(4));
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
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
                Right = right,
                LastTradeDateOrContractMonth = yyyymmdd,
                Strike = strike,
                Exchange = exchange,
                TradingClass = tradingClass,
                Multiplier = multiplier
            };

            var list = await GetContractDetailsAsync(c, ct).ConfigureAwait(false);
            var hit = list.FirstOrDefault() ?? throw new InvalidOperationException("Tekilleşmedi: contract bulunamadı.");
            return hit.ConId;
        }

        // =======================
        // TRADE API (async)
        // =======================

        public async Task<int> PlaceOrderAsync(Contract contract, Order order, CancellationToken ct = default)
        {
            var id = GetNextOrderId();
            var ack = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _orderAck[id] = ack;
            using var _ = ct.Register(() => ack.TrySetCanceled(ct));
            Client.placeOrder(id, contract, order);
            await ack.Task.ConfigureAwait(false); // ilk status/openOrder bekle
            return id;
        }

        public async Task CancelOrderAsync(int orderId, CancellationToken ct = default)
        {
            var ack = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _cancelAck[orderId] = ack;
            using var _ = ct.Register(() => ack.TrySetCanceled(ct));
            Client.cancelOrder(orderId);
            await ack.Task.ConfigureAwait(false); // "Cancelled" status bekle
        }

        public Task<int> PlaceProfitTakingAsync(Contract contract, int qty, double limitPrice, string tif = "DAY", string? account = null, bool close = true, CancellationToken ct = default)
        {
            var o = new Order
            {
                Action = "SELL",
                OrderType = "LMT",
                TotalQuantity = qty,
                LmtPrice = limitPrice,
                Tif = tif,
                OpenClose = close ? "C" : "O",
                Account = account ?? string.Empty
            };
            return PlaceOrderAsync(contract, o, ct);
        }

        public Task<int> PlaceBreakevenAsync(Contract contract, int qty, string tif = "DAY", string? account = null, bool close = true, CancellationToken ct = default)
        {
            var o = new Order
            {
                Action = "SELL",
                OrderType = "MKT",
                TotalQuantity = qty,
                Tif = tif,
                OpenClose = close ? "C" : "O",
                Account = account ?? string.Empty
            };
            return PlaceOrderAsync(contract, o, ct);
        }

        public Task<int> PlaceStopMarketAsync(Contract contract, int qty, double stopTrigger, string tif = "DAY", bool outsideRth = false, string? account = null, bool close = true, CancellationToken ct = default)
        {
            var o = new Order
            {
                Action = "SELL",
                OrderType = "STP",
                TotalQuantity = qty,
                AuxPrice = stopTrigger,     // tetik
                Tif = tif,
                OutsideRth = outsideRth,
                OpenClose = close ? "C" : "O",
                Account = account ?? string.Empty
            };
            return PlaceOrderAsync(contract, o, ct);
        }

        public Task<int> PlaceStopLimitAsync(Contract contract, int qty, double stopTrigger, double limitPrice, string tif = "DAY", bool outsideRth = false, string? account = null, bool close = true, CancellationToken ct = default)
        {
            var o = new Order
            {
                Action = "SELL",
                OrderType = "STP LMT",
                TotalQuantity = qty,
                AuxPrice = stopTrigger,     // tetik
                LmtPrice = limitPrice,      // satış limiti
                Tif = tif,
                OutsideRth = outsideRth,
                OpenClose = close ? "C" : "O",
                Account = account ?? string.Empty
            };
            return PlaceOrderAsync(contract, o, ct);
        }

        // TwsService içine ekle

        private static double R2(double x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);

        public async Task<(int parentId, int childId)> PlaceBreakoutBuyStopLimitWithProtectiveStopAsync(
            int conId,
            double triggerPrice,     // BUY tetik
            double offset,           // limit = trigger + offset
            double stopLoss,         // abs stop = trigger - stopLoss
            int quantity,
            string tif = "DAY",
            bool outsideRth = false,
            string? account = null,
            double stopLimitOffset = 0.05,
            CancellationToken ct = default)
        {
            var aux_stop = R2(triggerPrice);
            var limit_cap = R2(triggerPrice + offset);
            var stop_abs = R2(triggerPrice - stopLoss);
            var stop_limit = R2(stop_abs - stopLimitOffset);

            var c = new Contract { ConId = conId, Exchange = "SMART", Currency = "USD" };

            var parent = new Order
            {
                Action = "BUY",
                OrderType = "STP LMT",
                TotalQuantity = quantity,
                AuxPrice = aux_stop,
                LmtPrice = limit_cap,
                Tif = tif,
                OutsideRth = outsideRth,
                Transmit = false,
                Account = account ?? string.Empty
            };
            var parentId = await PlaceOrderAsync(c, parent, ct).ConfigureAwait(false);

            var child = new Order
            {
                Action = "SELL",
                OrderType = "STP LMT",
                TotalQuantity = quantity,
                AuxPrice = stop_abs,
                LmtPrice = stop_limit,
                Tif = tif,
                OpenClose = "C",
                ParentId = parentId,
                Transmit = true,
                Account = account ?? string.Empty
            };
            var childId = await PlaceOrderAsync(c, child, ct).ConfigureAwait(false);

            return (parentId, childId);
        }

        public async Task<(int parentId, int childId)> PlaceMarketBuyWithProtectiveStopAsync(
            int conId,
            double triggerPrice,     // sadece stop hesap için kullanılır
            double stopLoss,         // abs stop = trigger - stopLoss
            int quantity,
            string tif = "DAY",
            bool outsideRth = false,
            string? account = null,
            double stopLimitOffset = 0.05,
            CancellationToken ct = default)
        {
            var stop_abs = R2(triggerPrice - stopLoss);
            var stop_limit = R2(stop_abs - stopLimitOffset);

            var c = new Contract { ConId = conId, Exchange = "SMART", Currency = "USD" };

            var parent = new Order
            {
                Action = "BUY",
                OrderType = "MKT",
                TotalQuantity = quantity,
                Tif = tif,
                OutsideRth = outsideRth,
                Transmit = false,
                Account = account ?? string.Empty
            };
            var parentId = await PlaceOrderAsync(c, parent, ct).ConfigureAwait(false);

            var child = new Order
            {
                Action = "SELL",
                OrderType = "STP LMT",
                TotalQuantity = quantity,
                AuxPrice = stop_abs,
                LmtPrice = stop_limit,
                Tif = tif,
                OpenClose = "C",
                ParentId = parentId,
                Transmit = true,
                Account = account ?? string.Empty
            };
            var childId = await PlaceOrderAsync(c, child, ct).ConfigureAwait(false);

            return (parentId, childId);
        }


        // =======================
        // EWrapper overrides
        // =======================

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
                    strikes ?? new HashSet<double>()
                ));
            }
        }

        public override void securityDefinitionOptionParameterEnd(int reqId)
        {
            if (_optTcs.TryRemove(reqId, out var box))
                box.tcs.TrySetResult(box.buf);
        }

        public override void openOrder(int orderId, Contract c, Order o, OrderState s)
        {
            if (_orderAck.TryRemove(orderId, out var tcs))
                tcs.TrySetResult(s?.Status ?? "open");
        }

        public override void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice,
            int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            if (_orderAck.TryRemove(orderId, out var tcs))
                tcs.TrySetResult(status);

            if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                _cancelAck.TryRemove(orderId, out var cts))
                cts.TrySetResult(status);
        }

        public override void error(int id, int errorCode, string errorMsg)
        {
            // Request TCS’leri
            if (_symTcs.TryRemove(id, out var t1)) { t1.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            if (_cdTcs.TryRemove(id, out var t2)) { t2.tcs.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            if (_optTcs.TryRemove(id, out var t3)) { t3.tcs.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }

            // Order ACK
            if (_orderAck.TryRemove(id, out var o1)) { o1.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }
            if (_cancelAck.TryRemove(id, out var o2)) { o2.TrySetException(new Exception($"[{errorCode}] {errorMsg}")); return; }

            base.error(id, errorCode, errorMsg);
        }
    }
}
