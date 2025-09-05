using IBApi;
using ArcTriggerV2.Core.Utils;

namespace ArcTriggerV2.Core.Services
{
    public class TradeService : BaseService
    {
        public int PlaceOrder(Contract contract, Order order)
        {
            int orderId = GetNextOrderId();
            Client.placeOrder(orderId, contract, order);
            Console.WriteLine($"Order placed. Id={orderId}, {order.Action} {order.TotalQuantity} {contract.Symbol} {order.OrderType}");
            return orderId;
        }

        public void CancelOrder(int orderId)
        {
            Client.cancelOrder(orderId);
            Console.WriteLine($"Order cancelled. Id={orderId}");
        }

        public int PlaceProfitTaking(Contract contract, int qty, double limitPrice)
        {
            var order = new OrderBuilder()
                .WithAction("SELL")
                .WithOrderType("LMT")
                .WithQuantity(qty)
                .WithLimitPrice(limitPrice)
                .WithTif("DAY")
                .Build();

            return PlaceOrder(contract, order);
        }

        public int PlaceBreakeven(Contract contract, int qty)
        {
            var order = new OrderBuilder()
                .WithAction("SELL")
                .WithOrderType("MKT")
                .WithQuantity(qty)
                .WithTif("DAY")
                .Build();

            return PlaceOrder(contract, order);
        }

        public void InvalidateOrder(int orderId)
        {
            CancelOrder(orderId);
        }
    }
}