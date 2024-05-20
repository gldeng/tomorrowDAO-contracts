using System;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
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
            await InitializeAll(false);
            var result = await VoteContractStub.Register.SendWithExceptionAsync(new VotingRegisterInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task RegisterTest()
        {
            await InitializeAll(false);
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
            await RegisterTest();
            var result1 = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = 1, VoteOption = 0, VotingItemId = GovernanceR1A1VProposalId });
            result1.TransactionResult.Error.ShouldBe("");
            
            var result2 = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = 1, VoteOption = 0, VotingItemId = GovernanceHc1A1VProposalId });
            result2.TransactionResult.Error.ShouldContain("Invalid voter: not HC");
            
            await TokenContractStub.Approve.SendAsync(new ApproveInput { Amount = 5_00000000, Symbol = TokenElf, Spender = VoteContractAddress });
            var result3 = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = 1_00000000, VoteOption = 0, VotingItemId = GovernanceR1T1VProposalId});
            result3.TransactionResult.Error.ShouldBe("");
            
            var result4 = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = 1_00000000, VoteOption = 0, VotingItemId = GovernanceHc1T1VProposalId});
            result4.TransactionResult.Error.ShouldContain("Invalid voter: not HC");
            
            var result5 = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = 1, VoteOption = 0, VotingItemId = AdvisoryR1A1VProposalId });
            result5.TransactionResult.Error.ShouldBe("");
            
            var result6 = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = 1, VoteOption = 0, VotingItemId = AdvisoryHc1A1VProposalId });
            result6.TransactionResult.Error.ShouldContain("Invalid voter: not HC");
            
            var result7 = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = 1_00000000, VoteOption = 0, VotingItemId = AdvisoryR1T1VProposalId});
            result7.TransactionResult.Error.ShouldBe("");
            
            var result8 = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = 1_00000000, VoteOption = 0, VotingItemId = AdvisoryHc1T1VProposalId});
            result8.TransactionResult.Error.ShouldContain("Invalid voter: not HC");
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