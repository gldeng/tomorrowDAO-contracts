using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Types;
using TomorrowDAO.Contracts.DAO;

namespace TomorrowDAO.Contracts.Vote
{
    // The state class is access the blockchain state
    public partial class VoteContractState : ContractState 
    {
        public SingletonState<bool> Initialized { get; set; }
        // vote_scheme_id -> VoteScheme
        public MappedState<Hash, VoteScheme> VoteSchemes { get; set; }
        // voting_item_id(proposal id) -> VotingItem
        public MappedState<Hash, VotingItem> VotingItems { get; set; }
        // voting_item_id(proposal id) -> voter -> VotingRecord
        public MappedState<Hash, Address, VotingRecord> VotingRecords { get; set; }
        // voting_item_id -> VotingResult
        public MappedState<Hash, VotingResult> VotingResults { get; set; }
        // address -> dao_id -> amount
        public MappedState<Address, Hash, long> RemainVoteAmounts { get; set; }
        // dao_id -> EmergencyStatus
        public MappedState<Hash, bool> EmergencyStatusMap { get; set; }
    }
}