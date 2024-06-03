using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsCreateRemoveTokens : TreasuryContractTestsBase
{
    [Fact]
    public async Task RemoveSupportedStakingTokensTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasury(daoId);

        var result = await TreasuryContractStub.AddSupportedStakingTokens.SendAsync(new AddSupportedStakingTokensInput
        {
            DaoId = daoId,
            Symbols = new SymbolList
            {
                Data = { "STR", "STR-1", "STR-2" }
            }
        });

        result = await TreasuryContractStub.RemoveSupportedStakingTokens.SendAsync(new RemoveSupportedStakingTokensInput
        {
            DaoId = daoId,
            Symbols = new SymbolList
            {
                Data = { "STR", "STR-1" }
            }
        });
        
        var treasuryInfo = await TreasuryContractStub.GetTreasuryInfo.CallAsync(daoId);
        treasuryInfo.ShouldNotBeNull();
        treasuryInfo.SupportedStakingTokens.Data.ShouldContain("STR-2");
        treasuryInfo.SupportedStakingTokens.Data.ShouldNotContain("STR");
    }
}