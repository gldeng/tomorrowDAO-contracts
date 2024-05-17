using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalGetProposalInfo : GovernanceContractTestBase
{
    [Fact]
    public async Task GetProposalInfoTest()
    {
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false);
        var proposalId = executionResult.Output;

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