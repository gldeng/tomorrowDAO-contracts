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
        boolValue.Value = State.DaoTreasuryPaused[treasuryInfo!.TreasuryAddress];
        return boolValue;
    }
}