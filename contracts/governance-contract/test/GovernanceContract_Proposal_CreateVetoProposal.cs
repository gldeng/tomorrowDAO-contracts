using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalCreateVetoProposal : GovernanceContractTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GovernanceContractProposalCreateVetoProposal(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CreateVetoProposal()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();
        var vetoProposalId = await CreateProposal(schemeAddress);

        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = DefaultDaoId,
            ProposalTitle = "VetoProposalTitle",
            ProposalDescription = "VetoProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = schemeAddress,
            VoteSchemeId = DefaultVoteSchemeId
        };
        var proposalInput = new CreateVetoProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            VetoProposalId = vetoProposalId
        };
        var result = await GovernanceContractStub.CreateVetoProposal.SendAsync(proposalInput);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine(result.Output.ToString());
    }
}