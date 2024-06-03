using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsTransferRelease : TreasuryContractTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TreasuryContractTestsTransferRelease(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RequestTransferTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasuryAddDonateAndStaking(daoId);

        var proposalId = await RequestTransferAndVote(daoId);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
        var result = await GovernanceContractStub.ExecuteProposal.SendAsync(proposalId);

        var treasuryTransferReleased = GetLogEvent<TreasuryTransferReleased>(result.TransactionResult);
        treasuryTransferReleased.ShouldNotBeNull();
        treasuryTransferReleased.Amount.ShouldBe(OneElfAmount * 5);
        var lockId = treasuryTransferReleased.LockId;
        treasuryTransferReleased.ProposalId.ShouldBe(proposalId);

        var fundInfo = await TreasuryContractStub.GetFundInfo.CallAsync(new GetFundInfoInput
        {
            DaoId = daoId,
            Symbol = DefaultGovernanceToken
        });
        fundInfo.ShouldNotBeNull();
        fundInfo.LockedFunds.ShouldBe(0);
        fundInfo.AvailableFunds.ShouldBe(OneElfAmount * 15);

        var totalFundInfo = await TreasuryContractStub.GetTotalFundInfo.CallAsync(new GetTotalFundInfoInput
        {
            Symbol = DefaultGovernanceToken
        });
        totalFundInfo.ShouldNotBeNull();
        totalFundInfo.LockedFunds.ShouldBe(0);
        totalFundInfo.AvailableFunds.ShouldBe(OneElfAmount * 15);

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = DefaultGovernanceToken,
            Owner = UserAddress
        });
        balance.Balance.ShouldBe(OneElfAmount * 5);
    }
}