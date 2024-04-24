using System.Threading.Tasks;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractSchemeSetToken : GovernanceContractTestBase
{
    [Fact]
    public async Task SetGovernanceTokenTest()
    {
        await Initialize();
        var address = await AddGovernanceScheme();

        var inpute = new SetGovernanceTokenInput
        {
            DaoId = DefaultDaoId,
            GovernanceToken = "CPU"
        };

        await GovernanceContractStub.SetGovernanceToken.SendAsync(inpute);
    }
}