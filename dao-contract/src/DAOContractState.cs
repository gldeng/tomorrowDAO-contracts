using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.DAO
{
    // The state class is access the blockchain state
    public class DAOContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}