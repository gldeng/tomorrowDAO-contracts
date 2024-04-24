using System;
using System.IO;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestBase : TestBase
{
    protected readonly Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");

    protected readonly GovernanceSchemeThreshold DefaultSchemeThreshold = new GovernanceSchemeThreshold
    {
        MinimalRequiredThreshold = 1,
        MinimalVoteThreshold = 1,
        MinimalApproveThreshold = 1,
        MaximalRejectionThreshold = 2,
        MaximalAbstentionThreshold = 2
    };

    protected readonly string DefaultGovernanceToken = "ELF";
    
    protected readonly Hash DefaultVoteSchemeId = HashHelper.ComputeFrom("DefaultVoteSchemeId");

    protected async Task Initialize(Address address = null)
    {
        var input = new InitializeInput
        {
            DaoContractAddress = address??DAOContractAddress
        };
        await GovernanceContractStub.Initialize.SendAsync(input);
    }

    protected async Task<Address> AddGovernanceScheme(Hash daoId = default,
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
    
    protected async Task<Hash> CreateProposal(Address schemeAddress)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = DefaultDaoId,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = schemeAddress,
            VoteSchemeId = DefaultVoteSchemeId
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

        var result = await GovernanceContractStub.CreateProposal.SendAsync(input);
        return result.Output;
    }
}