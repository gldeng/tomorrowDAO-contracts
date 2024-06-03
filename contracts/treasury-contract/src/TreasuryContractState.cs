using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.Treasury;

// The state class is access the blockchain state
public partial class TreasuryContractState : ContractState
{
    public BoolState Initialized { get; set; }

    public BoolState IsPaused { get; set; }

    //<treasury address, is paused>
    public MappedState<Address, bool> TreasuryPausedMap { get; set; }

    //<DAO id -> TreasuryInfo>
    public MappedState<Hash, TreasuryInfo> TreasuryInfoMap { get; set; }

    //DAO id, <TreasuryAccountAddress -> DAO id>
    public MappedState<Address, Hash> TreasuryAccountMap { get; set; }

    //<DAO id, Symbol>
    public MappedState<Hash, string> SupportedStakingTokenMap { get; set; }

    //<treasury -> symbol -> FundInfo>
    public MappedState<Address, string, FundInfo> FundInfoMap { get; set; }

    //<symbol -> FundInfo>
    public MappedState<string, FundInfo> TotalFundInfoMap { get; set; }

    //<symbol -> account -> FundInfo>
    public MappedState<string, string, FundInfo> PledgedAmountMap { get; set; }

    //<proposal id -> lock id>
    public MappedState<Hash, Hash> ProposalLockMap { get; set; }

    //<lock id -> LockInfo>
    public MappedState<Hash, LockInfo> LockInfoMap { get; set; }
}