using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract
{
    public override Hash CreateProposal(CreateProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme);
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo,
            input.ProposalType, input.Transaction);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, scheme.SchemeAddress, scheme.GovernanceToken);
        FireProposalCreatedEvent(proposal);
        return proposalId;
    }

    public override Hash CreateVetoProposal(CreateVetoProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme);
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        var vetoProposalId = input.VetoProposalId;
        var vetoProposal = State.Proposals[vetoProposalId];
        Assert(vetoProposal != null, "Veto proposal not found.");

        var status = GetProposalStatus(vetoProposal);
        Assert(status == ProposalStatus.Pending, "Invalid veto proposal status.");
        var transaction = new ExecuteTransaction
        {
            ToAddress = Context.Self,
            ContractMethodName = nameof(VetoProposal),
            Params = input.VetoProposalId.ToByteString()
        };
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo, input.ProposalTime,
            ProposalType.Veto, transaction);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, scheme.SchemeAddress, scheme.GovernanceToken);
        vetoProposal.ProposalStatus = ProposalStatus.Challenged;
        State.Proposals[vetoProposalId] = vetoProposal;
        FireProposalCreatedEvent(proposal);
        return proposalId;
    }

    private Hash CheckAndGetProposalId<T>(T input, ProposalBasicInfo proposalBasicInfo, out GovernanceScheme scheme)
        where T : IMessage<T>
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        AssertParams(proposalBasicInfo, proposalBasicInfo.DaoId, proposalBasicInfo.VoteSchemeId,
            proposalBasicInfo.SchemeAddress);
        Assert(
            proposalBasicInfo.ProposalDescription.Length <=
            GovernanceContractConstants.MaxProposalDescriptionUrlLength && ValidateForumUrl(proposalBasicInfo.ForumUrl),
            "Invalid proposal description or forum url.");
        scheme = State.GovernanceSchemeMap[proposalBasicInfo.SchemeAddress];
        var schemeAddressList = State.DaoSchemeAddressList[proposalBasicInfo.DaoId];
        Assert(
            scheme != null && schemeAddressList != null && schemeAddressList.Value.Count > 0 &&
            schemeAddressList.Value.Contains(proposalBasicInfo.SchemeAddress), "Invalid scheme address.");
        var proposalId = GenerateId(input, Context.TransactionId);
        return proposalId;
    }

    private bool ValidateForumUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        var result = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return result;
    }

    private ProposalInfo ValidateAndGetProposalInfo(Hash proposalId, ProposalBasicInfo proposalBasicInfo,
        ProposalType proposalType, ExecuteTransaction executeTransaction = null, Hash vetoProposalId = null)
    {
        if (proposalType == ProposalType.Governance)
        {
            Assert(!ValidateExecuteTransaction(executeTransaction), "Invalid execute transaction.");
        }

        var proposal = new ProposalInfo
        {
            ProposalBasicInfo = proposalBasicInfo,
            ProposalId = proposalId,
            ProposalType = proposalType,
            ProposalTime = GetProposalTimePeriod(proposalBasicInfo, proposalType),
            ProposalStatus = ProposalStatus.PendingVote,
            ProposalStage = ProposalStage.Active,
            Proposer = Context.Sender,
            Transaction = executeTransaction,
            VetoProposalId = vetoProposalId
        };
        return proposal;
    }

    private bool ValidateExecuteTransaction(ExecuteTransaction transaction)
    {
        AssertParams(transaction, transaction.ToAddress, transaction.ContractMethodName);
        return transaction.ContractMethodName == nameof(VetoProposal) && transaction.ToAddress == Context.Self;
    }

    private GovernanceScheme GetScheme(Address schemeAddress)
    {
        var scheme = State.GovernanceSchemeMap[schemeAddress];
        Assert(scheme != null, "Scheme not found.");
        return scheme;
    }

    private void RegisterVotingItem(ProposalInfo proposal, Address schemeAddress, string governanceToken)
    {
        State.VoteContract.Register.Send(new VotingRegisterInput
        {
            SchemeAddress = schemeAddress,
            VotingItemId = proposal.ProposalId,
            StartTimestamp = proposal.ProposalTime.ActiveStartTime,
            EndTimestamp = proposal.ProposalTime.ActiveEndTime,
            AcceptedToken = governanceToken
        });
    }

    private void FireProposalCreatedEvent(ProposalInfo proposal)
    {
        var basicInfo = proposal.ProposalBasicInfo;
        var proposalTime = proposal.ProposalTime;
        Context.Fire(new ProposalCreated
        {
            DaoId = basicInfo.DaoId,
            ProposalId = proposal.ProposalId,
            ProposalType = proposal.ProposalType,
            ProposalTitle = basicInfo.ProposalTitle,
            ProposalDescription = basicInfo.ProposalDescription,
            ProposalStatus = proposal.ProposalStatus,
            ProposalStage = ProposalStage.Active,
            ActiveStartTime = proposalTime.ActiveStartTime,
            ActiveEndTime = proposalTime.ActiveEndTime,
            ExecuteStartTime = proposalTime.ExecuteStartTime,
            ExecuteEndTime = proposalTime.ExecuteEndTime,
            SchemeAddress = basicInfo.SchemeAddress,
            VoteSchemeId = basicInfo.VoteSchemeId,
            Transaction = proposal.Transaction,
            Proposer = proposal.Proposer,
            VetoProposalId = proposal.VetoProposalId
        });
    }

    public override Empty VetoProposal(Hash input)
    {
        var proposal = State.Proposals[input];
        Assert(proposal != null && ValidatePermission(proposal.ProposalBasicInfo.DaoId, Context.Sender),
            "Invalid proposal or no permission.");
        return new Empty();
    }

    public override Empty ExecuteProposal(Hash input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        AssertParams(input);
        var proposal = State.Proposals[input];
        Assert(proposal != null, "Proposal not found.");
        // todo: permission
        Assert(Context.Sender == proposal.Proposer, "No permission.");
        ExecuteProposal(proposal);
        State.Proposals.Remove(proposal.ProposalId);
        return new Empty();
    }

    //
    // public override Empty ExecuteOrganizationProposal(Hash input)
    // {
    //     Assert(State.Initialized.Value, "Not initialized yet.");
    //     AssertParams(input);
    //     var proposal = State.OrganizationProposals[input];
    //     Assert(proposal != null, "Proposal not found.");
    //     // todo: permission
    //     Assert(Context.Sender == proposal.Proposer, "No permission.");
    //     ExecuteProposal(proposal, true);
    //     State.OrganizationProposals.Remove(proposal.ProposalId);
    //     return new Empty();
    // }
    //
    private void ExecuteProposal(ProposalInfo proposal, bool isExecute = false)
    {
        Assert(Context.CurrentBlockTime > proposal.EndTime && Context.CurrentBlockTime < proposal.ExpiredTime,
            "The proposal is in active or expired.");
        var voteResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
        {
            VotingItemId = proposal.ProposalId
        });
        proposal = CheckProposalStatus(proposal, voteResult);
        Assert(proposal.ProposalStatus == ProposalStatus.Approved, "Proposal not approve.");

        if (isExecute)
        {
            var organization = State.Organizations[proposal.OrganizationAddress];
            Context.SendVirtualInline(
                CalculateVirtualHash(organization.OrganizationHash, organization.CreationSalt),
                proposal.Transaction.ToAddress,
                proposal.Transaction.ContractMethodName, proposal.Transaction.Params);
        }

        Context.Fire(new ProposalExecuted
        {
            ProposalId = proposal.ProposalId,
            ExecuteTime = Context.CurrentBlockTime,
            OrganizationAddress = proposal.OrganizationAddress
        });
    }

    private ProposalStatusOutput GetProposalStatus(ProposalInfo proposalInfo)
    {
        var votingResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
        {
            VotingItemId = proposalInfo.ProposalId
        });
        var threshold = State.ProposalGovernanceSchemeSnapShot[proposalInfo.ProposalId];
        var proposalStage = proposalInfo.ProposalStage;
        var proposalStatus = proposalInfo.ProposalStatus;
        var proposalTime = proposalInfo.ProposalTime;
        var result = new ProposalStatusOutput
        {
            ProposalStatus = proposalInfo.ProposalStatus,
            ProposalStage = proposalInfo.ProposalStage
        };
        
        if (proposalStage == ProposalStage.Active && Context.CurrentBlockTime < proposalTime.ActiveEndTime)
        {
            return result;
        }
        
        var totalVote = votingResult.VotesAmount;
        var approveVote = votingResult.ApproveCounts;
        var rejectVote = votingResult.RejectCounts;
        var abstainVote = votingResult.AbstainCounts;
        var totalVoter = votingResult.TotalVotersCount;
        var enoughVoter = totalVoter >= threshold.MinimalRequiredThreshold;
        if (!enoughVoter)
        {
            proposalStatus = ProposalStatus.Expired;
            return proposalStatus;
        }

        var enoughVote = rejectVote.Add(abstainVote).Add(approveVote) >= threshold.MinimalVoteThreshold;
        if (!enoughVote)
        {
            proposalStatus = ProposalStatus.Expired;
            return proposalStatus;
        }

        var isReject = (rejectVote / totalVote) * GovernanceContractConstants.AbstractVoteTotal >
                       threshold.MaximalRejectionThreshold;
        if (isReject)
        {
            proposalStatus = ProposalStatus.Rejected;
            return proposalStatus;
        }

        var isAbstained = abstainVote / totalVote * GovernanceContractConstants.AbstractVoteTotal >
                          threshold.MaximalAbstentionThreshold;
        if (isAbstained)
        {
            proposalStatus = ProposalStatus.Abstained;
            return proposalStatus;
        }

        var isApproved = approveVote / totalVote * GovernanceContractConstants.AbstractVoteTotal >
                         threshold.MinimalApproveThreshold;
        if (isApproved)
        {
            proposalStatus = ProposalStatus.Approved;
            if (HasPendingStatus(proposalInfo.ProposalId))
            {
                proposalStatus = ProposalStatus.Pending;
                return proposalStatus;
            }

            return proposalStatus;
        }


        proposalStatus = ProposalStatus.Expired;
        return proposalStatus;
    }

    private bool HasPendingStatus(Hash proposalId)
    {
        var proposal = State.Proposals[proposalId];
        return Context.CurrentBlockTime >= proposal.ProposalTime.ActiveEndTime &&
               proposal.ProposalType == ProposalType.Governance &&
               State.GovernanceSchemeMap[proposal.ProposalBasicInfo.SchemeAddress].GovernanceMechanism ==
               GovernanceMechanism.HighCouncil;
    }
    //
    // public override Empty SetProposalTimePeriod(SetProposalTimePeriodInput input)
    // {
    //     var minActiveTimePeriod = input.ProposalTimePeriod.MinActiveTimePeriod;
    //     var maxActiveTimePeriod = input.ProposalTimePeriod.MaxActiveTimePeriod;
    //     var expiredTimePeriod = input.ProposalTimePeriod.ExecuteTimePeriod;
    //     Assert(input.DaoId != null && minActiveTimePeriod > 0 &&
    //            maxActiveTimePeriod > 0 &&
    //            expiredTimePeriod > 0 && minActiveTimePeriod < maxActiveTimePeriod &&
    //            maxActiveTimePeriod < expiredTimePeriod, "Invalid input.");
    //     var timePeriod = new DaoProposalTimePeriod
    //     {
    //         MinActiveTimePeriod = minActiveTimePeriod,
    //         MaxActiveTimePeriod = maxActiveTimePeriod,
    //         ExecuteTimePeriod = expiredTimePeriod
    //     };
    //     State.DaoProposalTimePeriods[input.DaoId] = timePeriod;
    //     return new Empty();
    // }
    //
    // public override Empty SetOrganizationProposalWhitelistTransaction(
    //     SetOrganizationProposalWhitelistTransactionInput input)
    // {
    //     State.WhitelistTransactionList.Value.WhitelistTransactionList_.AddRange(input.WhitelistTransactionList
    //         .WhitelistTransactionList_);
    //     return new Empty();
    // }
    //
    // #region View
    //
    // public override ProposalInfo GetProposalInfo(Hash input)
    // {
    //     var voteResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
    //     {
    //         VotingItemId = input
    //     });
    //     var proposal = State.Proposals[input];
    //     return CheckProposalStatus(proposal, voteResult);
    // }
    //
    // public override DaoProposalTimePeriod GetProposalTimePeriod(Hash input)
    // {
    //     return GetDaoProposalTimePeriod(input);
    // }
    //
    // public override WhitelistTransactionList GetOrganizationProposalWhitelistTransaction(Empty input)
    // {
    //     return new WhitelistTransactionList
    //     {
    //         WhitelistTransactionList_ = { GetWhitelistTransactionList() }
    //     };
    // }
    //
    // public override GovernanceSubScheme GetProposalSnapShotScheme(Hash input)
    // {
    //     return State.ProposalGovernanceSchemeSnapShot[input];
    // }
    //
    // #endregion
}