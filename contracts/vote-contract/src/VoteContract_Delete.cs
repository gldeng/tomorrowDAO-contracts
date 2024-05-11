using System.Linq;
using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty Test(SetEmergencyStatusInput input)
    {
        Context.Fire(new EmergencyStatusSet
        {
            DaoId = input.DaoId,
            EmergencyStatus = input.IsEnable
        });
        
        return new Empty();
    }
}