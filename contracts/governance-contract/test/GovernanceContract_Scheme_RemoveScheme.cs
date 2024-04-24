using System.Threading.Tasks;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractSchemeRemoveScheme : GovernanceContractTestBase
{
    [Fact]
    public async Task RemoveSchemeTest()
    {
        await Initialize();
        var address = await AddGovernanceScheme();

        var inpute = new RemoveGovernanceSchemeInput
        {
            DaoId = DefaultDaoId,
            SchemeAddress = address
        };

        await GovernanceContractStub.RemoveGovernanceScheme.SendAsync(inpute);
    }
    
}