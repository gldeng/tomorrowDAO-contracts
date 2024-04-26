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
    
    [Fact]
    public async Task CreateProposalTest_Fo()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();
        
        var input = MockCreateProposalInput(schemeAddress);
        var executionResult = await GovernanceContractStub.CreateProposal.SendAsync(input);
        var logEvents = executionResult.TransactionResult.Logs;
        var existProposalCreated = false;
        foreach (var logEvent in logEvents)
        {
            if (logEvent.Name == "ProposalCreated")
            {
                existProposalCreated = true;
            }
        }
        existProposalCreated.ShouldBe(true);
    }

    [Fact]
    public async Task CreateProposalTest_Exists()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();

        var result = await CreateProposal(schemeAddress);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
        
        var input = MockCreateProposalInput(schemeAddress);

        var executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        //executionResult.TransactionResult.Error.ShouldContain("Proposal already exists");
    }
    
    [Fact]
    public async Task CreateProposalTest_CannotBeUnusedOrVeto()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();
        
        var input = MockCreateProposalInput(schemeAddress);
        input.ProposalType = ProposalType.Unused;
        var executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");
        
        input.ProposalType = ProposalType.Veto;
        executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");
    }
}