using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Election.Protobuf;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    
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

}