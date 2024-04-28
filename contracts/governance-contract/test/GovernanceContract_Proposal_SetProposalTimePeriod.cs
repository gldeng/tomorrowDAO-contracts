using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalSetProposalTimePeriod : GovernanceContractTestBase
{
    [Fact]
    public async Task SetProposalTimePeriodTest()
    {
        await Initialize();
        var input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
            ProposalTimePeriod = new DaoProposalTimePeriod
            {
                ActiveTimePeriod = 1,
                VetoActiveTimePeriod = 1,
                PendingTimePeriod = 1,
                ExecuteTimePeriod = 1,
                VetoExecuteTimePeriod = 1
            }
        };
        await GovernanceContractStub.SetProposalTimePeriod.SendAsync(input);
        var timePeriod = await GovernanceContractStub.GetDaoProposalTimePeriod.CallAsync(DefaultDaoId);
        timePeriod.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetProposalTimePeriodTest_InvalidInput()
    {
        await Initialize();
        var result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(new SetProposalTimePeriodInput());
        result.TransactionResult.Error.ShouldContain("Invalid input");
        
        var input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
        };
        result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Invalid input");
        
        input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
            ProposalTimePeriod = new DaoProposalTimePeriod
            {
                VetoActiveTimePeriod = 1,
                PendingTimePeriod = 1,
                ExecuteTimePeriod = 1,
                VetoExecuteTimePeriod = 1
            }
        };
        result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Invalid input");
    }
}