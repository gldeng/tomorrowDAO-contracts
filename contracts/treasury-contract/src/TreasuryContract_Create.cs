using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Empty CreateTreasury(CreateTreasuryInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertDaoSubsists(input.DaoId);
        AssertSymbolList(input.Symbols);
        var daoInfo = AssertSenderIsDaoContractOrDaoAdmin(input.DaoId);
        if (!string.IsNullOrWhiteSpace(daoInfo.GovernanceToken))
        {
            Assert(input.Symbols.Data.Contains(daoInfo.GovernanceToken), "Symbols must be include governance token.");
        }

        Assert(input.Symbols.Data.Contains(TreasuryContractConstants.DefaultToken),
            $"Symbols must be include {TreasuryContractConstants.DefaultToken} token.");

        var treasuryInfo = State.TreasuryInfoMap[input.DaoId];
        Assert(treasuryInfo == null, $"Dao {input.DaoId} treasury has been created.");

        var treasuryHash = GenerateTreasuryHash(input.DaoId, Context.Self);
        var treasuryAddress = GetTreasuryAddress(treasuryHash);

        var daoId = State.TreasuryAccountMap[treasuryAddress];
        Assert(daoId == null || daoId == Hash.Empty, "Treasury address already exists.");

        treasuryInfo = new TreasuryInfo
        {
            TreasuryAddress = treasuryAddress,
            SupportedStakingTokens = input.Symbols
        };
        State.TreasuryInfoMap[input.DaoId] = treasuryInfo;
        State.TreasuryAccountMap[treasuryAddress] = daoId;

        Context.Fire(new TreasuryCreated
        {
            DaoId = input.DaoId,
            TreasuryAccountAddress = treasuryAddress,
            SymbolList = input.Symbols
        });
        return new Empty();
    }

    public override Empty AddSupportedStakingTokens(AddSupportedStakingTokensInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertSymbolList(input.Symbols);
        AssertSenderIsDaoContractOrDaoAdmin(input.DaoId);
        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);
        treasuryInfo.SupportedStakingTokens.Data.AddRange(input.Symbols.Data);
        Assert(treasuryInfo.SupportedStakingTokens.Data.Count <= TreasuryContractConstants.MaxStakingTokenLimit,
            $"The staked token cannot be exceed {TreasuryContractConstants.MaxStakingTokenLimit} types");
        State.TreasuryInfoMap[input.DaoId] = treasuryInfo;
        
        Context.Fire(new SupportedStakingTokensAdded
        {
            DaoId = input.DaoId,
            AddedTokens = input.Symbols,
            SupportedTokens = treasuryInfo.SupportedStakingTokens
        });
        
        return new Empty();
    }
    
    public override Empty RemoveSupportedStakingTokens(RemoveSupportedStakingTokensInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertSenderIsDaoContractOrDaoAdmin(input.DaoId);
        AssertSymbolList(input.Symbols);
        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);
        
        var daoInfo = State.DaoContract.GetDAOInfo.Call(input.DaoId);
        if (!string.IsNullOrWhiteSpace(daoInfo.GovernanceToken))
        {
            Assert(!input.Symbols.Data.Contains(daoInfo.GovernanceToken), "Governance tokens cannot be removed.");
        }

        Assert(!input.Symbols.Data.Contains(TreasuryContractConstants.DefaultToken),
            $"{TreasuryContractConstants.DefaultToken} token cannot be removed.");
        

        foreach (var symbol in input.Symbols.Data)
        {
            treasuryInfo.SupportedStakingTokens.Data.Remove(symbol);
        }
        State.TreasuryInfoMap[input.DaoId] = treasuryInfo;
        
        Context.Fire(new SupportedStakingTokensRemoved
        {
            DaoId = input.DaoId,
            RemovedTokens = input.Symbols
        });

        return new Empty();
    }
}