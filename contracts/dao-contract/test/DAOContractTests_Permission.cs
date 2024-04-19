using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Theory]
    [InlineData("SetSubsistStatus", true, true, false)]
    [InlineData("UploadFileInfos", false, false, false)]
    [InlineData("RemoveFileInfos", false, false, false)]
    [InlineData("AddGovernanceScheme", true, true, false)]
    public async Task HashPermissionTests(string methodName, bool referendumPermission, bool highCouncilPermission, bool otherPermission)
    {
        await InitializeAsync();
        
        await GovernanceContractStub.Initialize.SendAsync(new TestContracts.Governance.InitializeInput
        {
            Referendum = ReferendumAddress,
            HighCouncil = HighCouncilAddress
        });
        
        var daoId = await CreateDAOAsync();

        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = ReferendumAddress,
                What = methodName
            });
            result.Value.ShouldBe(referendumPermission);
        }
        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = HighCouncilAddress,
                What = methodName
            });
            result.Value.ShouldBe(highCouncilPermission);
        }
        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = OtherAddress,
                What = methodName
            });
            result.Value.ShouldBe(otherPermission);
        }
    }
}