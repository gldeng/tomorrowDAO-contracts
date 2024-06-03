using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Address GetTreasuryAccountAddress(Hash input)
    {
        var treasuryInfo = State.TreasuryInfoMap[input];
        return treasuryInfo?.TreasuryAddress;
    }

    public override TreasuryInfo GetTreasuryInfo(Hash input)
    {
        return State.TreasuryInfoMap[input] ?? new TreasuryInfo();
    }

    public override Hash GetDAOIdByTreasuryAccountAddress(Address input)
    {
        var daoId = State.TreasuryAccountMap[input];
        return daoId;
    }

    public override BoolValue IsTokenSupportedStaking(IsTokenSupportedStakingInput input)
    {
        var treasuryInfo = State.TreasuryInfoMap[input.DaoId];
        var value = new BoolValue();
        value.Value = treasuryInfo != null && treasuryInfo.SupportedStakingTokens.Data.Contains(input.Symbol);
        return value;
    }

    public override BoolValue IsPaused(Hash input)
    {
        var boolValue = new BoolValue();
        if (State.IsPaused.Value)
        {
            boolValue.Value = true;
            return boolValue;
        }

        var treasuryInfo = State.TreasuryInfoMap[input];
        Assert(treasuryInfo != null, "Treasury has not bean created yet.");
        boolValue.Value = State.TreasuryPausedMap[treasuryInfo!.TreasuryAddress];
        return boolValue;
    }

    public override FundInfo GetFundInfo(GetFundInfoInput input)
    {
        var treasuryInfo = State.TreasuryInfoMap[input.DaoId];
        if (treasuryInfo == null)
        {
            return new FundInfo();
        }

        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] ?? new FundInfo();
        fundInfo.DaoId = input.DaoId;
        fundInfo.Symbol = input.Symbol;
        fundInfo.TreasuryAddress = treasuryInfo.TreasuryAddress;
        return fundInfo;
    }

    public override FundInfo GetTotalFundInfo(GetTotalFundInfoInput input)
    {
        var totalFundInfo = State.TotalFundInfoMap[input.Symbol] ?? new FundInfo();
        totalFundInfo.Symbol = input.Symbol;
        return totalFundInfo;
    }

    public override LockInfo GetLockInfo(LockInfoInput input)
    {
        var lockInfo = State.LockInfoMap[input.LockId ?? Hash.Empty];
        if (lockInfo == null)
        {
            var lockId = State.ProposalLockMap[input.ProposalId ?? Hash.Empty];
            if (lockId != input.LockId)
            {
                lockInfo = State.LockInfoMap[lockId];
            }
        }

        return lockInfo ?? new LockInfo();
    }
}