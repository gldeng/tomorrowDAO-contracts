using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Treasury
{
    // The state class is access the blockchain state
    public class TreasuryContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}