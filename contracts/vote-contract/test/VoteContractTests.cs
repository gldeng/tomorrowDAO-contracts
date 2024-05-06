using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Vote
{ 
    public class VoteContractTest : VoteContractTestBase
    {
        [Fact]
        public async Task InitializeTest()
        {
            var result = await VoteContractStub.Initialize.SendAsync(new InitializeInput
            {
                DaoContractAddress = DAOContractAddress,
                ElectionContractAddress = ElectionContractAddress,
                GovernanceContractAddress = GovernanceContractAddress,
            });
            result.TransactionResult.Error.ShouldBe("");
        }
        
        [Fact]
        public async Task InitializeTest_AlreadyInitialized()
        {
            await InitializeTest();
            var result = await VoteContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
        
        [Fact]
        public async Task InitializeTest_NoPermission()
        {
            var result = await VoteContractStubOther.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task InitializeTest_InvalidInput()
        {
            var result = await VoteContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
    }
    
}