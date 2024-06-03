using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsCreateCreateTreasury : TreasuryContractTestsBase
{
    [Fact]
    public async Task CreateTreasuryTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();

        var result = await TreasuryContractStub.CreateTreasury.SendAsync(new CreateTreasuryInput
            {
                DaoId = daoId,
                Symbols = new SymbolList
                {
                    Data = { DefaultGovernanceToken, "USDT" }
                }
            });
        result.ShouldNotBeNull();

        var address = await TreasuryContractStub.GetTreasuryAccountAddress.CallAsync(daoId);
        address.ShouldNotBeNull();

        var treasuryInfo = await TreasuryContractStub.GetTreasuryInfo.CallAsync(daoId);
        treasuryInfo.ShouldNotBeNull();
        treasuryInfo.SupportedStakingTokens.Data.ShouldContain(DefaultGovernanceToken);
    }
}