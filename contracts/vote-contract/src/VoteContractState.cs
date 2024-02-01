using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Vote
{
    // The state class is access the blockchain state
    public class VoteContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}