using System.Threading.Tasks;
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
        await InitializeAll();

        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(DefaultDaoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(1);
        var schemeAddress = addressList.Value.FirstOrDefault();

        var result = await CreateProposal(schemeAddress);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
    }

    [Fact]
    public async Task CreateProposalTest_EventTest()
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

        var url = ProposalCreated.Parser
            .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed)
            .ForumUrl;
        url.ShouldNotBeNull();
        url.ShouldContain("https://www.ForumUrl.com");
    }

    [Fact]
    public async Task CreateProposalTest_Exists()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();

        var input = MockCreateProposalInput(schemeAddress);
        var result = await CreateProposal(input);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);

        //Proposal Id will never be duplicated
        //var executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
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

    [Fact]
    public async Task CreateProposalTest_ExecuteTransactionIsNull()
    {
        await Initialize(DefaultAddress);
        var schemeAddress = await AddGovernanceScheme();

        var input = MockCreateProposalInput(schemeAddress);
        input.Transaction = null;

        var executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        _testOutputHelper.WriteLine(executionResult.TransactionResult.Error);
        executionResult.TransactionResult.Error.ShouldContain("Invalid input or parameter does not exist");
    }
}