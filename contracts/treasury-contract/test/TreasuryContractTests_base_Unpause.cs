using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsBaseUnpause : TreasuryContractTestsBase
{
    [Fact]
    public async Task UnpauseTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasury(daoId);

        var result = await TreasuryContractStub.Pause.SendAsync(daoId);
        
        result = await TreasuryContractStub.AddSupportedStakingTokens.SendWithExceptionAsync(new AddSupportedStakingTokensInput
        {
            DaoId = daoId,
            Symbols = new SymbolList
            {
                Data = { "STR" }
            }
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Treasury has bean paused.");
        
        result = await TreasuryContractStub.Unpause.SendAsync(daoId);
        
        result = await TreasuryContractStub.AddSupportedStakingTokens.SendAsync(new AddSupportedStakingTokensInput
        {
            DaoId = daoId,
            Symbols = new SymbolList
            {
                Data = { "STR" }
            }
        });
        result.ShouldNotBeNull();
        var supportedStakingTokensAdded = GetLogEvent<SupportedStakingTokensAdded>(result.TransactionResult);
        supportedStakingTokensAdded.ShouldNotBeNull();
        supportedStakingTokensAdded.DaoId.ShouldBe(daoId);
    }
}