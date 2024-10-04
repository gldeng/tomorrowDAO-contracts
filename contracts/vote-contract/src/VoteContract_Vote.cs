using System;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using AnonymousVote;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.DAO;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty Register(VotingRegisterInput input)
    {
        AssertCommon(input);
        Assert(Context.Sender == State.GovernanceContract.Value, "No permission.");
        var voteScheme = AssertVoteScheme(input.SchemeId);

        if (VoteMechanism.TokenBallot == voteScheme.VoteMechanism)
        {
            AssertToken(input.AcceptedToken);
        }

        var proposalInfo = AssertProposal(input.VotingItemId);
        AssertDaoSubsist(proposalInfo.DaoId);
        var governanceScheme = AssertGovernanceScheme(proposalInfo.SchemeAddress);

        State.VotingItems[input.VotingItemId] = new VotingItem
        {
            DaoId = proposalInfo.DaoId,
            VotingItemId = input.VotingItemId,
            SchemeId = input.SchemeId,
            AcceptedSymbol = input.AcceptedToken,
            RegisterTimestamp = Context.CurrentBlockTime,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp,
            GovernanceMechanism = governanceScheme.GovernanceMechanism.ToString(),
            IsAnonymous = input.IsAnonymous
        };
        State.VotingResults[input.VotingItemId] = new VotingResult
        {
            VotingItemId = input.VotingItemId,
            ApproveCounts = 0,
            RejectCounts = 0,
            AbstainCounts = 0,
            VotesAmount = 0,
            TotalVotersCount = 0,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp,
        };
        Context.Fire(new VotingItemRegistered
        {
            DaoId = proposalInfo.DaoId,
            VotingItemId = input.VotingItemId,
            SchemeId = input.SchemeId,
            AcceptedCurrency = input.AcceptedToken,
            RegisterTimestamp = Context.CurrentBlockTime,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp,
            IsAnonymous = input.IsAnonymous
        });
        if (input.IsAnonymous)
        {
            OnNewAnonymousVotingItemAdded(State.VotingItems[input.VotingItemId]);
        }
        return new Empty();
    }

    /// <summary>
    /// Register commitment for anonymous voting.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty RegisterCommitment(RegisterCommitmentInput input)
    {
        Assert(input.VotingItemId != null, "Invalid voting item id.");
        Assert(input.Commitment != null, "Invalid commitment.");

        AssertCommon(input);
        var votingItem = AssertVotingItem(input.VotingItemId);
        Assert(votingItem.IsAnonymous, "Not an anonymous voting.");
        Assert(votingItem.StartTimestamp <= Context.CurrentBlockTime,
            "Commitment registration has not started.");
        Assert(CommitmentDeadline(votingItem) >= Context.CurrentBlockTime,
            "Commitment registration has ended.");

        var daoInfo = AssertDaoSubsist(votingItem.DaoId);
        var voteScheme = AssertVoteScheme(votingItem.SchemeId);
        AssertEligibleVoter(daoInfo, voteScheme, votingItem, input.VoteAmount);
        
        Commit(input.VotingItemId, input.Commitment);
        return new Empty();
    }

    public override Empty Vote(VoteInput input)
    {
        AssertCommon(input);
        var votingItem = AssertVotingItem(input.VotingItemId);
        Assert(votingItem.StartTimestamp <= Context.CurrentBlockTime, "Vote not begin.");
        Assert(votingItem.EndTimestamp >= Context.CurrentBlockTime, "Vote ended.");
        var daoInfo = AssertDaoSubsist(votingItem.DaoId);
        var voteScheme = AssertVoteScheme(votingItem.SchemeId);
        var voteId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input), HashHelper.ComputeFrom(Context.Sender),
            Context.TransactionId);
        if (votingItem.IsAnonymous)
        {
            Assert(CommitmentDeadline(votingItem) < Context.CurrentBlockTime, "Anonymous vote has not started.");
            Nullify(input.VotingItemId, input.VoteOption, input.AnonymousVoteExtraInfo.Nullifier, input.AnonymousVoteExtraInfo.Proof);
            // Disucssion: Currently we don't record voting record for anonymous voting. Is it needed?
            UpdateVotingResultsForAnonymousVoting(input);
        }
        else
        {
            AssertEligibleVoter(daoInfo, voteScheme, votingItem, input.VoteAmount);
            AssertVotingRecord(votingItem.VotingItemId, Context.Sender, voteScheme);      
            var newVoter = AddVotingRecords(input, voteId);
            UpdateVotingResults(input, newVoter ? 1 : 0);
        }

        Context.Fire(new Voted
        {
            VotingItemId = votingItem.VotingItemId,
            Voter = Context.Sender,
            Amount = input.VoteAmount,
            VoteTimestamp = Context.CurrentBlockTime,
            Option = (VoteOption)input.VoteOption,
            VoteId = voteId,
            DaoId = votingItem.DaoId,
            VoteMechanism = voteScheme.VoteMechanism,
            StartTime = votingItem.StartTimestamp,
            EndTime = votingItem.EndTimestamp,
            Memo = input.Memo
        });
        return new Empty();
    }

    public override Empty Withdraw(WithdrawInput input)
    {
        AssertCommon(input);
        var daoInfo = AssertDao(input.DaoId);
        var withdrawAmount = AssertWithdraw(Context.Sender, input);
        var virtualAddressHash = GetVirtualAddressHash(Context.Sender, input.DaoId);
        TransferOut(virtualAddressHash, Context.Sender, daoInfo.GovernanceToken, withdrawAmount);
        RemoveAmount(input);
        Context.Fire(new Withdrawn
        {
            DaoId = input.DaoId,
            WithdrawAmount = withdrawAmount,
            Withdrawer = Context.Sender,
            WithdrawTimestamp = Context.CurrentBlockTime,
            VotingItemIdList = input.VotingItemIdList
        });
        return new Empty();
    }

    private void AssertEligibleVoter(DAOInfo daoInfo, VoteScheme voteScheme, VotingItem votingItem, long voteAmount)
    {
        if (GovernanceMechanism.HighCouncil.ToString() == votingItem.GovernanceMechanism)
        {
            if (daoInfo.IsNetworkDao)
            {
                AssertBP(Context.Sender);
            }
            else
            {
                AssertHighCouncil(daoInfo.DaoId, Context.Sender);
            }
        }
        else if (GovernanceMechanism.Organization.ToString() == votingItem.GovernanceMechanism)
        {
            AssertOrganizationMember(daoInfo.DaoId, Context.Sender);
        }

        switch (voteScheme.VoteMechanism)
        {
            case VoteMechanism.TokenBallot: // 1t1v
                TokenBallotTransfer(votingItem, voteAmount, voteScheme);
                AddAmount(votingItem, voteAmount, voteScheme);
                break;
            case VoteMechanism.UniqueVote: // 1a1v
                Assert(voteAmount == VoteContractConstants.UniqueVoteVoteAmount, "Invalid vote amount");
                break;
        }
    }

    private void TokenBallotTransfer(VotingItem votingItem, long voteAmount, VoteScheme voteScheme)
    {
        if (voteScheme.WithoutLockToken)
        {
            AssertTokenBalance(Context.Sender, votingItem.AcceptedSymbol, voteAmount);
        }
        else
        {
            var virtualAddress = GetVirtualAddress(Context.Sender, votingItem.DaoId);
            TransferIn(virtualAddress, Context.Sender, votingItem.AcceptedSymbol, voteAmount);
        }
    }

    private void AddAmount(VotingItem votingItem, long amount, VoteScheme voteScheme)
    {
        if (voteScheme.WithoutLockToken)
        {
            return;
        }

        State.DaoRemainAmounts[Context.Sender][votingItem.DaoId] += amount;
        State.DaoProposalRemainAmounts[Context.Sender][GetDaoProposalId(votingItem.DaoId, votingItem.VotingItemId)] =
            amount;
    }

    private void RemoveAmount(WithdrawInput input)
    {
        State.DaoRemainAmounts[Context.Sender][input.DaoId] -= input.WithdrawAmount;
        foreach (var votingItemId in input.VotingItemIdList.Value)
        {
            State.DaoProposalRemainAmounts[Context.Sender].Remove(GetDaoProposalId(input.DaoId, votingItemId));
        }
    }

    private bool AddVotingRecords(VoteInput input, Hash voteId)
    {
        var votingRecord = State.VotingRecords[input.VotingItemId][Context.Sender];
        var newVoter = votingRecord == null;
        if (newVoter)
        {
            State.VotingRecords[input.VotingItemId][Context.Sender] = new VotingRecord
            {
                VotingItemId = input.VotingItemId,
                Voter = Context.Sender,
                Amount = input.VoteAmount,
                VoteTimestamp = Context.CurrentBlockTime,
                Option = (VoteOption)input.VoteOption,
                VoteId = voteId
            };
        }
        else
        {
            votingRecord.Amount += input.VoteAmount;
            votingRecord.VoteTimestamp = Context.CurrentBlockTime;
            votingRecord.VoteId = voteId;
            State.VotingRecords[input.VotingItemId][Context.Sender] = votingRecord;
        }

        return newVoter;
    }

    private void UpdateVotingResults(VoteInput input, long deltaVoter)
    {
        var votingResult = State.VotingResults[input.VotingItemId];
        votingResult.VotesAmount += input.VoteAmount;
        votingResult.TotalVotersCount += deltaVoter;
        switch (input.VoteOption)
        {
            case (int)VoteOption.Approved:
                votingResult.ApproveCounts += input.VoteAmount;
                break;
            case (int)VoteOption.Rejected:
                votingResult.RejectCounts += input.VoteAmount;
                break;
            case (int)VoteOption.Abstained:
                votingResult.AbstainCounts += input.VoteAmount;
                break;
        }

        State.VotingResults[input.VotingItemId] = votingResult;
    }

    private void UpdateVotingResultsForAnonymousVoting(VoteInput input)
    {
        var votingResult = State.VotingResults[input.VotingItemId];
        votingResult.TotalVotersCount += 1;
        switch (input.VoteOption)
        {
            case (int)VoteOption.Approved:
                votingResult.ApproveCounts += 1;
                break;
            case (int)VoteOption.Rejected:
                votingResult.RejectCounts += 1;
                break;
            case (int)VoteOption.Abstained:
                votingResult.AbstainCounts += 1;
                break;
        }

        State.VotingResults[input.VotingItemId] = votingResult;
    }

    private void TransferIn(Address virtualAddress, Address from, string symbol, long amount)
    {
        State.TokenContract.TransferFrom.Send(
            new TransferFromInput
            {
                Symbol = symbol,
                Amount = amount,
                From = from,
                Memo = "TransferIn",
                To = virtualAddress
            });
    }

    private void TransferOut(Hash virtualAddressHash, Address to, string symbol, long amount)
    {
        State.TokenContract.Transfer.VirtualSend(virtualAddressHash,
            new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                Memo = "TransferOut",
                To = to
            });
    }

    private Timestamp CommitmentDeadline(VotingItem votingItem)
    {
       var duration = votingItem.EndTimestamp - votingItem.StartTimestamp;
       var halfWay = new Duration()
       {
           Seconds = duration.Seconds / 2
       };
       return votingItem.StartTimestamp + halfWay;
    }
    
    #region View

    public override VotingItem GetVotingItem(Hash input)
    {
        return State.VotingItems[input] ?? new VotingItem();
    }

    public override VotingResult GetVotingResult(Hash input)
    {
        return State.VotingResults[input] ?? new VotingResult();
    }

    public override VotingRecord GetVotingRecord(GetVotingRecordInput input)
    {
        AssertCommon(input);
        return State.VotingRecords[input.VotingItemId][input.Voter] ?? new VotingRecord();
    }

    public override Address GetVirtualAddress(GetVirtualAddressInput input)
    {
        return GetVirtualAddress(input.Voter, input.DaoId);
    }

    public override DaoRemainAmount GetDaoRemainAmount(GetDaoRemainAmountInput input)
    {
        return new DaoRemainAmount
        {
            DaoId = input.DaoId,
            Amount = State.DaoRemainAmounts[input.Voter][input.DaoId]
        };
    }

    public override ProposalRemainAmount GetProposalRemainAmount(GetProposalRemainAmountInput input)
    {
        AssertCommon(input);
        return new ProposalRemainAmount
        {
            DaoId = input.DaoId,
            VotingItemId = input.VotingItemId,
            Amount = State.DaoProposalRemainAmounts[input.Voter][GetDaoProposalId(input.DaoId, input.VotingItemId)]
        };
    }

    public override AddressList GetBPAddresses(Empty input)
    {
        var minerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty());
        var minerAddressList = minerList.Pubkeys
            .Select(x => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(x.ToHex()))).ToList();
        return new AddressList
        {
            Value = { minerAddressList }
        };
    }

    #endregion
}