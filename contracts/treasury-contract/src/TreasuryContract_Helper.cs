using System.Linq;
using System.Threading;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    //Assert Helper
    private void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Contract not initialized.");
    }

    private void AssertSenderDaoContract()
    {
        Assert(State.DaoContract.Value == Context.Sender, "No permission.");
    }

    private void AssertSenderGovernanceContract()
    {
        Assert(State.GovernanceContract.Value == Context.Sender, "No permission.");
    }

    private void AssertSymbolList(SymbolList symbols = null)
    {
        Assert(
            symbols != null && symbols.Data != null &&
            symbols.Data.Count <= TreasuryContractConstants.MaxStakingTokenLimit,
            $"The staked token cannot be empty or exceed {TreasuryContractConstants.MaxStakingTokenLimit} types");
    }

    private void AssertNotNullOrEmpty(object input, string message = null)
    {
        message = string.IsNullOrWhiteSpace(message) ? "Invalid input." : $"{message} cannot be null or empty.";
        Assert(input != null, message);
        switch (input)
        {
            case Address address:
                Assert(address.Value.Any(), message);
                break;
            case Hash hash:
                Assert(hash != Hash.Empty, message);
                break;
            case string s:
                Assert(!string.IsNullOrEmpty(s), message);
                break;
        }
    }

    private void AssertDaoSubsists(Hash daoId)
    {
        AssertNotNullOrEmpty(daoId, "DaoId");
        var boolValue = State.DaoContract.GetSubsistStatus.Call(daoId);
        Assert(boolValue.Value, "DAO does not exist or is not in subsistence.");
    }

    private TreasuryInfo AssertDaoSubsistAndTreasuryStatus(Hash daoId)
    {
        AssertDaoSubsists(daoId);
        Assert(!State.IsPaused.Value, "Treasury has bean paused.");

        var treasuryInfo = State.TreasuryInfoMap[daoId];
        Assert(treasuryInfo != null, "Treasury has not bean created yet.");
        
        var daoTreasuryPaused = State.DaoTreasuryPaused[treasuryInfo!.TreasuryAddress];
        Assert(!daoTreasuryPaused, "Treasury has bean paused.");
        
        return treasuryInfo;
    }

    private static Hash GenerateTreasuryHash(Hash daoId, Address treasuryAddress)
    {
        return HashHelper.ConcatAndCompute(daoId, HashHelper.ComputeFrom(treasuryAddress));
    }

    private Address GetTreasuryAddressFromDaoId(Hash daoId)
    {
        var treasuryHash = GenerateTreasuryHash(daoId, Context.Self);
        return Context.ConvertVirtualAddressToContractAddress(treasuryHash);
    }

    private Address GetTreasuryAddress(Hash treasuryHash)
    {
        return Context.ConvertVirtualAddressToContractAddress(treasuryHash);
    }


    private TokenInfo GetTokenInfo(string symbol)
    {
        return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol
        });
    }
}