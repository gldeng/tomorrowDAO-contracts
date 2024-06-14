using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
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
            GovernanceR1A1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, UniqueVoteVoteSchemeId);
            // Governance + HC + 1a1v
            GovernanceHc1A1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, UniqueVoteVoteSchemeId);
            // Governance + R + 1t1v
            GovernanceR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId);
            // Governance + HC + 1t1v
            GovernanceHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, TokenBallotVoteSchemeId);
            
            // Advisory + R + 1a1v
            AdvisoryR1A1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, UniqueVoteVoteSchemeId);
            // Advisory + HC + 1a1v
            AdvisoryHc1A1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, HcSchemeAddress, UniqueVoteVoteSchemeId);
            // Advisory + R + 1t1v
            AdvisoryR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId);
            // Advisory + HC + 1t1v
            AdvisoryHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, HcSchemeAddress, TokenBallotVoteSchemeId);
            
            // NetworkDao
            NetworkDaoGovernanceR1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoRSchemeAddress, UniqueVoteVoteSchemeId);
            NetworkDaoGovernanceHc1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoHcSchemeAddress, UniqueVoteVoteSchemeId);
            NetworkDaoGovernanceR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoGovernanceHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoAdvisoryR1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoRSchemeAddress, UniqueVoteVoteSchemeId);
            NetworkDaoAdvisoryHc1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoHcSchemeAddress, UniqueVoteVoteSchemeId);
            NetworkDaoAdvisoryR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoAdvisoryHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId);
        }

        [Fact]
        public async Task RegisterTest_Veto()
        {
            await RegisterTest();
            await HighCouncilElection(DaoId);
            await HighCouncilElectionFor(DaoId, UserAddress);
            await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceHc1A1VProposalId);
            await ApproveElf(OneElf * 10, VoteContractAddress);
            await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
            BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
            
            // Veto + R + 1a1v
            VetoR1A1VProposalId = await CreateVetoProposal(HcSchemeAddress, UniqueVoteVoteSchemeId, GovernanceHc1A1VProposalId);
            // Veto + R + 1t1v
            VetoR1T1VProposalId = await CreateVetoProposal(HcSchemeAddress, TokenBallotVoteSchemeId, GovernanceHc1T1VProposalId);
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
            await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceR1A1VProposalId);
            await VoteException(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceHc1A1VProposalId, "Invalid voter: not HC");
            await ApproveElf(OneElf * 10, VoteContractAddress);
            
            await Vote(OneElf, VoteOption.Approved, GovernanceR1T1VProposalId);
            await VoteException(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId, "Invalid voter: not HC");
            await Vote(UniqueVoteVoteAmount, VoteOption.Approved, AdvisoryR1A1VProposalId);
            await VoteException(UniqueVoteVoteAmount, VoteOption.Approved, AdvisoryHc1A1VProposalId, "Invalid voter: not HC");
            await Vote(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId);
            await VoteException(OneElf, VoteOption.Approved, AdvisoryHc1T1VProposalId, "Invalid voter: not HC");
            
            // NetworkDao
            await Vote(UniqueVoteVoteAmount, VoteOption.Abstained, NetworkDaoGovernanceR1A1VProposalId);
            await Vote(UniqueVoteVoteAmount, VoteOption.Rejected, NetworkDaoGovernanceHc1A1VProposalId);
            await Vote(OneElf, VoteOption.Abstained, NetworkDaoGovernanceR1T1VProposalId);
            await Vote(OneElf, VoteOption.Rejected, NetworkDaoGovernanceHc1T1VProposalId);
            await Vote(UniqueVoteVoteAmount, VoteOption.Abstained, NetworkDaoAdvisoryR1A1VProposalId);
            await Vote(UniqueVoteVoteAmount, VoteOption.Rejected, NetworkDaoAdvisoryHc1A1VProposalId);
            await Vote(OneElf, VoteOption.Abstained, NetworkDaoAdvisoryR1T1VProposalId);
            await Vote(OneElf, VoteOption.Rejected, NetworkDaoAdvisoryHc1T1VProposalId);
        }

        [Fact]
        public async Task WithdrawTest()
        {
            await RegisterTest();
            await TokenContractStub.Approve.SendAsync(new ApproveInput { Amount = OneElf * 10, Symbol = TokenElf, Spender = VoteContractAddress });
            await Vote(OneElf, VoteOption.Approved, GovernanceR1T1VProposalId);
            BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
            await Withdraw(DaoId, new VotingItemIdList { Value = { GovernanceR1T1VProposalId } }, OneElf);
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