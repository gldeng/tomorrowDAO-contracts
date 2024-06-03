using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractCreateAddTokens : TreasuryContractTestsBase
{
    [Fact]
    public async Task AddSupportedStakingTokensTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasury(daoId);

        var result = await TreasuryContractStub.AddSupportedStakingTokens.SendAsync(new AddSupportedStakingTokensInput
        {
            DaoId = daoId,
            Symbols = new SymbolList
            {
                Data = { "STR" }
            }
        });

        var treasuryInfo = await TreasuryContractStub.GetTreasuryInfo.CallAsync(daoId);
        treasuryInfo.ShouldNotBeNull();
        treasuryInfo.SupportedStakingTokens.Data.ShouldContain("STR");
    }
}