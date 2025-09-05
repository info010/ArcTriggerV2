using System;
using System.Threading;
using ArcTriggerV2.Core.Services;
using ArcTriggerV2.Core.Utils;
using IBApi;

class Program
{
    static void Main(string[] args)
        {
            Console.WriteLine("=== IBKR TWS API Multi-Order Test ===");

            var tradeService = new TradeService();
            tradeService.Connect("127.0.0.1", 7497, 0);

            Thread.Sleep(2000); // nextValidId için bekle

            // ------------------------
            // 1. LMT PUT
            // ------------------------
            var lmtPutContract = new OptionContractBuilder()
                .WithSymbol("NVDA")
                .WithExpiry("20251219")
                .WithStrike(180)
                .WithRight("P") // Put
                .WithExchange("SMART")
                .WithCurrency("USD")
                .Build();

            var lmtPutOrder = new OrderBuilder()
                .WithAction("BUY")
                .WithOrderType("LMT")
                .WithQuantity(1)
                .WithLimitPrice(6.50)
                .WithTif("DAY")
                .Build();

            int id1 = tradeService.PlaceOrder(lmtPutContract, lmtPutOrder);

            // ------------------------
            // 2. LMT CALL
            // ------------------------
            var lmtCallContract = new OptionContractBuilder()
                .WithSymbol("NVDA")
                .WithExpiry("20251219")
                .WithStrike(180)
                .WithRight("C") // Call
                .WithExchange("SMART")
                .WithCurrency("USD")
                .Build();

            var lmtCallOrder = new OrderBuilder()
                .WithAction("BUY")
                .WithOrderType("LMT")
                .WithQuantity(1)
                .WithLimitPrice(5.50)
                .WithTif("DAY")
                .Build();

            int id2 = tradeService.PlaceOrder(lmtCallContract, lmtCallOrder);

            // ------------------------
            // 3. MKT PUT
            // ------------------------
            var mktPutContract = new OptionContractBuilder()
                .WithSymbol("NVDA")
                .WithExpiry("20251219")
                .WithStrike(180)
                .WithRight("P")
                .WithExchange("SMART")
                .WithCurrency("USD")
                .Build();

            var mktPutOrder = new OrderBuilder()
                .WithAction("BUY")
                .WithOrderType("MKT")
                .WithQuantity(1)
                .WithTif("DAY")
                .Build();

            int id3 = tradeService.PlaceOrder(mktPutContract, mktPutOrder);

            // ------------------------
            // 4. MKT CALL
            // ------------------------
            var mktCallContract = new OptionContractBuilder()
                .WithSymbol("NVDA")
                .WithExpiry("20251219")
                .WithStrike(180)
                .WithRight("C")
                .WithExchange("SMART")
                .WithCurrency("USD")
                .Build();

            var mktCallOrder = new OrderBuilder()
                .WithAction("BUY")
                .WithOrderType("MKT")
                .WithQuantity(1)
                .WithTif("DAY")
                .Build();

            int id4 = tradeService.PlaceOrder(mktCallContract, mktCallOrder);

            // ------------------------
            Console.WriteLine($"Orders gönderildi: {id1}, {id2}, {id3}, {id4}");
            Console.WriteLine("10 saniye bekleniyor...");
            Thread.Sleep(10000);

            // Hepsini iptal et
            tradeService.CancelOrder(id1);
            tradeService.CancelOrder(id2);
            tradeService.CancelOrder(id3);
            tradeService.CancelOrder(id4);

            Console.WriteLine("Tüm orderlar iptal edildi.");
            Console.WriteLine("Çıkmak için tuşa basın...");
            Console.ReadKey();

            tradeService.Disconnect();
        }
}
