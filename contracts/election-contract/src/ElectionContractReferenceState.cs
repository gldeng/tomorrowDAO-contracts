using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Election;

// The state class is access the blockchain state
public partial class ElectionContractState
{
    
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

    
}