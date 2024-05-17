using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestBase : TestBase
{
    protected IBlockTimeProvider BlockTimeProvider;
    
    protected IBlockTimeProvider BlockTimeProvider1 = new BlockTimeProvider();

    public GovernanceContractTestBase() : base()
    {
        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
    }

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

    public async Task InitializeAllContract(Address daoAddress = null, Address voteAddress = null)
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

        //init election contract
        await ElectionContractStub.Initialize.SendAsync(new Election.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        });
    }

    /// <summary>
    /// Dependent on the InitializeAllContract method.
    /// </summary>
    /// <returns>DaoId</returns>
    internal async Task<Hash> MockDao(bool isNetworkDao = false)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string>()
                    {
                        {
                            "aa", "bb"
                        }
                    }
                }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold()
            {
                MinimalRequiredThreshold = 5,
                MinimalVoteThreshold = 3,
                MinimalApproveThreshold = 5,
                MaximalRejectionThreshold = 5,
                MaximalAbstentionThreshold = 5
            },
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = new HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 10,
                    MaxHighCouncilCandidateCount = 20,
                    ElectionPeriod = 7,
                    StakingAmount = 100000000
                },
                GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 8000,
                    MinimalVoteThreshold = 10000000000000,
                    MinimalApproveThreshold = 8000,
                    MaximalRejectionThreshold = 2000,
                    MaximalAbstentionThreshold = 2000
                }
            },
            IsTreasuryContractNeeded = false,
            IsNetworkDao = isNetworkDao
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        return log.DaoId;
    }

    protected async Task<Hash> MockVoteScheme()
    {
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.UniqueVote
        });
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot
        });

        return await GetVoteSchemeId(VoteMechanism.UniqueVote);
    }

    /// <summary>
    /// Dependent on the MockVoteScheme method.
    /// </summary>
    /// <param name="voteMechanism"></param>
    /// <returns></returns>
    private async Task<Hash> GetVoteSchemeId(VoteMechanism voteMechanism)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(VoteContractAddress),
            HashHelper.ComputeFrom(VoteMechanism.UniqueVote.ToString()));
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

    /// <summary>
    /// Dependent on the InitializeAllContract/MockDao/MockVoteScheme method.
    /// </summary>
    /// <param name="schemeAddress"></param>
    /// <param name="voteSchemeId"></param>
    /// <returns></returns>
    internal async Task<Hash> CreateProposal(Address schemeAddress, Hash voteSchemeId = null, Hash daoId = null)
    {
        var input = MockCreateProposalInput(schemeAddress, voteSchemeId, daoId);
        var result = await GovernanceContractStub.CreateProposal.SendAsync(input);
        return result.Output;
    }

    internal async Task<Hash> CreateProposal(CreateProposalInput input)
    {
        var result = await GovernanceContractStub.CreateProposal.SendAsync(input);
        return result.Output;
    }

    internal async Task<IExecutionResult<Hash>> CreateProposalAsync(CreateProposalInput input, bool withException)
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var schemeAddress = addressList.Value.FirstOrDefault();
        var voteMechanismId = await MockVoteScheme();

        input.ProposalBasicInfo.DaoId = daoId;
        input.ProposalBasicInfo.SchemeAddress = schemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = voteMechanismId;

        return withException
            ? await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateProposal.SendAsync(input);
    }

    internal async Task<IExecutionResult<Hash>> CreateVetoProposalAsync(CreateVetoProposalInput input,
        bool withException)
    {
        var proposalInfoOutput = await GovernanceContractStub.GetProposalInfo.CallAsync(input.VetoProposalId);
        input.ProposalBasicInfo.DaoId = proposalInfoOutput.DaoId;

        var addressList =
            await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(proposalInfoOutput.DaoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var referendumSchemeAddress = addressList.Value.LastOrDefault();

        input.ProposalBasicInfo.DaoId = proposalInfoOutput.DaoId;
        input.ProposalBasicInfo.SchemeAddress = referendumSchemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = proposalInfoOutput.VoteSchemeId;

        return withException
            ? await GovernanceContractStub.CreateVetoProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateVetoProposal.SendAsync(input);
    }

    internal CreateVetoProposalInput MockCreateVetoProposalInput()
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "VetoProposalTitle",
            ProposalDescription = "VetoProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null
        };
        var proposalInput = new CreateVetoProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            VetoProposalId = null
        };
        return proposalInput;
    }


    internal CreateProposalInput MockCreateProposalInput()
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null
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

    internal CreateProposalInput MockCreateProposalInput(Address schemeAddress, Hash voteSchemeId = null,
        Hash daoId = null)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = daoId ?? DefaultDaoId,
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