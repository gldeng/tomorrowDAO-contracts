using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using AnonymousVote;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using HighCouncilConfig = TomorrowDAO.Contracts.DAO.HighCouncilConfig;

namespace TomorrowDAO.Contracts.Vote;

public class VoteContractTestBase : TestBase
{
    internal IBlockTimeProvider BlockTimeProvider;
    
    public const int UniqueVoteVoteAmount = 1;
    public const long OneElf = 1_00000000;
    public const long ActiveTimePeriod = 7 * 24 * 60 * 60;
    public const long VetoActiveTimePeriod = 3 * 24 * 60 * 60;
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected Hash TokenBallotVoteSchemeId_NoLock_DayVote; 
    protected string TokenElf = "ELF";
    protected Hash DaoId;
    protected Hash OrganizationDaoId; // organization dao
    protected Hash NetworkDaoId;
    protected Address HcSchemeAddress;
    protected Hash HcSchemeId;
    protected Address RSchemeAddress;
    protected Hash RSchemeId;
    protected Address OSchemeAddress; // organization
    protected Hash OSchemeId;
    protected Address NetworkDaoHcSchemeAddress;
    protected Hash NetworkDaoHcSchemeId;
    protected Address NetworkDaoRSchemeAddress;
    protected Hash NetworkDaoRSchemeId;
    
    protected Hash GovernanceR1A1VProposalId;
    protected Hash GovernanceR1T1VProposalId;
    protected Hash GovernanceHc1A1VProposalId;
    protected Hash GovernanceHc1T1VProposalId;
    protected Hash GovernanceO1A1VProposalId;
    protected Hash GovernanceO1T1VProposalId;
    
    protected Hash AdvisoryR1A1VProposalId;
    protected Hash AdvisoryR1T1VProposalId;
    protected Hash AdvisoryR1T1VProposalId_NoLock_DayVote;
    protected Hash AdvisoryHc1A1VProposalId;
    protected Hash AdvisoryHc1T1VProposalId;
    protected Hash AdvisoryO1A1VProposalId;
    protected Hash AdvisoryO1T1VProposalId;

    protected Hash VetoR1A1VProposalId;
    protected Hash VetoR1T1VProposalId;
    
    protected Hash NetworkDaoGovernanceR1A1VProposalId;
    protected Hash NetworkDaoGovernanceR1T1VProposalId;
    protected Hash NetworkDaoGovernanceHc1A1VProposalId;
    protected Hash NetworkDaoGovernanceHc1T1VProposalId;
    
    protected Hash NetworkDaoAdvisoryR1A1VProposalId;
    protected Hash NetworkDaoAdvisoryR1T1VProposalId;
    protected Hash NetworkDaoAdvisoryHc1A1VProposalId;
    protected Hash NetworkDaoAdvisoryHc1T1VProposalId;

