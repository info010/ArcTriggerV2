using System;
using System.Threading.Tasks;
using ArcTriggerV2.Core.Services;
using ArcTriggerV2.Core.Models;
using IBApi;
using ArcTriggerV2.Core.Utils;

class Program
{
    static async Task Main(string[] args)
    {

        var tws = new TwsService();

        tws.OnMarketData += (data) =>
        {
            Console.WriteLine(
                $"[{data.Timestamp:HH:mm:ss.fff}] {data.TickerId} " +
                $"Last={data.Last} Bid={data.Bid} Ask={data.Ask} Vol={data.Volume}"
            );
        };

        await tws.ConnectAsync("127.0.0.1", 7497, 0);
        int aaplId = tws.RequestMarketData(265598, secType: "STK", marketDataType: 3);

        // TSLA (NASDAQ)
        int tslaId = tws.RequestMarketData(76792991, secType: "STK", marketDataType: 3);

        // GOOGL (NASDAQ)
        int googlId = tws.RequestMarketData(208813719, secType: "STK", marketDataType: 3);
        // bekle

        Thread.Sleep(300000);


        tws.CancelMarketData(aaplId);
        tws.CancelMarketData(tslaId);
        tws.CancelMarketData(googlId);

        tws.Disconnect();

        // Console.Write("Sembol ara: ");
        // var symbol = Console.ReadLine()?.Trim().ToUpperInvariant();
        // if (string.IsNullOrWhiteSpace(symbol)) symbol = "AAPL";

        // try
        // {
        //     // 1) Arama
        //     var matches = await svc.SearchSymbolsAsync(symbol);
        //     if (matches.Count == 0) { Console.WriteLine("Sonuç yok."); return; }

        //     Console.WriteLine($"\nBulunan semboller ({matches.Count}):");
        //     for (int i = 0; i < matches.Count; i++)
        //         Console.WriteLine($"[{i}] {matches[i].Symbol} | {matches[i].SecType} | ConId={matches[i].ConId}");

        //     // STK öncelikli seçim
        //     var candidates = matches
        //         .Where(m => m.ConId > 0)
        //         .GroupBy(m => m.ConId)
        //         .Select(g => g.First())
        //         .ToList();

        //     if (candidates.Count == 0)
        //     {
        //         Console.WriteLine("ConId içeren eşleşme yok.");
        //         return;
        //     }

        //     Console.WriteLine("\nConId seçim listesi:");
        //     for (int i = 0; i < candidates.Count; i++)
        //         Console.WriteLine($"[{i}] {candidates[i].Symbol} | {candidates[i].SecType} | ConId={candidates[i].ConId}");

        //     Console.Write("Seçim (index, enter=0): ");
        //     var pickIn = Console.ReadLine();
        //     var pickIdx = (int.TryParse(pickIn, out var tmp) && tmp >= 0 && tmp < candidates.Count) ? tmp : 0;

        //     var pick = candidates[pickIdx];

        //     // 2) Detay (underlying kontrolü)
        //     var stkDetails = await svc.GetContractDetailsAsync(new Contract { ConId = pick.ConId });
        //     if (stkDetails.Count == 0)
        //     {
        //         stkDetails = await svc.GetContractDetailsAsync(new Contract
        //         {
        //             Symbol = pick.Symbol,
        //             SecType = pick.SecType,
        //             Currency = "USD",
        //             Exchange = "SMART"
        //         });
        //     }
        //     if (stkDetails.Count == 0) { Console.WriteLine("Detay bulunamadı."); return; }

        //     var stk = stkDetails[0];
        //     var underConId = stk.ConId; // Opsiyon için underlying conid

        //     Console.WriteLine($"\nUnderlyer: {stk.Symbol} {stk.SecType} ConId={underConId}, LongName={stk.LongName}");

        //     // 3) Opsiyon parametreleri (GetOptionParamsAsync)
        //     Console.WriteLine("\nOpsiyon parametreleri getiriliyor...");
        //     static string MapUnderlyingSecType(string secType) => secType switch
        //     {
        //         "STK" => "STK",
        //         "ETF" => "STK",
        //         "CFD" => "STK",   // zincir için çoğu durumda STK kullanılır
        //         "FUT" => "FUT",
        //         "IND" => "IND",
        //         _ => "STK"
        //     };

        //     var uSecType = MapUnderlyingSecType(stk.SecType);

        //     // Tüm borsalar için parametre al (futFopExchange="")
        //     var chains = await svc.GetOptionParamsAsync(underConId, stk.Symbol, underlyingSecType: uSecType, futFopExchange: "");
        //     if (chains.Count == 0) { Console.WriteLine("Opsiyon parametresi yok."); return; }

        //     // Exchange listesi
        //     var exchanges = chains.Select(c => c.Exchange).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
        //     Console.WriteLine("\nBorsalar:");
        //     for (int i = 0; i < exchanges.Count; i++) Console.WriteLine($"[{i}] {exchanges[i]}");
        //     Console.Write("Borsa seç (enter=SMART): ");
        //     var exSel = Console.ReadLine();
        //     var exchange = string.IsNullOrWhiteSpace(exSel) ? "SMART"
        //                    : (int.TryParse(exSel, out var exIdx) && exIdx >= 0 && exIdx < exchanges.Count ? exchanges[exIdx] : exchanges.First());

        //     // Expiration set’i
        //     var expirations = chains.SelectMany(c => c.Expirations).Distinct().ToList();
        //     expirations.Sort(StringComparer.Ordinal); // YYYYMM veya YYYYMMDD lexicographically uygun
        //     Console.WriteLine("\nVade listesi:");
        //     for (int i = 0; i < expirations.Count; i++) Console.WriteLine($"[{i}] {expirations[i]}");
        //     Console.Write("Vade seç (index, enter=0): ");
        //     var vIn = Console.ReadLine();
        //     var vIdx = (int.TryParse(vIn, out var vi) && vi >= 0 && vi < expirations.Count) ? vi : 0;
        //     var expiry = expirations[vIdx];

        //     // Strikes
        //     var allStrikes = chains.SelectMany(c => c.Strikes).Distinct().ToList();
        //     allStrikes.Sort();
        //     var sample = allStrikes.ToList();
        //     Console.WriteLine("\nStrike örnekleri:");
        //     for (int i = 0; i < sample.Count; i++) Console.WriteLine($"[{i}] {sample[i]}");
        //     Console.Write("Strike seç (index, enter=orta): ");
        //     var sIn = Console.ReadLine();
        //     double strike;
        //     if (int.TryParse(sIn, out var si) && si >= 0 && si < sample.Count) strike = sample[si];
        //     else strike = sample.Count > 0 ? sample[sample.Count / 2] : allStrikes[allStrikes.Count / 2];

        //     Console.Write("Right seç (C/P, enter=C): ");
        //     var rightIn = Console.ReadLine();
        //     var right = string.IsNullOrWhiteSpace(rightIn) ? "C" : rightIn.Trim().ToUpperInvariant()[0] == 'P' ? "P" : "C";

        //     // Seçilen exchange için tradingClass/multiplier al
        //     var chainForEx = chains.FirstOrDefault(c => string.Equals(c.Exchange, exchange, StringComparison.OrdinalIgnoreCase))
        //                      ?? chains.First();
        //     var tradingClass = chainForEx.TradingClass;
        //     var multiplier = chainForEx.Multiplier;

        //     Console.WriteLine($"\nSeçimler → Exch={exchange}, Expiry={expiry}, Right={right}, Strike={strike}, TC={tradingClass}, Mult={multiplier}");

        //     // 4) Nihai conId çözümü (ResolveOptionConidAsync)
        //     var optConId = await svc.ResolveOptionConidAsync(
        //         symbol: stk.Symbol,
        //         exchange: exchange,
        //         right: right,
        //         yyyymmdd: expiry,
        //         strike: strike,
        //         tradingClass: tradingClass,
        //         multiplier: multiplier
        //     );

        //     Console.WriteLine($"\nNihai Option ConId: {optConId}");

        //     // Opsiyon detayını da göster
        //     var optDetails = await svc.GetContractDetailsAsync(new Contract { ConId = optConId });
        //     foreach (var cd in optDetails)
        //         Console.WriteLine($"OPT → {cd.Symbol} {cd.LocalSymbol} {cd.SecType} {cd.Exchange} {cd.Currency} ConId={cd.ConId} TC={cd.TradingClass} Mult={cd.Multiplier} LongName={cd.LongName}");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Hata: {ex.Message}");
        // }
    }
}