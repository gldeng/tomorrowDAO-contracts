using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract
{
    private static bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }
    
    private static bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private void AssertCommon<T>(T input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        Assert(input != null, "Input is null.");
    }

    private DAOInfo AssertDao(Hash daoId)
    {
        Assert(IsHashValid(daoId), "Invalid daoId.");
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoInfo != null && daoInfo.DaoId == daoId, "DAO not exists.");
        return daoInfo;
    }
    
    private DAOInfo AssertDaoSubsist(Hash daoId)
    {
        var daoInfo = AssertDao(daoId);
        Assert(daoInfo.SubsistStatus, "DAO not subsist.");
        return daoInfo;
    }
    
    private ProposalInfoOutput AssertProposal(Hash proposalId)
    {
        Assert(IsHashValid(proposalId) && State.VotingItems[proposalId] == null, "Invalid proposalId.");
        var proposalInfo = State.GovernanceContract.GetProposalInfo.Call(proposalId);
        Assert(proposalInfo != null && proposalInfo.ProposalId == proposalId, "Proposal not exists.");
        return proposalInfo;
    }

    private GovernanceScheme AssertGovernanceScheme(Address schemeAddress)
    {
        Assert(IsAddressValid(schemeAddress), "Invalid schemeAddress.");
        var governanceScheme = State.GovernanceContract.GetGovernanceScheme.Call(schemeAddress);
        Assert(governanceScheme != null && governanceScheme.SchemeAddress == schemeAddress, "governanceScheme not exists.");
        return governanceScheme;
    }

    private VoteScheme AssertVoteScheme(Hash voteSchemeId)
    {
        Assert(IsHashValid(voteSchemeId) && State.VoteSchemes[voteSchemeId] != null, "Invalid voteSchemeId.");
        return State.VoteSchemes[voteSchemeId];
    }
    
    private VotingItem AssertVotingItem(Hash votingItemId)
    {
        Assert(IsHashValid(votingItemId) && State.VotingItems[votingItemId] != null, "Invalid votingItemId.");
        return State.VotingItems[votingItemId];
    }

    private void AssertVotingRecord(Hash votingItemId, Address voter)
    {
        Assert(IsHashValid(votingItemId), "Invalid votingItemId.");
        Assert(State.VotingRecords[votingItemId][voter] == null, "Voter already voted.");
    }

    private void AssertToken(string token)
    {
        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = token
        });
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), "Token not exists.");
    }

    private void AssertHighCouncil(Address voter)
    {
        //todo check voter from election contract
    }
    
    private void AssertBP(Address voter)
    {
        Assert(State.AEDPoSContract.IsCurrentMiner.Call(voter).Value, "Invalid voter: not BP.");
    }

    private long AssertWithdraw(Address user, Hash daoId)
    {
        Assert(IsAddressValid(user), "Invalid withdraw user.");
        Assert(State.RemainVoteAmounts[user][daoId] > 0, "Invalid withdraw amount.");
        return State.RemainVoteAmounts[user][daoId];
    }

    private Hash GetVirtualAddressHash(Address user, Hash daoId)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(user), HashHelper.ComputeFrom(daoId));
    }
    
    private Address GetVirtualAddress(Address user, Hash daoId)
    {
        return Context.ConvertVirtualAddressToContractAddress(GetVirtualAddressHash(user, daoId));
    }
}