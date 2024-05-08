using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        [Fact]
        public async Task CreateVoteSchemeTest_NotInitialized()
        {
            var result = await VoteContractStub.CreateVoteScheme.SendWithExceptionAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldContain("Not initialized yet.");
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest()
        {
            await InitializeTest();
            var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldBe("");
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest_AlreadyExisted()
        {
            await CreateVoteSchemeTest();
            var result = await VoteContractStub.CreateVoteScheme.SendWithExceptionAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldContain("VoteScheme already exists.");
        }
    }
    
}