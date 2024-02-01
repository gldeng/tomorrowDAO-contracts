using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Election
{
    // The state class is access the blockchain state
    public class ElectionContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}