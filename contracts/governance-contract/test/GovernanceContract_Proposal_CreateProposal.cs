using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
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
        var input = MockCreateProposalInput();
        var result = await CreateProposalAsync(input, false);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
    }

    [Fact]
    public async Task CreateProposalTest_EventTest()
    {
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false);
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

        LogEvent proposalCreatedEvent = null;
        foreach (var logEvent in executionResult.TransactionResult.Logs)
        {
            if (logEvent.Name.Contains(nameof(ProposalCreated)))
            {
                proposalCreatedEvent = logEvent;
            }
        }

        proposalCreatedEvent.ShouldNotBeNull();
        var url = ProposalCreated.Parser
            .ParseFrom(proposalCreatedEvent.NonIndexed)
            .ForumUrl;
        url.ShouldNotBeNull();
        url.ShouldContain("https://www.ForumUrl.com");
    }

    [Fact]
    public async Task CreateProposalTest_CannotBeUnusedOrVeto()
    {
        var input = MockCreateProposalInput();
        input.ProposalType = (int)ProposalType.Unused;
        var executionResult = await CreateProposalAsync(input, true);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");

        input.ProposalType = (int)ProposalType.Veto;
        executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");
    }

    [Fact]
    public async Task CreateProposalTest_ExecuteTransactionIsNull()
    {
        var input = MockCreateProposalInput();
        input.ProposalType = (int)ProposalType.Governance;
        input.Transaction = null;
        var executionResult = await CreateProposalAsync(input, true);
        _testOutputHelper.WriteLine(executionResult.TransactionResult.Error);
        executionResult.TransactionResult.Error.ShouldContain("Invalid input or parameter does not exist");
    }
}