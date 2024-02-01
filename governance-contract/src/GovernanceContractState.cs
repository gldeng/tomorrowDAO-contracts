using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Governance
{
    // The state class is access the blockchain state
    public class GovernanceContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}