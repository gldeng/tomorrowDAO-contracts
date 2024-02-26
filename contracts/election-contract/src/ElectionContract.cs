using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input.DaoContractAddress.Value.Any(), "Empty dao contract address.");
        Assert(input.VoteContractAddress.Value.Any(), "Empty dao contract address.");
        State.Initialized.Value = true;
        State.DaoContractAddress.Value = input.DaoContractAddress;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        return new Empty();
    }

    public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
    {
        AssertInitialized();
        AssertSenderDaoContract();
        Assert(input.TermNumber == State.CurrentTermNumber[input.DaoId], "Invalid term_number");
        
        SavePreviousTermInformation(input);
        
        State.CurrentTermNumber[input.DaoId] = input.TermNumber.Add(1);
        
        //TODO WIP
        
        return base.TakeSnapshot(input);
    }

    private void SavePreviousTermInformation(TakeElectionSnapshotInput input)
    {
        var snapshot = new TermSnapshot
        {
            DaoId = input.DaoId,
            TermNumber = input.TermNumber,
        };
        if (State.Candidates[input.DaoId] == null) return;

        foreach (var candidate in State.Candidates[input.DaoId].Value)
        {
            var votes = State.CandidateVotes[input.DaoId][candidate];
            var validObtainedVotesAmount = 0L;
            if (votes != null) validObtainedVotesAmount = votes.ObtainedActiveVotedVotesAmount;
            snapshot.ElectionResult.Add(candidate.ToBase58(), validObtainedVotesAmount);
        }
        State.Snapshots[input.DaoId][input.TermNumber] = snapshot;
        
    }



    public override Empty ChangeVotingOption(ChangeVotingOptionInput input)
    {
        return base.ChangeVotingOption(input);
    }

    public override Empty Withdraw(Hash input)
    {
        return base.Withdraw(input);
    }

    public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
    {
        return base.UpdateCandidateInformation(input);
    }

    public override Empty UpdateMultipleCandidateInformation(UpdateMultipleCandidateInformationInput input)
    {
        return base.UpdateMultipleCandidateInformation(input);
    }

    public override Empty ReplaceCandidateAddress(ReplaceCandidatePubkeyInput input)
    {
        return base.ReplaceCandidateAddress(input);
    }

    public override Empty SetCandidateAdmin(SetCandidateAdminInput input)
    {
        return base.SetCandidateAdmin(input);
    }

    public override Empty RemoveEvilNode(RemoveEvilNodeInput input)
    {
        return base.RemoveEvilNode(input);
    }

    public override Empty EnableElection(Hash input)
    {
        return base.EnableElection(input);
    }

    public override Empty SetEmergency(SetEmergencyInput input)
    {
        return base.SetEmergency(input);
    }

    public override Empty SetHighCouncilConfig(SetHighCouncilConfigInput input)
    {
        return base.SetHighCouncilConfig(input);
    }

    public override AddressList GetVotedCandidates(Hash input)
    {
        return base.GetVotedCandidates(input);
    }

    public override AddressList GetVictories(Hash input)
    {
        return base.GetVictories(input);
    }

    public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
    {
        return base.GetTermSnapshot(input);
    }

    public override ElectionResult GetElectionResult(GetElectionResultInput input)
    {
        return base.GetElectionResult(input);
    }

    public override ElectorVote GetElectorVote(Address input)
    {
        return base.GetElectorVote(input);
    }

    public override CandidateVote GetCandidateVote(GetCandidateVoteInput input)
    {
        return base.GetCandidateVote(input);
    }

    public override GetPageableCandidateInformationOutput GetPageableCandidateInformation(PageInformation input)
    {
        return base.GetPageableCandidateInformation(input);
    }

    public override DataCenterRankingList GetDataCenterRankingList(Hash input)
    {
        return base.GetDataCenterRankingList(input);
    }

    public override Address GetEmergency(Hash input)
    {
        return base.GetEmergency(input);
    }

    public override HighCouncilConfig GetHighCouncilConfig(Hash input)
    {
        return base.GetHighCouncilConfig(input);
    }
}