    public VoteContractTestBase()
    {
        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
    }
    
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
        var result = await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });
        await DAOContractStub.SetTreasuryContractAddress.SendAsync(TreasuryContractAddress);
        return result;
    }

    public async Task<IExecutionResult<Empty>> InitializeGovernance()
    {
        return await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress
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
        await CreateVoteScheme(VoteMechanism.TokenBallot, true, VoteStrategy.DayDistinct);
        await CreateDao("DAO", true);
        await CreateDao("NetworkDAO");
        await CreateDao("Organization DAO", false, 2);
    }

    private async Task CreateVoteScheme(VoteMechanism voteMechanism, bool withoutLockToken = false, 
        VoteStrategy voteStrategy = VoteStrategy.ProposalDistinct)
    {
        var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = voteMechanism, WithoutLockToken = withoutLockToken, VoteStrategy = voteStrategy
        });

        var log = GetLogEvent<VoteSchemeCreated>(result.TransactionResult);
        switch (voteMechanism)
        {
            case VoteMechanism.UniqueVote:
                UniqueVoteVoteSchemeId = log.VoteSchemeId;
                break;
            case VoteMechanism.TokenBallot:
                if (withoutLockToken &&  VoteStrategy.DayDistinct == voteStrategy)
                {
                    TokenBallotVoteSchemeId_NoLock_DayVote = log.VoteSchemeId;
                }
                else
                {
                    TokenBallotVoteSchemeId = log.VoteSchemeId;
                }
                break;
        } 
    }

    private async Task CreateVoteScheme_NoLock_DayVote(VoteMechanism voteMechanism)
    {
        var result3 = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot, WithoutLockToken = true, VoteStrategy = VoteStrategy.DayDistinct
        });
    }

    public async Task CreateDao(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(GetCreateDAOInput(daoName, isNetworkDao, governanceMechanism));
        var dAOCreatedLog = GetLogEvent<DAOCreated>(result.TransactionResult);
        if (isNetworkDao)
        {
            NetworkDaoId = dAOCreatedLog.DaoId;
        }
        else
        {
            if (governanceMechanism == 2)
            {
                OrganizationDaoId = dAOCreatedLog.DaoId;
            }
            else
            {
                DaoId = dAOCreatedLog.DaoId;
            }
        }
        
        var governanceSchemeAddedLogs = GetMultiLogEvent<GovernanceSchemeAdded>(result.TransactionResult);
        foreach (var governanceSchemeAddedLog in governanceSchemeAddedLogs)
        {
            switch (governanceSchemeAddedLog.GovernanceMechanism)
            {
                case (Governance.GovernanceMechanism)GovernanceMechanism.HighCouncil when isNetworkDao:
                    NetworkDaoHcSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    NetworkDaoHcSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.HighCouncil:
                    HcSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    HcSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Referendum when isNetworkDao:
                    NetworkDaoRSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    NetworkDaoRSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Referendum:
                    RSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    RSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Organization:
                    OSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    OSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
            }
        }
    }

    internal async Task<Hash> CreateProposal(Hash DaoId, ProposalType proposalType, Address schemeAddress, Hash voteSchemeId, string error = "", bool anonymous=false)
    {
        IExecutionResult<Hash> result;
        if (string.IsNullOrEmpty(error))
        {
            result = await GovernanceContractStub.CreateProposal.SendAsync(GetCreateProposalInput(DaoId, proposalType, schemeAddress, voteSchemeId, anonymous));
            result.TransactionResult.Error.ShouldBe(error);
            var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
            return governanceProposalLog.ProposalId;
        }

        if (!anonymous)
        {
            result = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(GetCreateProposalInput(DaoId, proposalType, schemeAddress, voteSchemeId, anonymous));
            result.TransactionResult.Error.ShouldContain(error);   
        }
        return null;
    }
    
    protected async Task HighCouncilElection(Hash daoId)
    {
        await ApproveElf(OneElf * 100, ElectionContractAddress);;
        await ElectionContractStub.AnnounceElection.SendAsync(new AnnounceElectionInput
        {
            DaoId = daoId,
            CandidateAdmin = DefaultAddress
        });
        await ElectionVote(DefaultAddress);
        //var result = await TakeSnapshot(DaoId, 1);
        (await ElectionContractStub.GetHighCouncilMembers.CallAsync(DaoId)).Value.ShouldContain(DefaultAddress);
    }
    
    protected async Task HighCouncilElectionFor(Hash daoId, Address candidateAddress)
    {
        await ApproveElf(OneElf * 100, ElectionContractAddress);
        await ElectionContractStub.AnnounceElectionFor.SendAsync(new AnnounceElectionForInput
        {
            DaoId = daoId, Candidate = candidateAddress, CandidateAdmin = DefaultAddress
        });
        await ElectionVote(candidateAddress);
        //var result = await TakeSnapshot(DaoId, 2);
        (await ElectionContractStub.GetHighCouncilMembers.CallAsync(DaoId)).Value.ShouldContain(candidateAddress);
    }

    protected async Task ApproveElf(long amount, Address spender)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput { Spender = spender, Symbol = TokenElf, Amount = amount });
    }

    protected async Task<IExecutionResult<Hash>> ElectionVote(Address candidateAddress)
    {
        return await ElectionContractStub.Vote.SendAsync(new VoteHighCouncilInput
        {
            DaoId = DaoId, CandidateAddress = candidateAddress, Amount = OneElf * 10,
            EndTimestamp = DateTime.UtcNow.AddDays(4).ToTimestamp(), Token = null
        });
    }

    protected async Task<IExecutionResult<Empty>> TakeSnapshot(Hash daoId, long termNumber)
    {
        return await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput { DaoId = daoId, TermNumber = termNumber });
    }

    internal async Task<IExecutionResult<Empty>> Vote(long amount, VoteOption voteOption, Hash votingItemId)
    {
        var result = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = amount, VoteOption = (int)voteOption, VotingItemId = votingItemId, Memo = "memo"});
        result.TransactionResult.Error.ShouldBe("");
        return result;
    }
    
    internal async Task<long> GetDaoRemainAmount(Hash daoId, Address voter, long amount)
    {
        var result = (await VoteContractStub.GetDaoRemainAmount.CallAsync(
            new GetDaoRemainAmountInput { DaoId = DaoId, Voter = DefaultAddress })).Amount;
        result.ShouldBe(amount);
        return result;
    }
    
    internal async Task<long> GetDaoProposalRemainAmount(Hash daoId, Address voter, Hash votingItemId, long amount)
    {
        var result = (await VoteContractStub.GetProposalRemainAmount.CallAsync(
            new GetProposalRemainAmountInput { DaoId = DaoId, Voter = DefaultAddress, VotingItemId = votingItemId})).Amount;
        amount.ShouldBe(amount);
        return result;
    }

    internal async Task<IExecutionResult<Empty>> Withdraw(Hash daoId, VotingItemIdList list, long withdrawAmount)
    {
        var result = await VoteContractStub.Withdraw.SendAsync(new WithdrawInput { DaoId = daoId, VotingItemIdList = list, WithdrawAmount = withdrawAmount });
        result.TransactionResult.Error.ShouldBe("");
        return result;
    }

    internal async Task<IExecutionResult<Empty>> VoteException(long amount, VoteOption voteOption, Hash votingItemId, string error)
    {
        var result = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = amount, VoteOption = (int)voteOption, VotingItemId = votingItemId });
        result.TransactionResult.Error.ShouldContain(error);
        return result;
    }

    internal async Task<Hash> CreateVetoProposal(Address schemeAddress, Hash voteSchemeId, Hash vetoProposalId)
    {
        var result = await GovernanceContractStub.CreateVetoProposal.SendAsync(GetCreateVetoProposalInput(schemeAddress, voteSchemeId, vetoProposalId));
        result.TransactionResult.Error.ShouldBe("");
        var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
        return governanceProposalLog.ProposalId;
    }

    internal static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private static List<T> GetMultiLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
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

    private CreateDAOInput GetCreateDAOInput(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        return new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string> { { "aa", "bb" } }
                }
            },
            GovernanceToken = governanceMechanism == 2 ? "" : "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 0,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0
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
                },
                HighCouncilMembers = new DAO.AddressList(){Value = { new []{DefaultAddress, UserAddress} }},
                IsHighCouncilElectionClose = false
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao,
            GovernanceMechanism = governanceMechanism,
            Members = new DAO.AddressList{Value = { DefaultAddress }}
        };
    }

    private CreateProposalInput GetCreateProposalInput(Hash DaoId, ProposalType proposalType, Address schemeAddress, Hash voteSchemeId, bool anonymous=false)
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
                VoteSchemeId = voteSchemeId,
                ActiveTimePeriod = ActiveTimePeriod,
                IsAnonymous = anonymous
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

    private CreateVetoProposalInput GetCreateVetoProposalInput(Address schemeAddress, Hash voteSchemeId, Hash vetoProposalId)
    {
        return new CreateVetoProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteSchemeId,
                ActiveTimePeriod = VetoActiveTimePeriod 
            },
            VetoProposalId = vetoProposalId
        };
    }

    #region Anonymous Vote

    internal AnonymousVoteContractContainer.AnonymousVoteContractStub AnonymousVoteContractStub { get; set; }
    internal AnonymousVoteAdmin.AnonymousVoteAdminContractContainer.AnonymousVoteAdminContractStub AnonymousVoteAdminContractStub
    {
        get;
        set;
    }
    
        protected async Task<Address> DeployGroth16VerifierContractAsync()
        {
            var b64Code =
            "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gwKZCIAAeEwAAAAAkQDqAb0IiQC8TAAAAACWAEQPZgCKANNMAAAAAJEYiQ5mAIoAKyEAAAAAhhiDDsoAigDkTAAAAACDAKsAOQmKAHBNAAAAAJYARA9mAIsAiE0AAAAAlgjVDiAAiwCUTQAAAACRGIkOZgCLAAZOAAAAAJYI1Q6dCYsAIE4AAAAAlgiMEKwJiwB5TgAAAACWALUE/QmLAKxOAAAAAJEYiQ5mAIwAO08AAAAAxgEGCSoGjABHTwAAAACEGIMOygCNAFRPAAAAAJEYiQ5mAI0AKyEAAAAAhhiDDsoAjQBlTwAAAACDAAEAuAqNAGVPAAAAAIMAxQC/Co4Ack8AAAAAlgBTDswKjwB8TwAAAACWAI8RZgCQAJxPAAAAAJYAmxFmAJAAAAABAMINAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAMINAAABAIsIAAABAIsIAAABAMINAAABAMINAAABAAASAAABAAASAAABAMINAAABAPoRAAABAPoRAAABAPoRAAABAFwSAAACAL8SAAABAFwSAAACAL8SAAABAM8BAAACAFsCAAADANIBAAAEAF4CAAABAPoRAAABAPoRAAABAIsIAAABAIsIAAABAFwSAAABAIsIAAABAIsIAAABAIsIAAABAIsIAAABAIsIAAABAIsIAAABAIsI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MwGTAGkBYwCTAGkBawDUAIkBYwCTAIkBawDUAKABYwCTAKABawDUAKMBMwGTAKkBYwCTAKkBawDUAMABYwCTAMABawDUAMkBYwCTAMkBawDUAOABYwCTAOABawDUAOMBMwGTAOkBYwCTAOkBawDUAAACYwCTAAACawDUAAMCMwGTAAkCYwCTAAkCawDUACACYwCTACACawDUACkCYwCTACkCawDUAEACYwCTAEACawDUAEkCYwCTAEkCawDUAGACYwCTAGACawDUAGMCMwGTAGkCYwCTAGkCawDUAIACYwCTAIACawDUAIkCYwCTAIkCawDUAKACYwCTAKACawDUAKMCMwGTAKkCYwCTAKkCawDUAMACYwCTAMACawDUAMkCYwCTAMkCawDUAOkCYwCTAOkCawDUAAMDMwGTAAkDYwCTAAkDawDUACMDMwGTACkDYwCTACkDawDUAEkDYwCTAEkDawDUAGkDYwCTAGkDawDUAIkDYwCTAIkDawDUAKkDYwCTAKkDawDUAMkDYwCTAMkDawDUAOMDywOTAOkDYwCTAOkDawDUAAAEYwCTAAAEawDUAAMEMwGTAAkEYwCTAAkEawDUACAEYwCTACAEawDUACkEYwCTACkEawDUAEAEYwCTAEAEawDUAGkEYwCTAGkEawDUAIMEMwGTAIkEYwCTAIkEawDUAKkEYwCTAKkEawDUAMAEYwCTAMAEawDUAMkEYwCTAMkEawDUAOAEYwCTAOAEawDUAOkEYwCTAOkEawDUAAAFYwCTAAAFawDUAAkFYwCTAAkFawDUACAFYwCTACAFawDUACkFYwCTACkFawDUAEAFYwCTAEAFawDUAEkFYwCTAEkFawDUAGAFYwCTAGAFawDUAGkFYwCTAGkFawDUAIAFYwCTAIAFawDUAIkFYwCTAIkFawDUAKAFYwCTAKAFawDUAMAFYwCTAMAFawDUAOAFYwCTAOAFawDUAIAGYwCTAIAGawDUAKAGYwCTAKAGawDUAMAGYwCTAMAGawDUAGAHYwCTAGAHawDUAIAHYwCTAIAHawDUAKAHYwCTAKAHawDUAMAHYwCTAMAHawDUAOAHYwCTAOAHawDUAAAIYwCTAAAIawDUACAIYwCTACAIawDUAEAIYwCTAEAIawDUAGAIYwCTAGAIawDUAIAIYwCTAIAIawDUACEJMwGTAEEJMwGTAGEJMwGTAIAJYwCTAIAJawDUAIEJMwGTAKAJYwCTAKAJawDUAKEJMwGTAMAJYwCTAMAJawDUAMEJMwGTAOEJMwGTAAEKMwGTACEKMwGTAGAKYwCTAGAKawDUAGEKSwTxCIAKYwCTAIAKawDUAKAKYwCTAKAKawDUAMAKYwCTAMAKawDUAOAKYwCTAOAKawDUAAALYwCTAAALawDUACALYwCTACALawDUAEALYwCTAEALawDUAEELEwWTAGALYwCTAGALawDUAIALYwCTAIALawDUAIAMYwCTAIAMawDUAKAMYwCTAKAMawDUAMAMYwCTAMAMawDUAGANYwCTAGANawDUAIANYwCTAIANawDUAKANYwCTAKANawDUAMANYwCTAMANawDUAOANYwCTAOANawDUAAAOYwCTAAAOawDUACAOYwCTACAOawDUAEAOYwCTAEAOawDUAGAOYwCTAGAOawDUAIAOYwCTAIAOawDUAIAPYwCTAIAPawDUAKAPYwCTAKAPawDUAMAPYwCTAMAPawDUAKAQYwCTAKAQawDUAMAQYwCTAMAQawDUAOAQYwCTAOAQawDUAAARYwCTAAARawDUACARYwCTACARawDUAEARYwCTAEARawDUAEQRSwRWCWARYwCTAGARawDUAIARYwCTAIARawDUAKARYwCTAKARawDUAMARYwCTAMARawDUAGATYwCTAGATawDUAIATYwCTAIATawDUAKATYwCTAKATawDUACAUYwCTACAUawDUAEAUYwCTAEAUawDUAGAUYwCTAGAUawDUAIAUYwCTAIAUawDUAKAUYwCTAKAUawDUAMAUYwCTAMAUawDUAOAUYwCTAOAUawDUAAAVYwCTAAAVawDUACAVYwCTACAVawDUAEAVYwCTAEAVawDUAEAWYwCTAEAWawDUAGAWYwCTAGAWawDUAIAWYwCTAIAWawDUACAXYwCTACAXawDUAEAXYwCTAEAXawDUAGAXYwCTAGAXawDUAIAXYwCTAIAXawDUAKAXYwCTAKAXawDUAMAXYwCTAMAXawDUAOAXYwCTAOAXawDUAAAYYwCTAAAYawDUACAYYwCTACAYawDUAEAYYwCTAEAYawDUAAAaMwGTACAaMwGTAAAbMwGTACAbMwGTAEAbMwGTAGAbMwGTAIAbMwGTAKAbMwGTAOAbMwGTAAAc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XzAAPFBhaXJpbmc+Yl9fN18wAE1ha2VHMQBQMQA8LmNjdG9yPmJfXzEwXzEAPEFzc2VydElucHV0SXNWYWxpZD5nX19Bc3NlcnRWYWxpZEJpZ0ludHw2XzEARmllbGRDb2RlY2AxAEZ1bmNgMQBSZXBlYXRlZEZpZWxkYDEASU1lc3NhZ2VgMQBJRGVlcENsb25lYWJsZWAxAElFbnVtZXJhYmxlYDEASUVxdWF0YWJsZWAxAE1hcnNoYWxsZXJgMQBNZXNzYWdlUGFyc2VyYDEASUVudW1lcmF0b3JgMQBDU2hhcnBTbWFydENvbnRyYWN0YDEASUxpc3RgMQBJUmVhZE9ubHlMaXN0YDEAQWxwaGExAGIxAGMxAGQxAEl0ZW0xAHAxAHgxAHkxAEFFbGYuU3RhbmRhcmRzLkFDUzEyAFRvQnl0ZXMzMgBNYWtlRzIAUDIARnVuY2AyAFVuYXJ5U2VydmVyTWV0aG9kYDIAVmFsdWVUdXBsZWAyAEdhbW1hMgBCZXRhMgBEZWx0YTIAYjIAYzIAUGFpcmluZ1Byb2QyAEl0ZW0yAE1ha2VGcDIAeDIAeTIAUGFpcmluZ1Byb2QzAFJlYWRJbnQ2NABXcml0ZUludDY0AFBhaXJpbmdQcm9kNABWYWx1ZVR1cGxlYDYAPD45ADxNb2R1bGU+AGdldF9BAHNldF9BAGdldF9CAHNldF9CAElDAGdldF9DAHNldF9DAGdldF9YAHNldF9YAGdldF9ZAHNldF9ZAGFfAGJfAGNfAHNlY29uZF8AYmFzaWNGZWVfAGlzU2l6ZUZlZUZyZWVfAHByb29mXwBzeW1ib2xfAGZlZXNfAG93bmVyQWRkcmVzc18AY29udHJhY3RBZGRyZXNzXwBmaXJzdF8AaW5wdXRfAHhfAHlfAFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIAUGFpcmluZ0xpYgA8PmMARmllbGRDb2RlYwBfcmVwZWF0ZWRfZmVlc19jb2RlYwBfcmVwZWF0ZWRfaW5wdXRfY29kZWMAU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMAQm4yNTRHMUFkZABBRWxmLktlcm5lbC5TbWFydENvbnRyYWN0LlNoYXJlZABBc3NlcnRJbnB1dElzVmFsaWQAPEE+a19fQmFja2luZ0ZpZWxkADxCPmtfX0JhY2tpbmdGaWVsZAA8Qz5rX19CYWNraW5nRmllbGQAPFg+a19fQmFja2luZ0ZpZWxkADxZPmtfX0JhY2tpbmdGaWVsZABzbmFya1NjYWxhckZpZWxkAEJ1aWxkAGdldF9TZWNvbmQAc2V0X1NlY29uZABBZGRNZXRob2QATWFpbk5hbWVzcGFjZQBCaW5kU2VydmljZQBGcm9tR2VuZXJhdGVkQ29kZQBHZXRIYXNoQ29kZQBnZXRfQmFzaWNGZWUAc2V0X0Jhc2ljRmVlAFVzZXJDb250cmFjdE1ldGhvZEZlZQBnZXRfSXNTaXplRmVlRnJlZQBzZXRfSXNTaXplRmVlRnJlZQBJTWVzc2FnZQBSZWFkTWVzc2FnZQBXcml0ZU1lc3NhZ2UASUJ1ZmZlck1lc3NhZ2UARm9yTWVzc2FnZQBSZWFkUmF3TWVzc2FnZQBXcml0ZVJhd01lc3NhZ2UARW51bWVyYWJsZQBJRGlzcG9zYWJsZQBSdW50aW1lVHlwZUhhbmRsZQBHZXRUeXBlRnJvbUhhbmRsZQBfX1NlcnZpY2VOYW1lAFN5c3RlbS5SdW50aW1lAENsb25lAE1ldGhvZFR5cGUAVmFsdWVUeXBlAEFFbGYuQ1NoYXJwLkNvcmUATWFpbkNvbnRyYWN0QmFzZQBEaXNwb3NlAENyZWF0ZQBOZWdhdGUAQUVsZi5TZGsuQ1NoYXJwLlN0YXRlAE1haW5Db250cmFjdFN0YXRlAFRvbW9ycm93REFPLkNvbnRyYWN0cy5Wb3RlAFRocmVhZFN0YXRpY0F0dHJpYnV0ZQBDb21waWxlckdlbmVyYXRlZEF0dHJpYnV0ZQBHZW5lcmF0ZWRDb2RlQXR0cmlidXRlAERlYnVnZ2VyTm9uVXNlckNvZGVBdHRyaWJ1dGUARGVidWdnYWJsZUF0dHJpYnV0ZQBBc3NlbWJseVRpdGxlQXR0cmlidXRlAFRhcmdldEZyYW1ld29ya0F0dHJpYnV0ZQBFeHRlbnNpb25BdHRyaWJ1dGUAQXNzZW1ibHlGaWxlVmVyc2lvbkF0dHJpYnV0ZQBBc3NlbWJseUluZm9ybWF0aW9uYWxWZXJzaW9uQXR0cmlidXRlAEFzc2VtYmx5Q29uZmlndXJhdGlvbkF0dHJpYnV0ZQBSZWZTYWZldHlSdWxlc0F0dHJpYnV0ZQBUdXBsZUVsZW1lbnROYW1lc0F0dHJpYnV0ZQBDb21waWxhdGlvblJlbGF4YXRpb25zQXR0cmlidXRlAEFzc2VtYmx5UHJvZHVjdEF0dHJpYnV0ZQBBc3NlbWJseUNvbXBhbnlBdHRyaWJ1dGUAUnVudGltZUNvbXBhdGliaWxpdHlBdHRyaWJ1dGUAQnl0ZQBnZXRfVmFsdWUAc2V0X1ZhbHVlAF9fTWFyc2hhbGxlcl9nb29nbGVfcHJvdG9idWZfQm9vbFZhbHVlAFRvQmlnSW50VmFsdWUAdmFsdWUAQ29tcHV0ZUludDY0U2l6ZQBDb21wdXRlTWVzc2FnZVNpemUAQ2FsY3VsYXRlU2l6ZQBDb21wdXRlU3RyaW5nU2l6ZQBBRWxmAGdldF9Qcm9vZgBzZXRfUHJvb2YAVHJhbnNmb3JtUHJvb2YAX19NZXRob2RfVmVyaWZ5UHJvb2YAR29vZ2xlLlByb3RvYnVmAFJlYWRUYWcAV3JpdGVSYXdUYWcAU3lzdGVtLlJ1bnRpbWUuVmVyc2lvbmluZwBCbjI1NFBhaXJpbmcARnJvbUJhc2U2NFN0cmluZwBUb0RpYWdub3N0aWNTdHJpbmcAUmVhZFN0cmluZwBXcml0ZVN0cmluZwBBRWxmU3RyaW5nAFRvU3RyaW5nAEZvclN0cmluZwBhcmcAU2FmZU1hdGgAZ2V0X0xlbmd0aABvcF9HcmVhdGVyVGhhbk9yRXF1YWwAWmtWZXJpZmllci5kbGwAQ2hlY2tOb3ROdWxsAGdldF9TeW1ib2wAc2V0X1N5bWJvbABSZWFkQm9vbABXcml0ZUJvb2wAc2VydmljZUltcGwAQm4yNTRHMU11bABTY2FsYXJNdWwAQ29kZWRJbnB1dFN0cmVhbQBDb2RlZE91dHB1dFN0cmVhbQBnZXRfSXRlbQBTeXN0ZW0ATWVyZ2VGaWVsZEZyb20AcGI6Okdvb2dsZS5Qcm90b2J1Zi5JQnVmZmVyTWVzc2FnZS5JbnRlcm5hbE1lcmdlRnJvbQBQYXJzZUZyb20AQWRkRW50cmllc0Zyb20AQm9vbGVhbgBvcF9MZXNzVGhhbgBFeHRlbnNpb24Ab3BfU3VidHJhY3Rpb24AR29vZ2xlLlByb3RvYnVmLlJlZmxlY3Rpb24AU3lzdGVtLlJlZmxlY3Rpb24AQWNzMTJSZWZsZWN0aW9uAENvcmVSZWZsZWN0aW9uAE1haW5SZWZsZWN0aW9uAEF1dGhvcml0eUluZm9SZWZsZWN0aW9uAEdyb3RoMTZWZXJpZmllclJlZmxlY3Rpb24AT3B0aW9uc1JlZmxlY3Rpb24AV3JhcHBlcnNSZWZsZWN0aW9uAEVtcHR5UmVmbGVjdGlvbgBBZGRpdGlvbgBTZXJ2ZXJTZXJ2aWNlRGVmaW5pdGlvbgBOb3RJbXBsZW1lbnRlZEV4Y2VwdGlvbgBBc3NlcnRpb25FeGNlcHRpb24AcGI6Okdvb2dsZS5Qcm90b2J1Zi5JQnVmZmVyTWVzc2FnZS5JbnRlcm5hbFdyaXRlVG8AR2VuZXJhdGVkQ2xyVHlwZUluZm8AQXV0aG9yaXR5SW5mbwBnZXRfWmVybwBJc1plcm8AemVybwBaaXAAU2tpcABBRWxmLlNkay5DU2hhcnAAU3lzdGVtLkxpbnEAc2NhbGFyAEFGaWVsZE51bWJlcgBCRmllbGROdW1iZXIAQ0ZpZWxkTnVtYmVyAFhGaWVsZE51bWJlcgBZRmllbGROdW1iZXIAU2Vjb25kRmllbGROdW1iZXIAQmFzaWNGZWVGaWVsZE51bWJlcgBJc1NpemVGZWVGcmVlRmllbGROdW1iZXIAUHJvb2ZGaWVsZE51bWJlcgBTeW1ib2xGaWVsZE51bWJlcgBGZWVzRmllbGROdW1iZXIAT3duZXJBZGRyZXNzRmllbGROdW1iZXIAQ29udHJhY3RBZGRyZXNzRmllbGROdW1iZXIARmlyc3RGaWVsZE51bWJlcgBJbnB1dEZpZWxkTnVtYmVyAENyZWF0ZUJ1aWxkZXIAb3RoZXIAR3JvdGgxNlZlcmlmaWVyAFprVmVyaWZpZXIAU3lzdGVtLkNvZGVEb20uQ29tcGlsZXIATWFpbkNvbnRyYWN0Q29udGFpbmVyAGdldF9QYXJzZXIATWVzc2FnZVBhcnNlcgBfcGFyc2VyAEpzb25Gb3JtYXR0ZXIASUV4ZWN1dGlvbk9ic2VydmVyAFNldE9ic2VydmVyAF9vYnNlcnZlcgBJRW51bWVyYXRvcgBHZXRFbnVtZXJhdG9yAC5jdG9yAC5jY3RvcgBwYjo6R29vZ2xlLlByb3RvYnVmLklNZXNzYWdlLkRlc2NyaXB0b3IAcGI6Okdvb2dsZS5Qcm90b2J1Zi5JTWVzc2FnZS5nZXRfRGVzY3JpcHRvcgBTZXJ2aWNlRGVzY3JpcHRvcgBNZXNzYWdlRGVzY3JpcHRvcgBGaWxlRGVzY3JpcHRvcgBkZXNjcmlwdG9yAFN5c3RlbS5EaWFnbm9zdGljcwBfdW5rbm93bkZpZWxkcwBSZXNldEZpZWxkcwBnZXRfU2VydmljZXMAU3lzdGVtLlJ1bnRpbWUuQ29tcGlsZXJTZXJ2aWNlcwBEZWJ1Z2dpbmdNb2RlcwBnZXRfRmVlcwBVc2VyQ29udHJhY3RNZXRob2RGZWVzAEFFbGYuVHlwZXMAZ2V0X05lc3RlZFR5cGVzAGdldF9NZXNzYWdlVHlwZXMAR29vZ2xlLlByb3RvYnVmLldlbGxLbm93blR5cGVzAEZyb21CaWdFbmRpYW5CeXRlcwBUb0JpZ0VuZGlhbkJ5dGVzAEVxdWFscwBNZXNzYWdlRXh0ZW5zaW9ucwBHb29nbGUuUHJvdG9idWYuQ29sbGVjdGlvbnMAU3lzdGVtLkNvbGxlY3Rpb25zAFByb3RvUHJlY29uZGl0aW9ucwBnZXRfQ2hhcnMATWFyc2hhbGxlcnMAZ2V0X0Rlc2NyaXB0b3JzAEFkZERlc2NyaXB0b3JzAGdldF9Pd25lckFkZHJlc3MAc2V0X093bmVyQWRkcmVzcwBnZXRfQ29udHJhY3RBZGRyZXNzAHNldF9Db250cmFjdEFkZHJlc3MAQ29uY2F0AE1haW5Db250cmFjdABBRWxmLktlcm5lbC5TbWFydENvbnRyYWN0AENTaGFycFNtYXJ0Q29udHJhY3RBYnN0cmFjdABPYmplY3QAU2VsZWN0AFVua25vd25GaWVsZFNldABvcF9JbXBsaWNpdABnZXRfQ3VycmVudABHMVBvaW50AEcyUG9pbnQAZ2V0X0NvdW50AEJyYW5jaENvdW50AENhbGxDb3VudABDb252ZXJ0AGdldF9GaXJzdABzZXRfRmlyc3QAZ2V0X0lucHV0AF9fTWFyc2hhbGxlcl9ncm90aDE2X3ZlcmlmaWVyX1ZlcmlmeVByb29mSW5wdXQAaW5wdXQAb3V0cHV0AE1vdmVOZXh0AGdldF9Db250ZXh0AFBhcnNlQ29udGV4dABXcml0ZUNvbnRleHQAQ1NoYXJwU21hcnRDb250cmFjdENvbnRleHQAdgBNb2RQb3cAY3R4AFRvQnl0ZUFycmF5AFRvQXJyYXkAR2V0VmVyaWZ5aW5nS2V5AG9wX0VxdWFsaXR5AG9wX0luZXF1YWxpdHkASXNOdWxsT3JFbXB0eQBFeGVjdXRpb25PYnNlcnZlclByb3h5AAAAAAB5QwBpAFYAUQBjAG0AOQAwAGIAMgBKADEAWgBpADkAdABaAFgATgB6AFkAVwBkAGwATAAyAEYAMQBkAEcAaAB2AGMAbQBsADAAZQBWADkAcABiAG0AWgB2AEwAbgBCAHkAYgAzAFIAdgBHAGcAOQBoAFoAVwB4AG0AAHlMADIATgB2AGMAbQBVAHUAYwBIAEoAdgBkAEcAOABpAFgAZwBvAE4AUQBYAFYAMABhAEcAOQB5AGEAWABSADUAUwBXADUAbQBiAHgASQBuAEMAaABCAGoAYgAyADUAMABjAG0ARgBqAGQARgA5AGgAWgBHAFIAeQAAeVoAWABOAHoARwBBAEUAZwBBAFMAZwBMAE0AZwAwAHUAWQBXAFYAcwBaAGkANQBCAFoARwBSAHkAWgBYAE4AegBFAGkAUQBLAEQAVwA5ADMAYgBtAFYAeQBYADIARgBrAFoASABKAGwAYwAzAE0AWQBBAGkAQQBCAAB5SwBBAHMAeQBEAFMANQBoAFoAVwB4AG0ATABrAEYAawBaAEgASgBsAGMAMwBOAEMASABhAG8AQwBHAGwAUgB2AGIAVwA5AHkAYwBtADkAMwBSAEUARgBQAEwAawBOAHYAYgBuAFIAeQBZAFcATgAwAGMAeQA1AFcAACFiADMAUgBsAFkAZwBaAHcAYwBtADkAMABiAHoATQA9AAAfQwBvAG4AdAByAGEAYwB0AEEAZABkAHIAZQBzAHMAABlPAHcAbgBlAHIAQQBkAGQAcgBlAHMAcwAAeUMAaQBSAFEAYwBtADkAMABiADIASgAxAFoAaQA5AGkAWQBYAE4AbABMADIAZAB5AGIAMwBSAG8ATQBUAFoAZgBkAG0AVgB5AGEAVwBaAHAAWgBYAEkAdQBjAEgASgB2AGQARwA4AFMARQBHAGQAeQBiADMAUgBvAAB5TQBUAFoAZgBkAG0AVgB5AGEAVwBaAHAAWgBYAEkAYQBFAG0ARgBsAGIARwBZAHYAYgAzAEIAMABhAFcAOQB1AGMAeQA1AHcAYwBtADkAMABiAHgAbwBlAFoAMgA5AHYAWgAyAHgAbABMADMAQgB5AGIAMwBSAHYAAHlZAG4AVgBtAEwAMwBkAHkAWQBYAEIAdwBaAFgASgB6AEwAbgBCAHkAYgAzAFIAdgBJAHMARQBEAEMAaABCAFcAWgBYAEoAcABaAG4AbABRAGMAbQA5AHYAWgBrAGwAdQBjAEgAVgAwAEUAagBjAEsAQgBYAEIAeQAAeWIAMgA5AG0ARwBBAEUAZwBBAFMAZwBMAE0AaQBnAHUAWgAzAEoAdgBkAEcAZwB4AE4AbAA5ADIAWgBYAEoAcABaAG0AbABsAGMAaQA1AFcAWgBYAEoAcABaAG4AbABRAGMAbQA5AHYAWgBrAGwAdQBjAEgAVgAwAAB5TABsAEIAeQBiADIAOQBtAEUAZwAwAEsAQgBXAGwAdQBjAEgAVgAwAEcAQQBJAGcAQQB5AGcASgBHAGgAOABLAEIAMABjAHgAVQBHADkAcABiAG4AUQBTAEMAUQBvAEIAZQBCAGcAQgBJAEEARQBvAEMAUgBJAEoAAHlDAGcARgA1AEcAQQBJAGcAQQBTAGcASgBHAGkAUQBLAEEAMABaAHcATQBoAEkATgBDAGcAVgBtAGEAWABKAHoAZABCAGcAQgBJAEEARQBvAEMAUgBJAE8AQwBnAFoAegBaAFcATgB2AGIAbQBRAFkAQQBpAEEAQgAAeUsAQQBrAGEAYgB3AG8ASABSAHoASgBRAGIAMgBsAHUAZABCAEkAeABDAGcARgA0AEcAQQBFAGcAQQBTAGcATABNAGkAWQB1AFoAMwBKAHYAZABHAGcAeABOAGwAOQAyAFoAWABKAHAAWgBtAGwAbABjAGkANQBXAAB5WgBYAEoAcABaAG4AbABRAGMAbQA5AHYAWgBrAGwAdQBjAEgAVgAwAEwAawBaAHcATQBoAEkAeABDAGcARgA1AEcAQQBJAGcAQQBTAGcATABNAGkAWQB1AFoAMwBKAHYAZABHAGcAeABOAGwAOQAyAFoAWABKAHAAAHlaAG0AbABsAGMAaQA1AFcAWgBYAEoAcABaAG4AbABRAGMAbQA5AHYAWgBrAGwAdQBjAEgAVgAwAEwAawBaAHcATQBoAHEAcwBBAFEAbwBGAFUASABKAHYAYgAyAFkAUwBOAFEAbwBCAFkAUgBnAEIASQBBAEUAbwAAeUMAegBJAHEATABtAGQAeQBiADMAUgBvAE0AVABaAGYAZABtAFYAeQBhAFcAWgBwAFoAWABJAHUAVgBtAFYAeQBhAFcAWgA1AFUASABKAHYAYgAyAFoASgBiAG4AQgAxAGQAQwA1AEgATQBWAEIAdgBhAFcANQAwAAB5RQBqAFUASwBBAFcASQBZAEEAaQBBAEIASwBBAHMAeQBLAGkANQBuAGMAbQA5ADAAYQBEAEUAMgBYADMAWgBsAGMAbQBsAG0AYQBXAFYAeQBMAGwAWgBsAGMAbQBsAG0AZQBWAEIAeQBiADIAOQBtAFMAVwA1AHcAAHlkAFgAUQB1AFIAegBKAFEAYgAyAGwAdQBkAEIASQAxAEMAZwBGAGoARwBBAE0AZwBBAFMAZwBMAE0AaQBvAHUAWgAzAEoAdgBkAEcAZwB4AE4AbAA5ADIAWgBYAEoAcABaAG0AbABsAGMAaQA1AFcAWgBYAEoAcAAAeVoAbgBsAFEAYwBtADkAdgBaAGsAbAB1AGMASABWADAATABrAGMAeABVAEcAOQBwAGIAbgBRAHkAWQBnAG8AUABSADMASgB2AGQARwBnAHgATgBsAFoAbABjAG0AbABtAGEAVwBWAHkARQBrADgASwBDADEAWgBsAAB5YwBtAGwAbQBlAFYAQgB5AGIAMgA5AG0ARQBpAEkAdQBaADMASgB2AGQARwBnAHgATgBsADkAMgBaAFgASgBwAFoAbQBsAGwAYwBpADUAVwBaAFgASgBwAFoAbgBsAFEAYwBtADkAdgBaAGsAbAB1AGMASABWADAAAGlHAGgAbwB1AFoAMgA5AHYAWgAyAHgAbABMAG4AQgB5AGIAMwBSAHYAWQBuAFYAbQBMAGsASgB2AGIAMgB4AFcAWQBXAHgAMQBaAFMASQBBAFkAZwBaAHcAYwBtADkAMABiAHoATQA9AAALUAByAG8AbwBmAAALSQBuAHAAdQB0AAADWAAAA1kAAAtGAGkAcgBzAHQAAA1TAGUAYwBvAG4AZAAAA0EAAANCAAADQwAAAQALdgBhAGwAdQBlAAB5QwBoAGwAUQBjAG0AOQAwAGIAMgBKADEAWgBpADkAaQBZAFgATgBsAEwAMgBGAGoAYwB6AEUAeQBMAG4AQgB5AGIAMwBSAHYARQBnAFYAaABZADMATQB4AE0AaABvAFMAWQBXAFYAcwBaAGkAOQB2AGMASABSAHAAAHliADIANQB6AEwAbgBCAHkAYgAzAFIAdgBHAGgAdABuAGIAMgA5AG4AYgBHAFUAdgBjAEgASgB2AGQARwA5AGkAZABXAFkAdgBaAFcAMQB3AGQASABrAHUAYwBIAEoAdgBkAEcAOABhAEgAbQBkAHYAYgAyAGQAcwAAeVoAUwA5AHcAYwBtADkAMABiADIASgAxAFoAaQA5ADMAYwBtAEYAdwBjAEcAVgB5AGMAeQA1AHcAYwBtADkAMABiAHgAbwBQAFkAVwBWAHMAWgBpADkAagBiADMASgBsAEwAbgBCAHkAYgAzAFIAdgBJAGwANABLAAB5RgBsAFYAegBaAFgASgBEAGIAMgA1ADAAYwBtAEYAagBkAEUAMQBsAGQARwBoAHYAWgBFAFoAbABaAFgATQBTAEsAZwBvAEUAWgBtAFYAbABjAHgAZwBDAEkAQQBNAG8AQwB6AEkAYwBMAG0ARgBqAGMAegBFAHkAAHlMAGwAVgB6AFoAWABKAEQAYgAyADUAMABjAG0ARgBqAGQARQAxAGwAZABHAGgAdgBaAEUAWgBsAFoAUgBJAFkAQwBoAEIAcABjADEAOQB6AGEAWABwAGwAWAAyAFoAbABaAFYAOQBtAGMAbQBWAGwARwBBAE0AZwAAeUEAUwBnAEkASQBqAG8ASwBGAFYAVgB6AFoAWABKAEQAYgAyADUAMABjAG0ARgBqAGQARQAxAGwAZABHAGgAdgBaAEUAWgBsAFoAUgBJAE8AQwBnAFoAegBlAFcAMQBpAGIAMgB3AFkAQQBTAEEAQgBLAEEAawBTAAB5RQBRAG8ASgBZAG0ARgB6AGEAVwBOAGYAWgBtAFYAbABHAEEASQBnAEEAUwBnAEQATQBnADQASwBEAEYAVgB6AFoAWABKAEQAYgAyADUAMABjAG0ARgBqAGQARQBJAGgAcQBnAEkAVQBRAFUAVgBzAFoAaQA1AFQAAGlkAEcARgB1AFoARwBGAHkAWgBIAE0AdQBRAFUATgBUAE0AVABLAEsAawB2AFEAQgBCAFcARgBqAGMAegBFAHkAVQBBAEIAUQBBAFYAQQBDAFkAZwBaAHcAYwBtADkAMABiAHoATQA9AAAJRgBlAGUAcwAAG0kAcwBTAGkAegBlAEYAZQBlAEYAcgBlAGUAAA1TAHkAbQBiAG8AbAAAEUIAYQBzAGkAYwBGAGUAZQAAgJsyADAANgA5ADIAOAA5ADgAMQA4ADkAMAA5ADIANwAzADkAMgA3ADgAMQA5ADMAOAA2ADkAMgA3ADQANAA5ADUANQA1ADYANgAxADcANwA4ADgANQAzADAAOAAwADgANAA4ADYAMgA3ADAAMQAxADgAMwA3ADEANwAwADEANQAxADYANgA2ADYAMgA1ADIAOAA3ADcAOQA2ADkAAICbMQAxADcAMQAzADAANgAyADgANwA4ADIAOQAyADYANQAzADkANgA3ADkANwAxADMANwA4ADEAOQA0ADMANQAxADkANgA4ADAAMwA5ADUAOQA2ADMAOQA2ADgANQAzADkAMAA0ADUANwAyADgANwA5ADQAOAA4ADEANgA2ADAAOAA0ADIAMwAxADcANAAwADUANQA3ADIANwA5AACAmzEAMgAxADYAOAA1ADIAOAA4ADEAMAAxADgAMQAyADYAMwA3ADAANgA4ADkANQAyADUAMgAzADEANQA2ADQAMAA1ADMANAA4ADEAOAAyADIAMgA5ADQAMwAzADQAOAAxADkAMwAzADAAMgAxADMAOQAzADUAOAAzADcANwAxADYAMgA2ADQANQAwADIAOQA5ADMANwAwADAANgAAgJcyADgAMQAxADIAMAA1ADcAOAAzADMANwAxADkANQA3ADIAMAAzADUANwA0ADcANAA5ADYANQA5ADcAOQA5ADQANwA2ADkAMAA0ADMAMQA2ADIAMgAxADIANwA5ADgANgA4ADEANgA4ADMAOQAyADAAOAA1ADcANgAzADUAOAAwADIANAA2ADAAOAA4ADAAMwA1ADQAMgAAgJsxADYAMQAyADkAMQA3ADYANQAxADUANwAxADMAMAA3ADIAMAA0ADIANAA0ADIANwAzADQAOAAzADkAMAAxADIAOQA2ADYANQA2ADMAOAAxADcAOAA5ADAANgA4ADgANwA4ADUAOAAwADUAMAA5ADAAMAAxADEAMAAxADEANQA3ADAAOQA4ADkAMwAxADUANQA1ADkAOQAxADMAAICZOQAwADEAMQA3ADAAMwA0ADUAMwA3ADcAMgAwADMAMAAzADcANQAxADIANAA0ADYANgA2ADQAMgAyADAAMwA2ADQAMQA2ADMANgA4ADIANQAyADIAMwA5ADAANgAxADQANQA5ADAAOAA3ADcAMAAzADAAOAA3ADIANAA1ADQAOQA2ADQANgA5ADAAOQA0ADgAMAA1ADEAMAAAgJsxADEANQA1ADkANwAzADIAMAAzADIAOQA4ADYAMwA4ADcAMQAwADcAOQA5ADEAMAAwADQAMAAyADEAMwA5ADIAMgA4ADUANwA4ADMAOQAyADUAOAAxADIAOAA2ADEAOAAyADEAMQA5ADIANQAzADAAOQAxADcANAAwADMAMQA1ADEANAA1ADIAMwA5ADEAOAAwADUANgAzADQAAICbMQAwADgANQA3ADAANAA2ADkAOQA5ADAAMgAzADAANQA3ADEAMwA1ADkANAA0ADUANwAwADcANgAyADIAMwAyADgAMgA5ADQAOAAxADMANwAwADcANQA2ADMANQA5ADUANwA4ADUAMQA4ADAAOAA2ADkAOQAwADUAMQA5ADkAOQAzADIAOAA1ADYANQA1ADgANQAyADcAOAAxAACAmTQAMAA4ADIAMwA2ADcAOAA3ADUAOAA2ADMANAAzADMANgA4ADEAMwAzADIAMgAwADMANAAwADMAMQA0ADUANAAzADUANQA2ADgAMwAxADYAOAA1ADEAMwAyADcANQA5ADMANAAwADEAMgAwADgAMQAwADUANwA0ADEAMAA3ADYAMgAxADQAMQAyADAAMAA5ADMANQAzADEAAICZOAA0ADkANQA2ADUAMwA5ADIAMwAxADIAMwA0ADMAMQA0ADEANwA2ADAANAA5ADcAMwAyADQANwA0ADgAOQAyADcAMgA0ADMAOAA0ADEAOAAxADkAMAA1ADgANwAyADYAMwA2ADAAMAAxADQAOAA3ADcAMAAyADgAMAA2ADQAOQAzADAANgA5ADUAOAAxADAAMQA5ADMAMAAAgJsyADEAMgA4ADAANQA5ADQAOQA0ADkANQAxADgAOQA5ADIAMQA1ADMAMwAwADUANQA4ADYANwA4ADMAMgA0ADIAOAAyADAANgA4ADIANgA0ADQAOQA5ADYAOQAzADIAMQA4ADMAMQA4ADYAMwAyADAANgA4ADAAOAAwADAAMAA3ADIAMQAzADMANAA4ADYAOAA4ADcANAAzADIAAICXMQA1ADAAOAA3ADkAMQAzADYANAAzADMAOQA3ADQANQA1ADIAOAAwADAAMAAzADAAOQA2ADMAOAA5ADkANwA3ADEAMQA2ADIANgA0ADcANwAxADUAMAA2ADkANgA4ADUAOAA5ADAANQA0ADcANAA4ADkAMQAzADIAMQA3ADgAMwAxADQANwAzADYANAA3ADAANgA2ADIAAICZMQAwADgAMQA4ADMANgAwADAANgA5ADUANgA2ADAAOQA4ADkANAA1ADQAOQA3ADcAMQAzADMANAA3ADIAMQA0ADEAMwAxADgANwA5ADEAMwAwADQANwAzADgAMwAzADMAMQA1ADYAMQA2ADAAMQA2ADAANgAyADYAMAAyADgAMwAxADYANwA2ADEANQA5ADUAMwAyADkANQAAgJsxADEANAAzADQAMAA4ADYANgA4ADYAMwA1ADgAMQA1ADIAMwAzADUANQA0ADAANQA1ADQANgA0ADMAMQAzADAAMAAwADcAMwAwADcANgAxADcAMAA3ADgAMwAyADQAOQA3ADUAOQA4ADEAMgA1ADcAOAAyADMANAA3ADYANAA3ADIAMQAwADQANgAxADYAMQA5ADYAMAA5ADAAAICbMQA2ADIAMgA1ADEANAA4ADMANgA0ADMAMQA2ADMAMwA3ADMANwA2ADcANgA4ADEAMQA5ADIAOQA3ADQANQA2ADgANgA4ADkAMAA4ADQAMgA3ADkAMgA1ADgAMgA5ADgAMQA3ADcANAA4ADYAOAA0ADEAMwA5ADEANwA1ADMAMAA5ADYAMgAwADIAMQA3ADAAOQA4ADgAMQA0AACAmTUAMQA2ADcAMgA2ADgANgA4ADkANAA1ADAAMgAwADQAMQA2ADIAMAA0ADYAMAA4ADQANAA0ADIANQA4ADEAMAA1ADEANQA2ADUAOQA5ADcANwAzADMAMgAzADMAMAA2ADIANAA3ADgAMwAxADcAOAAxADMANwA1ADUANgAzADYAMQA2ADIANAAxADMAMQA2ADQANgA5ADAAAICbMQAyADgAOAAyADMANwA3ADgANAAyADAANwAyADYAOAAyADIANgA0ADkANwA5ADMAMQA3ADQANAA1ADMANgA1ADMAMAAzADMANwA1ADEANQA5ADgAMgA4ADIANwAyADQAMgAzADQAOQA1ADAAOAA4ADkAMQAxADkAOAA1ADYAOAA5ADQANgAzADAAMgAyADAAOQA0ADIANgAwAACAmzEAOQA0ADgAOAAyADEANQA4ADUANgA2ADYANQAxADcAMwA1ADYANQA1ADIANgA3ADUAOAAzADYAMAA1ADEAMAAxADIANQA5ADMAMgAyADEANAAyADUAMgA3ADYANwAyADcANQA4ADEANgAzADIAOQAyADMAMgA0ADUANAA4ADcANQA4ADAANAA0ADcANAA4ADQANAA3ADgANgAAgJsxADMAMAA4ADMANAA5ADIANgA2ADEANgA4ADMANAAzADEAMAA0ADQAMAA0ADUAOQA5ADIAMgA4ADUANAA3ADYAMQA4ADQAMQA4ADIAMQA0ADQAMAA5ADkAOAAyADkANQAwADcAMwA1ADAAMwA1ADIAMQAyADgANgAxADUAMQA4ADIANQAxADYANQAzADAAMAAxADQANwA3ADcAAICXNgAwADIAMAA1ADEAMgA4ADEANwA5ADYAMQA1ADMANgA5ADIAMwA5ADIANQAyADMANwAwADIANgA3ADYANwA4ADIAMAAyADMANAA3ADIANwA0ADQANQAyADIAMAAzADIANgA3ADAAOAAwADEAMAA5ADEANgAxADcAMgA0ADYANAA5ADgANQA1ADEAMgAzADgAOQAxADMAAICZOQA3ADMAMgA0ADYANQA5ADcAMgAxADgAMAAzADMANQA2ADIAOQA5ADYAOQA0ADIAMQA1ADEAMwA3ADgANQA2ADAAMgA5ADMANAA3ADAANgAwADkANgA5ADAAMgAzADEANgA0ADgAMwA1ADgAMAA4ADgAMgA4ADQAMgA3ADgAOQA2ADYAMgA2ADYAOQAyADEAMgA4ADkAMAAAgJkyADcANwA2ADUAMgA2ADYAOQA4ADYAMAA2ADgAOAA4ADQAMwA0ADAANwA0ADIAMAAwADMAOAA0ADIANgA0ADgAMgA0ADQANgAxADYAOAA4ADEAOQA4ADMAOAA0ADkAOAA5ADUAMgAxADAAOQAxADIANQAzADIAOAA5ADcANwA2ADIAMwA1ADYAMAAyADQAOQA1ADYANwA4AACAmTgANQA4ADYAMwA2ADQAMgA3ADQANQAzADQANQA3ADcAMQA1ADQAOAA5ADQANgAxADEAMAA4ADAAMgAzADQAMAA0ADgANgA0ADgAOAA4ADMANwA4ADEAOQA1ADUAMwA0ADUANgAyADIANQA3ADgANQAzADEAMgAzADMAMQAxADMAMQA4ADAANQAzADIAMgAzADQAOAA0ADIAAICbMgAxADIANwA2ADEAMwA0ADkAMgA5ADgAOAAzADEAMgAxADEAMgAzADMAMgAzADMANQA5ADQANQAwADYANQA4ADMAMgAwADgAMgAwADAANwA1ADYAOQA4ADQAOQAwADYANgA2ADgANwAwADQAOAA3ADQANQAwADkAOAA1ADYAMAAzADkAOAA4ADIAMQA0ADMANAA5ADQAMAA3AACAmTQAOQAxADAANgAyADgANQAzADMAMQA3ADEANQA5ADcANgA3ADUAMAAxADgANwAyADQANwAwADkANgAzADEANwA4ADgAOQA0ADgAMwA1ADUANAAyADIAOAAyADkANAA5ADkAOAA1ADUAMAAzADMAOQA2ADUAMAAxADgANgA2ADUAMwAwADAAMwA4ADYANgAzADcAOAA4ADQAAICbMgAwADUAMwAyADQANgA4ADgAOQAwADAAMgA0ADAAOAA0ADUAMQAwADQAMwAxADcAOQA5ADAAOQA4ADAAOQA3ADAAOAAxADYAMAAwADQAOAAwADMANwA2ADEAMgA3ADgANwAwADIAOQA5ADEANAAyADEAOAA5ADYAOQA2ADYAMgAwADcANQAyADUAMAAwADYANgA0ADMAMAAyAACAmzEANQAzADMANQA4ADUAOAAxADAAMgAyADgAOQA5ADQANwA2ADQAMgA1ADAANQA0ADUAMAA2ADkAMgAwADEAMgAxADEANgAyADIAMgA4ADIANwAyADMAMwA5ADEAOAAxADgANQAxADUAMAAxADcANgA4ADgAOAA2ADQAMQA5ADAAMwA1ADMAMQA1ADQAMgAwADMANAAwADEANwAAgJk1ADMAMQAxADUAOQA3ADAANgA3ADYANgA3ADYANwAxADUAOAAxADYANAA2ADcAMAA5ADkAOQA4ADEANwAxADcAMAAzADgAMgA4ADkANgA1ADgANwA1ADYANwA3ADYAMwA3ADIAOQAyADMAMQA1ADAANQA1ADAAMwAwADMANQAzADcANwA5ADUAMwAxADQAMAA0ADgAMQAyAAADMAAAgJsyADEAOAA4ADgAMgA0ADIAOAA3ADEAOAAzADkAMgA3ADUAMgAyADIAMgA0ADYANAAwADUANwA0ADUAMgA1ADcAMgA3ADUAMAA4ADgANQA0ADgAMwA2ADQANAAwADAANAAxADYAMAAzADQAMwA0ADMANgA5ADgAMgAwADQAMQA4ADYANQA3ADUAOAAwADgANAA5ADUANgAxADcAAB9pAG4AdgBhAGwAaQBkACAAdgBhAGwAdQBlADoAIAAAAzEAAAMyAACAmzIAMQA4ADgAOAAyADQAMgA4ADcAMQA4ADMAOQAyADcANQAyADIAMgAyADQANgA0ADAANQA3ADQANQAyADUANwAyADcANQAwADgAOAA2ADkANgAzADEAMQAxADUANwAyADkANwA4ADIAMwA2ADYAMgA2ADgAOQAwADMANwA4ADkANAA2ADQANQAyADIANgAyADAAOAA1ADgAMwAALXAAYQBpAHIAaQBuAGcALQBsAGUAbgBnAHQAaABzAC0AZgBhAGkAbABlAGQAAClwAGEAaQByAGkAbgBnAC0AYwBoAGUAYwBrAC0AZgBhAGkAbABlAGQAAHlDAGgAeABRAGMAbQA5ADAAYgAyAEoAMQBaAGkAOQBqAGIAMgA1ADAAYwBtAEYAagBkAEMAOQB0AFkAVwBsAHUATABuAEIAeQBiADMAUgB2AEcAaABsAFEAYwBtADkAMABiADIASgAxAFoAaQA5AGkAWQBYAE4AbAAAeUwAMgBGAGoAYwB6AEUAeQBMAG4AQgB5AGIAMwBSAHYARwBoAFoAbgBjAG0AOQAwAGEARABFADIAWAAzAFoAbABjAG0AbABtAGEAVwBWAHkATABuAEIAeQBiADMAUgB2AE0AbAA4AEsARABFADEAaABhAFcANQBEAAB5YgAyADUAMABjAG0ARgBqAGQAQgBwAFAAcwBzAHoAMgBBAFIARgBOAFkAVwBsAHUAUQAyADkAdQBkAEgASgBoAFkAMwBSAFQAZABHAEYAMABaAGMAcgBLADkAZwBFAFoAVQBIAEoAdgBkAEcAOQBpAGQAVwBZAHYAAHlZAG0ARgB6AFoAUwA5AGgAWQAzAE0AeABNAGkANQB3AGMAbQA5ADAAYgA4AHIASwA5AGcARQBXAFoAMwBKAHYAZABHAGcAeABOAGwAOQAyAFoAWABKAHAAWgBtAGwAbABjAGkANQB3AGMAbQA5ADAAYgAwAEkAUQAAQXEAZwBJAE4AVABXAEYAcABiAGsANQBoAGIAVwBWAHoAYwBHAEYAagBaAFcASQBHAGMASABKAHYA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MgA3AGUAMQAAAAAAOAAIAAEAQQBzAHMAZQBtAGIAbAB5ACAAVgBlAHIAcwBpAG8AbgAAADEALgAwAC4AMAAuADAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACwAAAMAAAAID4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            var code = Convert.FromBase64String(b64Code);
            var contractOperation = GetContractOperation(code, "groth16_verifier");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result =await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                });
            return result.Output;
        }
        
        protected async Task<Address> DeployMerkleTreeContractAsync()
        {
            var code = System.IO.File.ReadAllBytes(typeof(MerkleTreeWithHistory.CreateTreeInput).Assembly.Location);
            var contractOperation = GetContractOperation(code, "merkletree");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                });
            return result.Output;
        }

    protected async Task InitializeAnonymousVoteAsync()
    {
        AnonymousVoteContractStub =
            GetContractStub<AnonymousVoteContractContainer.AnonymousVoteContractStub>(VoteContractAddress,
                DefaultAccount.KeyPair);
        AnonymousVoteAdminContractStub =
            GetContractStub<AnonymousVoteAdmin.AnonymousVoteAdminContractContainer.AnonymousVoteAdminContractStub>(
                VoteContractAddress, DefaultAccount.KeyPair);
        var merkleTreeContractAddress = await DeployMerkleTreeContractAsync();
        var groth16VerifierContractAddress = await DeployGroth16VerifierContractAsync();
        
        await AnonymousVoteAdminContractStub.SetMerkleTreeHistoryContractAddress.SendAsync(merkleTreeContractAddress);
        await AnonymousVoteAdminContractStub.SetGroth16VerifierAddress.SendAsync(groth16VerifierContractAddress);
    }

    #endregion
}