using IBApi;

namespace ArcTriggerV2.Core.Utils
{
    public class OrderBuilder
    {
        private readonly Order _order;

        public OrderBuilder()
        {
            _order = new Order
            {
                Tif = "DAY",
                Transmit = true
            };
        }

        public OrderBuilder WithAction(string action)
        {
            _order.Action = action;
            return this;
        }

        public OrderBuilder WithOrderType(string type)
        {
            _order.OrderType = type;
            return this;
        }

        public OrderBuilder WithQuantity(int quantity)
        {
            _order.TotalQuantity = quantity;
            return this;
        }

        public OrderBuilder WithLimitPrice(double price)
        {
            _order.LmtPrice = price;
            return this;
        }

        public OrderBuilder WithStopPrice(double price)
        {
            _order.AuxPrice = price;
            return this;
        }

        public OrderBuilder WithTif(string tif)
        {
            _order.Tif = tif;
            return this;
        }

        public OrderBuilder WithTransmit(bool transmit)
        {
            _order.Transmit = transmit;
            return this;
        }

        public OrderBuilder WithOutsideRth(bool allow)
        {
            _order.OutsideRth = allow;
            return this;
        }

        public OrderBuilder WithGoodAfterTime(string time)
        {
            _order.GoodAfterTime = time; // format: yyyyMMdd HH:mm:ss
            return this;
        }

        public OrderBuilder WithGoodTillDate(string time)
        {
            _order.GoodTillDate = time; // format: yyyyMMdd HH:mm:ss
            return this;
        }

        public OrderBuilder WithParentId(int parentId)
        {
            _order.ParentId = parentId;
            return this;
        }

        public OrderBuilder WithOcaGroup(string groupId, int type = 1)
        {
            _order.OcaGroup = groupId;
            _order.OcaType = type; // 1=CancelAll, 2=Reduce
            return this;
        }

        public Order Build() => _order;
    }
}
