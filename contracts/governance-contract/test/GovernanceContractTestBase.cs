using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestBase : TestBase
{
    protected Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");

    internal readonly GovernanceSchemeThreshold DefaultSchemeThreshold = new GovernanceSchemeThreshold
    {
        MinimalRequiredThreshold = 1,
        MinimalVoteThreshold = 1,
        MinimalApproveThreshold = 1,
        MaximalRejectionThreshold = 2,
        MaximalAbstentionThreshold = 2
    };

    protected readonly string DefaultGovernanceToken = "ELF";

    protected Hash DefaultVoteSchemeId = HashHelper.ComputeFrom("DefaultVoteSchemeId");

    protected async Task Initialize(Address daoAddress = null, Address voteAddress = null)
    {
        //init governance contrct
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress
        };
        await GovernanceContractStub.Initialize.SendAsync(input);
    }

    public async Task InitializeAll(Address daoAddress = null, Address voteAddress = null)
    {
        //init governance contrct
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress
        };
        await GovernanceContractStub.Initialize.SendAsync(input);
        
        //init vote contract
        await VoteContractStub.Initialize.SendAsync(new Vote.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            ElectionContractAddress = ElectionContractAddress
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

        DefaultVoteSchemeId = await InitializeVoteScheme();
        DefaultDaoId= await InitializeDao();
    }

    private async Task<Hash> InitializeVoteScheme()
    {
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.UniqueVote
        });

        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(VoteContractAddress),
            HashHelper.ComputeFrom(VoteMechanism.UniqueVote.ToString()));
    }

    private async Task<Hash> InitializeDao()
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
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

    internal async Task<Address> AddGovernanceScheme(Hash daoId = default,
        GovernanceMechanism mechanism = GovernanceMechanism.Referendum, GovernanceSchemeThreshold threshold = null,
        string governanceToken = null)
    {
        var input = new AddGovernanceSchemeInput
        {
            DaoId = daoId ?? DefaultDaoId,
            GovernanceMechanism = mechanism,
            SchemeThreshold = threshold ?? DefaultSchemeThreshold,
            GovernanceToken = governanceToken ?? DefaultGovernanceToken
        };

        var executionResult = await GovernanceContractStub.AddGovernanceScheme.SendAsync(input);
        return executionResult.Output;
    }

    internal async Task<Hash> CreateProposal(Address schemeAddress, Hash voteSchemeId = null)
    {
        var input = MockCreateProposalInput(schemeAddress, voteSchemeId);
        var result = await GovernanceContractStub.CreateProposal.SendAsync(input);
        return result.Output;
    }

    internal async Task<Hash> CreateProposal(CreateProposalInput input)
    {
        var result = await GovernanceContractStub.CreateProposal.SendAsync(input);
        return result.Output;
    }

    internal CreateProposalInput MockCreateProposalInput(Address schemeAddress, Hash voteSchemeId = null)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = DefaultDaoId,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = schemeAddress,
            VoteSchemeId = voteSchemeId ?? DefaultVoteSchemeId
        };
        var executeTransaction = new ExecuteTransaction
        {
            ContractMethodName = "ContractMethodName",
            ToAddress = UserAddress,
            Params = ByteStringHelper.FromHexString(StringExtensions.GetBytes("Params").ToHex())
        };

        var input = new CreateProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            ProposalType = ProposalType.Governance,
            Transaction = executeTransaction
        };
        return input;
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