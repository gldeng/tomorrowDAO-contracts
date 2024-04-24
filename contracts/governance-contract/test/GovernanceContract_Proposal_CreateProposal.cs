using System;
using System.Threading.Tasks;
using AElf;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalCreateProposal : GovernanceContractTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GovernanceContractProposalCreateProposal(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CreateProposalTest()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();

        var result = await CreateProposal(schemeAddress);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
    }
}