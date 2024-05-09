using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public class VoteContractTestBase : TestBase
{
    protected Hash ProposalId = HashHelper.ComputeFrom("ProposalId");
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected string TokenElf = "ELF";
    protected Hash DaoId;

    public async Task<IExecutionResult<Empty>> InitializeVote()
    {
        return await VoteContractStub.Initialize.SendAsync(new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
        });
    }
    
    public async Task InitializeAll()
    {
        //init governance contrct
        await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress
        });
        
        //init dao contract
        await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });
        
        //init vote contract
        await InitializeVote();

        UniqueVoteVoteSchemeId = await InitializeVoteScheme(VoteMechanism.UniqueVote);
        TokenBallotVoteSchemeId = await InitializeVoteScheme(VoteMechanism.TokenBallot);
        DaoId = await InitializeDao();
    }
    
    private async Task<Hash> InitializeVoteScheme(VoteMechanism voteMechanism)
    {
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = voteMechanism
        });

        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(VoteContractAddress),
            HashHelper.ComputeFrom(voteMechanism.ToString()));
    }
    
    private async Task<Hash> InitializeDao()
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput()
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia = { new Dictionary<string, string>(){{"aa", "bb"}} }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold()
            {
                MinimalRequiredThreshold = 5,
                MinimalVoteThreshold = 3,
                MinimalApproveThreshold = 5,
                MaximalRejectionThreshold = 5,
                MaximalAbstentionThreshold = 5
            }
        });
        
        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        return log.DaoId;
    }
    
    private static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }
}