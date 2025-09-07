using IBApi;
using ArcTriggerV2.Core.Models;

namespace ArcTriggerV2.Core.Abstractions
{
    public interface IContractService
    {
        Task<IReadOnlyList<SymbolMatch>> SearchSymbolsAsync(string query, CancellationToken ct = default); // reqMatchingSymbols
        Task<IReadOnlyList<ContractInfo>> GetContractDetailsAsync(Contract key, CancellationToken ct = default); // reqContractDetails
        Task<IReadOnlyList<OptionChainParams>> GetOptionParamsAsync(int underlyingConId, string symbol, string underlyingSecType, string futFopExchange, CancellationToken ct = default); // reqSecDefOptParams
        Task<int> ResolveOptionConidAsync(string symbol, string exchange, string right, string yyyymmdd, double strike, string tradingClass = "", string multiplier = "", CancellationToken ct = default); // reqContractDetails -> conId
    }

}