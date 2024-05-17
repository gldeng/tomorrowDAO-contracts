using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.CSharp.Core.Extension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAO.Contracts.Vote;
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

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task CreateVetoProposalTest()
    {
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime());
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false, VoteMechanism.TokenBallot);
        var vetoProposalId = executionResult.Output;
        
        //Election
        await HighCouncilElection(input.ProposalBasicInfo.DaoId);

        //Vote 10s
        BlockTimeProvider.SetBlockTime(10000);
        await VoteProposalAsync(vetoProposalId, OneElfAmount, VoteOption.Approved);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
        var vetoProposalInput = MockCreateVetoProposalInput();
        vetoProposalInput.VetoProposalId = vetoProposalId;
        var result = await CreateVetoProposalAsync(vetoProposalInput, false);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine(result.Output.ToString());
    }
}