
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContract : ElectionContractContainer.ElectionContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        return base.Initialize(input);
    }

    public override Empty RegisterElectionVotingEvent(RegisterElectionVotingEventInput input)
    {
        return base.RegisterElectionVotingEvent(input);
    }

    public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
    {
        return base.TakeSnapshot(input);
    }

    public override Empty AnnounceElection(AnnounceElectionInput input)
    {
        return base.AnnounceElection(input);
    }

    public override Empty AnnounceElectionFor(AnnounceElectionForInput input)
    {
        return base.AnnounceElectionFor(input);
    }

    public override Empty QuitElection(QuitElectionInput input)
    {
        return base.QuitElection(input);
    }

    public override Hash Vote(VoteHighCouncilInput input)
    {
        return base.Vote(input);
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