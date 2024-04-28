using System.Threading.Tasks;
using TomorrowDAO.Contracts.DAO;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractSchemeRemoveScheme : GovernanceContractTestBase
{
    [Fact]
    public async Task RemoveSchemeTest()
    {
        await Initialize(DefaultAddress);
        var address = await AddGovernanceScheme();

        var setSubsistStatusInput = new SetSubsistStatusInput
        {
            DaoId = DefaultDaoId,
            Status = true
        };
        await DAOContractStub.SetSubsistStatus.SendAsync(setSubsistStatusInput);

        var inpute = new RemoveGovernanceSchemeInput
        {
            DaoId = DefaultDaoId,
            SchemeAddress = address
        };

        await GovernanceContractStub.RemoveGovernanceScheme.SendAsync(inpute);
    }
    
}