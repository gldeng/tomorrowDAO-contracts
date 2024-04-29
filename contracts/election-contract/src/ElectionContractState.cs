using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

// The state class is access the blockchain state
public partial class ElectionContractState : ContractState 
{
        
    public BoolState Initialized { get; set; }
    
    public SingletonState<Address> DaoContractAddress { get; set; }
    public SingletonState<Address> VoteContractAddress { get; set; }
        
    // dao id -> bool
    public MappedState<Hash, bool> VotingEventEnabledStatus { get; set; }
    
    // dao id -> HighCouncilConfig
    public MappedState<Hash, HighCouncilConfig> HighCouncilConfig { get; set; }
    
    // dao id -> banned address
    public MappedState<Hash, Address, bool> BannedAddressMap { get; set; }
    
    // dao id -> candidate address -> Candidate Information
    public MappedState<Hash, Address, CandidateInformation> CandidateInformationMap { get; set; }
    
    // dao id -> candidate address -> Sponsor (who will pay announce election fee for this candidate)
    public MappedState<Hash, Address, Address> CandidateSponsorMap { get; set; }
    
    // dao id -> candidates
    public MappedState<Hash, AddressList> Candidates { get; set; }
    
    // dao id -> candidate admin map -> candidates
    public MappedState<Hash, Address, AddressList> ManagedCandidateMap { get; set; }
    
    // dao -> Address-> Voter's Voting Information
    public MappedState<Hash, Address, ElectorVote> ElectorVotes { get; set; }

    // dao -> Address-> Voting information of candidates
    public MappedState<Hash, Address, CandidateVote> CandidateVotes { get; set; }
    
    // dao -> termId -> snapshot
    public MappedState<Hash, long, TermSnapshot> Snapshots { get; set; }

    // dao -> Current term
    public MappedState<Hash, long> CurrentTermNumber { get; set; }

    // dao -> Whether HC voting is turned on
    public MappedState<Hash, bool> ElectionEnabledStatus { get; set; }
    
    // dao id -> Time unit: seconds
    public MappedState<Hash, long> MinimumLockTime { get; set; }

    // dao id -> Time unit: seconds
    public MappedState<Hash, long> MaximumLockTime { get; set; }
    
}