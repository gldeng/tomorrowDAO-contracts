using System;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
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
            var result = await VoteContractStub.Register.SendWithExceptionAsync(new VotingRegisterInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task RegisterTest()
        {
            await InitializeAll();
            await CreateVoteSchemeTest();
            var result = await VoteContractStub.Register.SendAsync(new VotingRegisterInput
            {
                VotingItemId = ProposalId,
                SchemeId = UniqueVoteVoteSchemeId,
                AcceptedToken = TokenElf,
                StartTimestamp = Timestamp.FromDateTime(new DateTime(2024, 5, 8, 0, 0, 0, DateTimeKind.Utc)),
                EndTimestamp = Timestamp.FromDateTime(new DateTime(2024, 5, 9, 0, 0, 0, DateTimeKind.Utc))
            });
            
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
            _testOutputHelper.WriteLine("DaoId={id}", DaoId);
        }
    }
    
}