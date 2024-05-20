using System;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public VoteContractTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task RegisterTest_NoPermission()
        {
            await InitializeAll();
            var result = await VoteContractStub.Register.SendWithExceptionAsync(new VotingRegisterInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task RegisterTest()
        {
            await InitializeAll();
            // Governance + R + 1a1v
            GovernanceR1A1VProposalId = await CreateProposal(ProposalType.Governance, RSchemeAddress, UniqueVoteVoteSchemeId);
            // Governance + HC + 1a1v
            GovernanceHc1A1VProposalId = await CreateProposal(ProposalType.Governance, HCSchemeAddress, UniqueVoteVoteSchemeId);
            // Governance + R + 1t1v
            GovernanceR1T1VProposalId = await CreateProposal(ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId);
            // Governance + HC + 1t1v
            GovernanceHc1T1VProposalId = await CreateProposal(ProposalType.Governance, HCSchemeAddress, TokenBallotVoteSchemeId);
            
            // Advisory + R + 1a1v
            AdvisoryR1A1VProposalId = await CreateProposal(ProposalType.Advisory, RSchemeAddress, UniqueVoteVoteSchemeId);
            // Advisory + HC + 1a1v
            AdvisoryHc1A1VProposalId = await CreateProposal(ProposalType.Advisory, HCSchemeAddress, UniqueVoteVoteSchemeId);
            // Advisory + R + 1t1v
            AdvisoryR1T1VProposalId = await CreateProposal(ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId);
            // Advisory + HC + 1t1v
            AdvisoryHc1T1VProposalId = await CreateProposal(ProposalType.Advisory, HCSchemeAddress, TokenBallotVoteSchemeId);
            
            // // Veto + R + 1a1v
            // VetoR1A1VProposalId = await CreateVetoProposal(UniqueVoteVoteSchemeId);
            // // Veto + R + 1t1v
            // VetoR1T1VProposalId = await CreateVetoProposal(TokenBallotVoteSchemeId);
        }
        
        [Fact]
        public async Task VoteTest_NotInitialized()
        {
            var result = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput());
            result.TransactionResult.Error.ShouldContain("Not initialized yet.");
        }
        
        [Fact]
        public async Task VoteTest()
        {
            // var result = await VoteContractStub.Vote.SendAsync(new VoteInput
            // {
            // });
        }

        [Fact]
        public async Task GetVirtualAddressTest()
        {
            var daoId = HashHelper.ComputeFrom("daoId");
            await InitializeTest();
            var result = await VoteContractStub.GetVirtualAddress.CallAsync(new GetVirtualAddressInput
            {
                DaoId = daoId,
                Voter = DefaultAddress
            });
            _testOutputHelper.WriteLine("virtual address={0}", result);
        }
    }
    
}