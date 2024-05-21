using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Shouldly;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractBaseTests : TestBase
{
    protected readonly Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");
    protected const string DefaultGovernanceToken = "ELF";


    protected async Task Initialize(Address daoAddress = null, Address voteAddress = null,
        Address governanceAddress = null)
    {
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress,
            GovernanceContractAddress = governanceAddress ?? GovernanceContractAddress
        };
        await ElectionContractStub.Initialize.SendAsync(input);
    }

    protected static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }
}