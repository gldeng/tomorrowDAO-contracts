using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Empty Donate(DonateInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Symbol, "Symbol");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);

        // Assert(treasuryInfo.SupportedStakingTokens.Data.Contains(input.Symbol),
        //     $"Donation Token {input.Symbol} is not supported at the moment.");

        TransferTokenToTreasury(treasuryInfo, input.Symbol, input.Amount, false);

        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] ?? new FundInfo();
        fundInfo.AvailableFunds += input.Amount;
        State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] = fundInfo;

        var totalFundInfo = State.TotalFundInfoMap[input.Symbol] ?? new FundInfo();
        totalFundInfo.AvailableFunds += input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;

        Context.Fire(new DonationReceived
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Donor = Context.Sender,
            DonationTime = Context.CurrentBlockTime
        });

        return new Empty();
    }

    public override Empty StakeToken(StakeTokenInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Symbol, "Symbol");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);

        Assert(treasuryInfo.SupportedStakingTokens.Data.Contains(input.Symbol),
            $"Staking Token {input.Symbol} is not supported.");

        TransferTokenToTreasury(treasuryInfo, input.Symbol, input.Amount, true);

        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] ?? new FundInfo();
        fundInfo.AvailableFunds += input.Amount;
        State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] = fundInfo;

        var totalFundInfo = State.TotalFundInfoMap[input.Symbol] ?? new FundInfo();
        totalFundInfo.AvailableFunds += input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;

        Context.Fire(new TokenStaked
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Account = Context.Sender,
            StakedTime = Context.CurrentBlockTime
        });

        return new Empty();
    }

    private void TransferTokenToTreasury(TreasuryInfo treasuryInfo, string symbol, long amount, bool isStakingToken)
    {
        var memo = isStakingToken ? "Staking" : "donate" + " token to treasury";
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = treasuryInfo.TreasuryAddress,
            Symbol = symbol,
            Amount = amount,
            Memo = memo
        });
    }
}