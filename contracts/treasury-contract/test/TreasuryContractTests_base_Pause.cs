using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsBasePause : TreasuryContractTestsBase
{
    [Fact]
    public async Task PauseTest()
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
    }
}