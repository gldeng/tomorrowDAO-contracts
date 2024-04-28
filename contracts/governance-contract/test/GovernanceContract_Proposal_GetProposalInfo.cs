using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalGetProposalInfo : GovernanceContractTestBase
{
    [Fact]
    public async Task GetProposalInfoTest()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();
        var proposalId = await CreateProposal(schemeAddress);

        var proposalInfo = await GovernanceContractStub.GetProposalInfo.CallAsync(proposalId);
        proposalInfo.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task GetProposalInfoTest_NotExist()
    {
        var proposalInfo = await GovernanceContractStub.GetProposalInfo.CallAsync(DefaultDaoId);
        proposalInfo.ShouldNotBeNull();
        proposalInfo.DaoId.ShouldBeNull();
    }
}