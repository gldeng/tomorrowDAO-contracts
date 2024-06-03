using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsTransferUnlockToken : TreasuryContractTestsBase
{
    [Fact]
    public async Task UnlockTokenTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasuryAddDonateAndStaking(daoId);

        var proposalId = await RequestTransferAndVote(daoId, false);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000 * 2);
        var result = await GovernanceContractStub.ExecuteProposal.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Error.ShouldContain("The proposal is in active or expired");

        result = await TreasuryContractStub.UnlockToken.SendAsync(proposalId);
        var treasuryTokenUnlocked = GetLogEvent<TreasuryTokenUnlocked>(result.TransactionResult);
        treasuryTokenUnlocked.ShouldNotBeNull();
        
        var fundInfo = await TreasuryContractStub.GetFundInfo.CallAsync(new GetFundInfoInput
        {
            DaoId = daoId,
            Symbol = DefaultGovernanceToken
        });
        fundInfo.ShouldNotBeNull();
        fundInfo.LockedFunds.ShouldBe(0);
        fundInfo.AvailableFunds.ShouldBe(OneElfAmount * 20);

        var totalFundInfo = await TreasuryContractStub.GetTotalFundInfo.CallAsync(new GetTotalFundInfoInput
        {
            Symbol = DefaultGovernanceToken
        });
        totalFundInfo.ShouldNotBeNull();
        totalFundInfo.LockedFunds.ShouldBe(0);
        totalFundInfo.AvailableFunds.ShouldBe(OneElfAmount * 20);

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = DefaultGovernanceToken,
            Owner = treasuryTokenUnlocked.LockInfo.TreasuryAddress
        });
        balance.Balance.ShouldBe(OneElfAmount * 20);
    }
}