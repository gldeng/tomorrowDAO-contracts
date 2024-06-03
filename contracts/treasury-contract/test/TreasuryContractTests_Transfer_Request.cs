using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsTransferRequest : TreasuryContractTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TreasuryContractTestsTransferRequest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RequestTransferTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        await CreateTreasuryAddDonateAndStaking(daoId);
        
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var schemeAddress = addressList.Value.FirstOrDefault();
        await MockVoteScheme();
        var voteMechanismId = await GetVoteSchemeId(VoteMechanism.UniqueVote);

        var result = await TreasuryContractStub.RequestTransfer.SendAsync(new RequestTransferInput
        {
            DaoId = daoId,
            Amount = OneElfAmount * 5,
            Symbol = DefaultGovernanceToken,
            Recipient = UserAddress,
            ProposalInfo = new ProposalInfo
            {
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "http://121.id",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteMechanismId
            }
        });

        var treasuryTokenLocked = GetLogEvent<TreasuryTokenLocked>(result.TransactionResult);
        treasuryTokenLocked.ShouldNotBeNull();
        treasuryTokenLocked.LockInfo.ShouldNotBeNull();
        var proposalId = treasuryTokenLocked.LockInfo.ProposalId;
        var lockId = treasuryTokenLocked.LockInfo.LockId;
        var proposalCreated = GetLogEvent<ProposalCreated>(result.TransactionResult);
        proposalCreated.ShouldNotBeNull();
        proposalCreated.ProposalId.ShouldBe(proposalId);

        // var treasuryAddress = await TreasuryContractStub.GetTreasuryAccountAddress.CallAsync(daoId);
        var treasuryInfo = await TreasuryContractStub.GetFundInfo.CallAsync(new GetFundInfoInput
        {
            DaoId = daoId,
            Symbol = DefaultGovernanceToken
        });
        treasuryInfo.ShouldNotBeNull();
        treasuryInfo.AvailableFunds.ShouldBe(OneElfAmount * 15);
        treasuryInfo.LockedFunds.ShouldBe(OneElfAmount * 5);

        var totalFundInfo = await TreasuryContractStub.GetTotalFundInfo.CallAsync(new GetTotalFundInfoInput
        {
            Symbol = DefaultGovernanceToken
        });
        totalFundInfo.ShouldNotBeNull();
        totalFundInfo.AvailableFunds.ShouldBe(OneElfAmount * 15);
        totalFundInfo.LockedFunds.ShouldBe(OneElfAmount * 5);

        var lockInfo = await TreasuryContractStub.GetLockInfo.CallAsync(new LockInfoInput
        {
            ProposalId = proposalId,
            LockId = null
        });
        lockInfo.ShouldNotBeNull();
        lockInfo.Amount.ShouldBe(OneElfAmount * 5);
        
        lockInfo = await TreasuryContractStub.GetLockInfo.CallAsync(new LockInfoInput
        {
            ProposalId = null,
            LockId = lockInfo.LockId
        });
        lockInfo.ShouldNotBeNull();
        lockInfo.Amount.ShouldBe(OneElfAmount * 5);
    }
}