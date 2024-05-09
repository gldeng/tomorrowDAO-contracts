using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract
{
    public override Hash CreateProposal(CreateProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme);
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        Assert(input.ProposalType != ProposalType.Unused && input.ProposalType != ProposalType.Veto,
            "ProposalType cannot be Unused or Veto.");
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo,
            input.ProposalType, input.Transaction);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, input.ProposalBasicInfo.VoteSchemeId, scheme.GovernanceToken);
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
        Assert(status.ProposalStage == ProposalStage.Pending, "Invalid veto proposal stage.");
        Assert(status.ProposalStatus == ProposalStatus.Approved, "Invalid veto proposal status.");
        var transaction = new ExecuteTransaction
        {
            ToAddress = Context.Self,
            ContractMethodName = nameof(VetoProposal),
            Params = input.VetoProposalId.ToByteString()
        };
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo,
            ProposalType.Veto, transaction, vetoProposalId);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, input.ProposalBasicInfo.VoteSchemeId, scheme.GovernanceToken);
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
            VetoProposalId = vetoProposalId,
            ForumUrl = proposalBasicInfo.ForumUrl
        };
        return proposal;
    }

    private bool ValidateExecuteTransaction(ExecuteTransaction transaction)
    {
        AssertParams(transaction, transaction?.ToAddress, transaction?.ContractMethodName);
        // normal proposal can not call VetoProposal method
        return transaction.ContractMethodName == nameof(VetoProposal) && transaction.ToAddress == Context.Self;
    }

    private GovernanceScheme GetScheme(Address schemeAddress)
    {
        var scheme = State.GovernanceSchemeMap[schemeAddress];
        Assert(scheme != null, "Scheme not found.");
        return scheme;
    }

    private void RegisterVotingItem(ProposalInfo proposal, Hash voteSchemeId, string governanceToken)
    {
        State.VoteContract.Register.Send(new VotingRegisterInput
        {
            VotingItemId = proposal.ProposalId,
            SchemeId = voteSchemeId,
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
            VetoProposalId = proposal.VetoProposalId,
            ForumUrl = proposal.ForumUrl
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
        Assert(Context.Sender == proposal.Proposer, "No permission.");
        ExecuteProposal(proposal);
        State.Proposals.Remove(proposal.ProposalId);
        return new Empty();
    }

    private void ExecuteProposal(ProposalInfo proposal, bool isExecute = false)
    {
        Assert(Context.CurrentBlockTime > proposal.ProposalTime.ExecuteStartTime
               && Context.CurrentBlockTime < proposal.ProposalTime.ExecuteEndTime,
            "The proposal is in active or expired.");
        var proposalStatusOutput = GetProposalStatus(proposal);
        Assert(proposalStatusOutput.ProposalStatus == ProposalStatus.Approved
               && proposalStatusOutput.ProposalStage == ProposalStage.Execute, "Proposal can not execute.");
        var governanceScheme = GetGovernanceScheme(proposal.ProposalBasicInfo.SchemeAddress);
        Assert(governanceScheme != null, "GovernanceScheme not found.");
        var schemeId = governanceScheme.SchemeId;

        if (isExecute)
        {
            Context.SendVirtualInline(
                schemeId,
                proposal.Transaction.ToAddress,
                proposal.Transaction.ContractMethodName, proposal.Transaction.Params);
        }

        Context.Fire(new ProposalExecuted
        {
            ProposalId = proposal.ProposalId,
            ExecuteTime = Context.CurrentBlockTime
        });
    }

    public override ProposalStatusOutput GetProposalStatus(Hash input)
    {
        Assert(input != null && input != Hash.Empty, "Invalid input.");
        var proposal = State.Proposals[input];
        return proposal == null ? new ProposalStatusOutput() : GetProposalStatus(proposal);
    }

    private ProposalStatusOutput GetProposalStatus(ProposalInfo proposalInfo)
    {
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

        var votingResult = State.VoteContract.GetVotingResult.Call(proposalInfo.ProposalId);

        var totalVote = votingResult.VotesAmount;
        var approveVote = votingResult.ApproveCounts;
        var rejectVote = votingResult.RejectCounts;
        var abstainVote = votingResult.AbstainCounts;
        var totalVoter = votingResult.TotalVotersCount;
        var enoughVoter = totalVoter >= threshold.MinimalRequiredThreshold;
        if (!enoughVoter)
        {
            return new ProposalStatusOutput
            {
                ProposalStatus = ProposalStatus.BelowThreshold,
                ProposalStage = ProposalStage.Finished
            };
        }
        //the proposal of 1a1v is not subject to this control
        var schemeAddress = proposalInfo.ProposalBasicInfo.SchemeAddress;
        var governanceScheme = State.GovernanceSchemeMap[schemeAddress];
        var voteSchemeId = proposalInfo.ProposalBasicInfo.VoteSchemeId;
        var voteScheme = State.VoteContract.GetVoteScheme.Call(voteSchemeId);
        if (governanceScheme?.GovernanceMechanism != GovernanceMechanism.HighCouncil &&
            voteScheme?.VoteMechanism != VoteMechanism.UniqueVote)
        {
            var enoughVote = rejectVote.Add(abstainVote).Add(approveVote) >= threshold.MinimalVoteThreshold;
            if (!enoughVote)
            {
                return new ProposalStatusOutput
                {
                    ProposalStatus = ProposalStatus.BelowThreshold,
                    ProposalStage = ProposalStage.Finished
                };
            }
        }

        var isReject = (rejectVote / totalVote) * GovernanceContractConstants.AbstractVoteTotal >
                       threshold.MaximalRejectionThreshold;
        if (isReject)
        {
            return new ProposalStatusOutput
            {
                ProposalStatus = ProposalStatus.Rejected,
                ProposalStage = ProposalStage.Finished
            };
        }

        var isAbstained = abstainVote / totalVote * GovernanceContractConstants.AbstractVoteTotal >
                          threshold.MaximalAbstentionThreshold;
        if (isAbstained)
        {
            return new ProposalStatusOutput
            {
                ProposalStatus = ProposalStatus.Abstained,
                ProposalStage = ProposalStage.Finished
            };
        }

        var isApproved = approveVote / totalVote * GovernanceContractConstants.AbstractVoteTotal >
                         threshold.MinimalApproveThreshold;
        if (!isApproved)
        {
            return new ProposalStatusOutput
            {
                ProposalStatus = ProposalStatus.Expired,
                ProposalStage = ProposalStage.Finished
            };
        }

        proposalStatus = ProposalStatus.Approved;
        if (HasPendingStatus(proposalInfo.ProposalId))
        {
            return new ProposalStatusOutput
            {
                ProposalStatus = proposalStatus,
                ProposalStage = ProposalStage.Pending
            };
        }

        return new ProposalStatusOutput
        {
            ProposalStatus = proposalStatus,
            ProposalStage = ProposalStage.Execute
        };
    }

    private bool HasPendingStatus(Hash proposalId)
    {
        var proposal = State.Proposals[proposalId];
        return Context.CurrentBlockTime >= proposal.ProposalTime.ActiveEndTime &&
               proposal.ProposalType == ProposalType.Governance &&
               State.GovernanceSchemeMap[proposal.ProposalBasicInfo.SchemeAddress].GovernanceMechanism ==
               GovernanceMechanism.HighCouncil;
    }

    public override Empty SetProposalTimePeriod(SetProposalTimePeriodInput input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        AssertParams(input, input?.DaoId, input?.ProposalTimePeriod);
        var activeTimePeriod = input!.ProposalTimePeriod.ActiveTimePeriod;
        var vetoActiveTimePeriod = input.ProposalTimePeriod.VetoActiveTimePeriod;
        var pendingTimePeriod = input.ProposalTimePeriod.PendingTimePeriod;
        var executeTimePeriod = input.ProposalTimePeriod.ExecuteTimePeriod;
        var vetoExecuteTimePeriod = input.ProposalTimePeriod.VetoExecuteTimePeriod;
        //TODO 
        // AssertNumberInRange(activeTimePeriod, GovernanceContractConstants.MinActiveTimePeriod,
        //     GovernanceContractConstants.MaxActiveTimePeriod, "ActiveTimePeriod");
        // AssertNumberInRange(vetoActiveTimePeriod, GovernanceContractConstants.MinVetoActiveTimePeriod,
        //     GovernanceContractConstants.MaxVetoActiveTimePeriod, "VetoActiveTimePeriod");
        // AssertNumberInRange(pendingTimePeriod, GovernanceContractConstants.MinPendingTimePeriod,
        //     GovernanceContractConstants.MaxPendingTimePeriod, "PendingTimePeriod");
        // AssertNumberInRange(executeTimePeriod, GovernanceContractConstants.MinExecuteTimePeriod,
        //     GovernanceContractConstants.MaxExecuteTimePeriod, "ExecuteTimePeriod");
        // AssertNumberInRange(vetoExecuteTimePeriod, GovernanceContractConstants.MinVetoExecuteTimePeriod,
        //     GovernanceContractConstants.MaxVetoExecuteTimePeriod, "VetoExecuteTimePeriod");

        var timePeriod = State.DaoProposalTimePeriods[input.DaoId];
        if (timePeriod == null)
        {
            timePeriod = new DaoProposalTimePeriod();
        }

        timePeriod.ActiveTimePeriod = activeTimePeriod;
        timePeriod.VetoActiveTimePeriod = vetoActiveTimePeriod;
        timePeriod.PendingTimePeriod = pendingTimePeriod;
        timePeriod.ExecuteTimePeriod = executeTimePeriod;
        timePeriod.VetoExecuteTimePeriod = vetoExecuteTimePeriod;

        State.DaoProposalTimePeriods[input.DaoId] = timePeriod;

        Context.Fire(new DaoProposalTimePeriodSet
        {
            DaoId = input.DaoId,
            ActiveTimePeriod = activeTimePeriod,
            VetoActiveTimePeriod = vetoActiveTimePeriod,
            PendingTimePeriod = pendingTimePeriod,
            ExecuteTimePeriod = executeTimePeriod,
            VetoExecuteTimePeriod = vetoExecuteTimePeriod
        });
        return new Empty();
    }

    public override ProposalInfoOutput GetProposalInfo(Hash input)
    {
        var proposal = State.Proposals[input];
        if (proposal == null)
        {
            return new ProposalInfoOutput();
        }

        var voteResult = State.VoteContract.GetVotingResult.Call(input);

        var proposalInfoOutput = new ProposalInfoOutput
        {
            DaoId = proposal.ProposalBasicInfo?.DaoId,
            ProposalId = proposal.ProposalId,
            ProposalTitle = proposal.ProposalBasicInfo?.ProposalTitle,
            ProposalDescription = proposal.ProposalBasicInfo?.ProposalDescription,
            ForumUrl = proposal.ForumUrl,
            ProposalType = proposal.ProposalType,
            ActiveStartTime = proposal.ProposalTime?.ActiveStartTime,
            ActiveEndTime = proposal.ProposalTime?.ActiveEndTime,
            ExecuteStartTime = proposal.ProposalTime?.ExecuteStartTime,
            ExecuteEndTime = proposal.ProposalTime?.ExecuteEndTime,
            ProposalStatus = proposal.ProposalStatus,
            ProposalStage = proposal.ProposalStage,
            Proposer = proposal.Proposer,
            SchemeAddress = proposal.ProposalBasicInfo?.SchemeAddress,
            Transaction = proposal.Transaction,
            VoteSchemeId = proposal.ProposalBasicInfo?.VoteSchemeId,
            VetoProposalId = proposal.VetoProposalId,
            VotersCount = voteResult.VotesAmount,
            VoteCount = voteResult.TotalVotersCount,
            ApprovalCount = voteResult.ApproveCounts,
            RejectionCount = voteResult.RejectCounts,
            AbstentionCount = voteResult.AbstainCounts
        };
        return proposalInfoOutput;
    }

    public override DaoProposalTimePeriod GetDaoProposalTimePeriod(Hash input)
    {
        var timePeriod = State.DaoProposalTimePeriods[input];
        return timePeriod ?? new DaoProposalTimePeriod
        {
            ActiveTimePeriod = GovernanceContractConstants.MinActiveTimePeriod,
            VetoActiveTimePeriod = GovernanceContractConstants.MinVetoActiveTimePeriod,
            PendingTimePeriod = GovernanceContractConstants.MinPendingTimePeriod,
            ExecuteTimePeriod = GovernanceContractConstants.MinExecuteTimePeriod,
            VetoExecuteTimePeriod = GovernanceContractConstants.MinVetoExecuteTimePeriod
        };
    }

    public override GovernanceSchemeThreshold GetProposalSnapShotScheme(Hash input)
    {
        if (input == null || input == Hash.Empty)
        {
            return new GovernanceSchemeThreshold();
        }

        return State.ProposalGovernanceSchemeSnapShot[input];
    }

    public override Empty ClearProposal(Hash input)
    {
        return new Empty();
    }
}