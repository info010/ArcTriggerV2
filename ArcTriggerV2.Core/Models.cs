namespace ArcTriggerV2.Core;

public record ContractKey(string Symbol, string Exchange, string Currency, string SecType="STK");
public enum OrderSide { Buy, Sell }
public enum OrderType { Mkt, Lmt, Stop, StopLimit }

public record NewOrderRequest(
    ContractKey C, OrderSide Side, OrderType Type, int Qty,
    double? Limit=null, double? AuxPrice=null, string Tif="DAY");

public record OrderStatusDto(int OrderId, string Status, int Filled, int Remaining, double AvgFillPrice);
public record PositionDto(ContractKey C, int Position, double AvgCost);
