using System.Threading.Tasks;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractSchemeUpdateThreshold : GovernanceContractTestBase
{
    [Fact]
    public async Task UpdateSchemeThresholdTest()
    {
        await Initialize(DefaultAddress);
        var address = await AddGovernanceScheme();
        
        var inpute = new UpdateGovernanceSchemeThresholdInput
        {
            DaoId = DefaultDaoId,
            SchemeAddress = address,
            SchemeThreshold = null
        };

        await GovernanceContractStub.UpdateGovernanceSchemeThreshold.SendAsync(inpute);
    }
}