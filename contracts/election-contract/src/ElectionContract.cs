using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Election.Protobuf;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input.DaoContractAddress != null && input.DaoContractAddress.Value.Any(), "Empty dao contract address.");
        Assert(input.VoteContractAddress != null && input.VoteContractAddress.Value.Any(), "Empty dao contract address.");
        State.DaoContractAddress.Value = input.DaoContractAddress;
        State.VoteContractAddress.Value = input.VoteContractAddress;
        State.GovernanceContractAddress.Value = input.GovernanceContractAddress;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty SetHighCouncilConfig(SetHighCouncilConfigInput input)
    {
        return base.SetHighCouncilConfig(input);
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
    
     public override Empty RegisterElectionVotingEvent(RegisterElectionVotingEventInput input)
    {
        AssertInitialized();
        AssertSenderDaoContract();

        Assert(input.DaoId.Value.Any(), "Empty dao id");
        Assert(input.MaxCandidateCount >= ElectionContractConstants.MinCandidateCount, "Invalid max candidate count");
        Assert(input.MaxHighCouncilMemberCount >= ElectionContractConstants.MinCandidateMemberCount,
            "Invalid max high council member count");
        Assert(input.ElectionPeriod >= ElectionContractConstants.MinElectionPeriodSeconds, "Invalid ElectionPeriod");

        var governanceTokenInfo = GetTokenInfo(input.GovernanceToken);
        Assert(governanceTokenInfo != null && governanceTokenInfo.Symbol.Length > 0, "Invalid governanceToken");
        Assert(State.HighCouncilConfig[input.DaoId] == null, "Already registered");
        
        var highCouncilConfig = new HighCouncilConfig
        {
            ElectionPeriod = input.ElectionPeriod,
            MaxHighCouncilCandidateCount = input.MaxCandidateCount,
            MaxHighCouncilMemberCount = input.MaxHighCouncilMemberCount,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = input.GovernanceToken,
        };

        State.HighCouncilConfig[input.DaoId] = highCouncilConfig;
        State.VotingEventEnabledStatus[input.DaoId] = true;
        State.CurrentTermNumber[input.DaoId] = 1;

        Context.Fire(new ElectionVotingEventRegistered
        {
            DaoId = input.DaoId,
            ElectionPeriod = input.ElectionPeriod,
            CandidateCount = input.MaxCandidateCount,
            HighCouncilMemberCount = input.MaxHighCouncilMemberCount,
            GovernanceToken = input.GovernanceToken
        });

        return new Empty();
    }

    
    public override Empty AnnounceElection(AnnounceElectionInput input)
    {
        Assert(input.CandidateAdmin.Value.Any(), "Admin is needed while announcing election.");
        Assert(input.DaoId.Value.Any(), "Dao id empty");
        Assert(State.HighCouncilConfig[input.DaoId] != null, "Dao not exists");
        Assert(State.ManagedCandidateMap[input.DaoId][Context.Sender] == null, "Candidate cannot be others' admin.");
        
        AnnounceElection(input.DaoId, Context.Sender);
        
        var managedAddresses = State.ManagedCandidateMap[input.DaoId][input.CandidateAdmin] ?? new AddressList();
        if (!managedAddresses.Value.Contains(Context.Sender))
        {
            managedAddresses.Value.Add(Context.Sender);
            State.ManagedCandidateMap[input.DaoId][input.CandidateAdmin] = managedAddresses;
        }
        
        LockCandidateAnnounceToken(input.DaoId);
        
        return new Empty();
    }
    

    public override Empty AnnounceElectionFor(AnnounceElectionForInput input)
    {
        Assert(input.CandidateAdmin.Value.Any(), "Admin is needed while announcing election.");
        Assert(input.Candidate.Value.Any(), "For candidate address is needed.");
        Assert(input.DaoId.Value.Any(), "Dao id empty");
        Assert(State.HighCouncilConfig[input.DaoId] != null, "Dao not exists");
        Assert(State.ManagedCandidateMap[input.DaoId][input.Candidate] == null, "Candidate cannot be others' admin.");

        AnnounceElection(input.DaoId, input.Candidate);

        var managedAddresses = State.ManagedCandidateMap[input.DaoId][input.CandidateAdmin] ?? new AddressList();
        if (!managedAddresses.Value.Contains(Context.Sender))
        {
            managedAddresses.Value.Add(input.Candidate);
            State.ManagedCandidateMap[input.DaoId][input.CandidateAdmin] = managedAddresses;
        }
        
        LockCandidateAnnounceToken(input.DaoId);
        
        State.CandidateSponsorMap[input.DaoId][input.Candidate] = input.CandidateAdmin;
        return new Empty();
    }

    private void AnnounceElection(Hash daoId, Address candidateAddress)
    {
        var highCouncil = State.HighCouncilConfig[daoId];
        var candidateInformation = State.CandidateInformationMap[daoId][candidateAddress];

        if (candidateInformation != null)
        {
            Assert(!candidateInformation.IsCurrentCandidate,
                $"This address already announced election. {candidateAddress}");
            candidateInformation.AnnouncementTransactionId = Context.OriginTransactionId;
            candidateInformation.IsCurrentCandidate = true;
            // In this way we can keep history of current candidate, like terms, missed time slots, etc.
            State.CandidateInformationMap[daoId][candidateAddress] = candidateInformation;
        }
        else
        {
            Assert(!IsAddressBanned(daoId, candidateAddress), "This candidate already banned before.");
            State.CandidateInformationMap[daoId][candidateAddress] = new CandidateInformation
            {
                Address = candidateAddress, 
                AnnouncementTransactionId = Context.OriginTransactionId,
                IsCurrentCandidate = true
            };
        }
        
        Context.Fire(new CandidateAdded
        {
            DaoId = daoId,
            Candidate = candidateAddress,
            Amount = highCouncil.LockTokenForElection
        });

        State.Candidates[daoId].Value.Add(candidateAddress);
    }

    private void LockCandidateAnnounceToken(Hash daoId)
    {
        var highCouncilConfig = State.HighCouncilConfig[daoId];
        var lockId = Context.OriginTransactionId;
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        var sponsorAddress = Context.Sender;
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = sponsorAddress,
            To = lockVirtualAddress,
            Symbol = Context.Variables.NativeSymbol,
            Amount = highCouncilConfig.LockTokenForElection,
            Memo = $"Lock for dao announcing election, {daoId.ToHex()}."
        });
    }

    private bool IsAddressBanned(Hash daoId, Address address)
    {
        return State.BannedAddressMap[daoId][address];
    }


    public override Empty QuitElection(QuitElectionInput input)
    {
        var hCouncilConfig = State.HighCouncilConfig[input.DaoId];
        QuitElection(input.DaoId, input.Candidate);

        var managedCandidates = State.ManagedCandidateMap[input.DaoId][Context.Sender];
        Assert(managedCandidates.Value.Contains(input.Candidate), "Only admin can quit election.");
        
        var candidateInformation = State.CandidateInformationMap[input.DaoId][input.Candidate];
        
        // Unlock candidate's native token.
        var lockId = candidateInformation.AnnouncementTransactionId;
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = lockVirtualAddress,
            To = State.CandidateSponsorMap[input.DaoId][input.Candidate] ?? input.Candidate,
            Symbol = Context.Variables.NativeSymbol,
            Amount = hCouncilConfig.LockTokenForElection,
            Memo = $"Quit election, {input.DaoId}."
        });
        
        // Update candidate information.
        candidateInformation.IsCurrentCandidate = false;
        candidateInformation.AnnouncementTransactionId = Hash.Empty;
        State.CandidateInformationMap[input.DaoId][input.Candidate] = candidateInformation;

        managedCandidates.Value.Remove(input.Candidate);
        if (managedCandidates.Value.Any())
            State.ManagedCandidateMap[input.DaoId][Context.Sender] = managedCandidates;
        else
            State.ManagedCandidateMap[input.DaoId].Remove(Context.Sender);

        State.CandidateSponsorMap[input.DaoId].Remove(input.Candidate);
        return new Empty();
    }


    private void QuitElection(Hash daoId, Address address)
    {
        Assert(State.Candidates[daoId].Value.Contains(address), "Target is not a candidate.");
        
        // TODO assert not in-term
        State.Candidates[daoId].Value.Remove(address);
        
    }
    
    public override Hash Vote(VoteHighCouncilInput input)
    {
        Assert(input.DaoId.Value.Any(), "Dao id required");
        Assert(input.CandidateAddress.Value.Any(), "Candidate address required");
        
        
        
        
        return base.Vote(input);
    }
}