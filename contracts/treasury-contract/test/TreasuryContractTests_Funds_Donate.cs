using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsFundsDonate : TreasuryContractTestsBase
{
    [Fact]
    public async Task DonateTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasury(daoId);

        var executionResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = TreasuryContractAddress,
            Symbol = DefaultGovernanceToken,
            Amount = OneElfAmount * 10
        });

        var result = await TreasuryContractStub.Donate.SendAsync(new DonateInput
        {
            DaoId = daoId,
            Amount = OneElfAmount * 10,
            Symbol = DefaultGovernanceToken
        });
        result.ShouldNotBeNull();

        var fundInfo  = await TreasuryContractStub.GetFundInfo.CallAsync(new GetFundInfoInput
        {
            DaoId = daoId,
            Symbol = DefaultGovernanceToken
        });
        fundInfo.ShouldNotBeNull();
        fundInfo.AvailableFunds.ShouldBe(OneElfAmount * 10);
    }
    
    [Fact]
    public async Task DonateTest_Insufficient()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasuryAddDonateAndStaking(daoId);

        var result = await TreasuryContractStub.Donate.SendWithExceptionAsync(new DonateInput
        {
            DaoId = daoId,
            Amount = OneElfAmount * 10,
            Symbol = DefaultGovernanceToken
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Insufficient allowance");
    }
}