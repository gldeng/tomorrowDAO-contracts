using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
{
    
    public override Hash Vote(VoteHighCouncilInput input)
    {
        Assert(input.DaoId.Value.Any(), "Dao id required");
        Assert(input.CandidateAddress.Value.Any(), "Candidate address required");
        
        
        
        
        return base.Vote(input);
    }
    
}