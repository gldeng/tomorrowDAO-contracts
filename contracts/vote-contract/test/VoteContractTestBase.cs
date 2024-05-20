using System;
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
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected string TokenElf = "ELF";
    protected Hash DaoId;
    protected Address HCSchemeAddress;
    protected Hash HCSchemeId;
    protected Address RSchemeAddress;
    protected Hash RSchemeId;
    
    protected Hash GovernanceR1A1VProposalId;
    protected Hash GovernanceR1T1VProposalId;
    protected Hash GovernanceHc1A1VProposalId;
    protected Hash GovernanceHc1T1VProposalId;
    
    protected Hash AdvisoryR1A1VProposalId;
    protected Hash AdvisoryR1T1VProposalId;
    protected Hash AdvisoryHc1A1VProposalId;
    protected Hash AdvisoryHc1T1VProposalId;

    protected Hash VetoR1A1VProposalId;
    protected Hash VetoR1T1VProposalId;

    public async Task<IExecutionResult<Empty>> InitializeVote()
    {
        return await VoteContractStub.Initialize.SendAsync(new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
        });
    }

    public async Task<IExecutionResult<Empty>> InitializeDAO()
    {
        return await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });
    }

    public async Task<IExecutionResult<Empty>> InitializeGovernance()
    {
        return await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress
        });
    }

    public async Task<IExecutionResult<Empty>> InitializeElection()
    {
        return await ElectionContractStub.Initialize.SendAsync(new Election.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        });
    }

    public async Task InitializeAll()
    {
        await InitializeGovernance();
        await InitializeDAO();
        await InitializeElection();
        await InitializeVote();
        await CreateVoteScheme(VoteMechanism.UniqueVote);
        await CreateVoteScheme(VoteMechanism.TokenBallot);
        await CreateDao();
    }

    private async Task CreateVoteScheme(VoteMechanism voteMechanism)
    {
        var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = voteMechanism
        });

        var log = GetLogEvent<VoteSchemeCreated>(result.TransactionResult);
        switch (voteMechanism)
        {
            case VoteMechanism.UniqueVote:
                UniqueVoteVoteSchemeId = log.VoteSchemeId;
                break;
            case VoteMechanism.TokenBallot:
                TokenBallotVoteSchemeId = log.VoteSchemeId;
                break;
        } 
    }

    public async Task CreateDao(bool isNetworkDao = false)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(GetCreateDAOInput(isNetworkDao));

        var dAOCreatedLog = GetLogEvent<DAOCreated>(result.TransactionResult);
        DaoId = dAOCreatedLog.DaoId;

        var governanceSchemeAddedLogs = GetMultiLogEvent<GovernanceSchemeAdded>(result.TransactionResult);
        foreach (var governanceSchemeAddedLog in governanceSchemeAddedLogs)
        {
            if (governanceSchemeAddedLog.GovernanceMechanism == (Governance.GovernanceMechanism)GovernanceMechanism.HighCouncil)
            {
                HCSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                HCSchemeId = governanceSchemeAddedLog.SchemeId;
            }
            else
            {
                RSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                RSchemeId = governanceSchemeAddedLog.SchemeId;
            }
        }
    }

    protected async Task<Hash> CreateProposal(ProposalType proposalType, Address schemeAddress, Hash voteSchemeId)
    {
        var result = await GovernanceContractStub.CreateProposal.SendAsync(GetCreateProposalInput(proposalType, schemeAddress, voteSchemeId));
        result.TransactionResult.Error.ShouldBe("");
        var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
        return governanceProposalLog.ProposalId;
    }
    
    protected async Task<Hash> CreateVetoProposal(Hash voteSchemeId)
    {
        var result = await GovernanceContractStub.CreateVetoProposal.SendAsync(GetCreateVetoProposalInput(voteSchemeId));
        result.TransactionResult.Error.ShouldBe("");
        var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
        return governanceProposalLog.ProposalId;
    }

    protected static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    protected static List<T> GetMultiLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var res = new List<T>();
        foreach (var log in transactionResult.Logs.Where(log => log.Name == typeof(T).Name))
        {
            var logEvent = new T();
            logEvent.MergeFrom(log.NonIndexed);
            res.Add(logEvent);
        }

        return res;
    }

    protected CreateDAOInput GetCreateDAOInput(bool isNetworkDao = false)
    {
        return new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string> { { "aa", "bb" } }
                }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 100000000,
                MinimalApproveThreshold = 5000,
                MaximalRejectionThreshold = 2000,
                MaximalAbstentionThreshold = 2000
            },
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = new HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 2,
                    MaxHighCouncilCandidateCount = 20,
                    ElectionPeriod = 7,
                    StakingAmount = 100000000
                },
                GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2000,
                    MaximalAbstentionThreshold = 2000
                }
            },
            IsTreasuryContractNeeded = false,
            IsNetworkDao = isNetworkDao
        };
    }

    protected CreateProposalInput GetCreateProposalInput(ProposalType proposalType, Address schemeAddress, Hash voteSchemeId)
    {
        return new CreateProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteSchemeId
            },
            ProposalType = (int)proposalType,
            Transaction = new ExecuteTransaction
            {
                ContractMethodName = "ContractMethodName",
                ToAddress = UserAddress,
                Params = ByteStringHelper.FromHexString(StringExtensions.GetBytes("Params").ToHex())
            }
        };
    }
    
    protected CreateVetoProposalInput GetCreateVetoProposalInput(Hash voteSchemeId)
    {
        return new CreateVetoProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = RSchemeAddress,
                VoteSchemeId = voteSchemeId
            },
            VetoProposalId = GovernanceHc1T1VProposalId
        };
    }
}