using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract : DAOContractContainer.DAOContractBase
{
    public override Empty SetGovernanceContract(Address input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(IsAddressValid(input), "Invalid address.");
        State.GovernanceContract.Value = input;
        return new Empty();
    }
    
    public override Empty SetElectionContract(Address input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(IsAddressValid(input), "Invalid address.");
        State.ElectionContract.Value = input;
        return new Empty();
    }
    
    public override Empty SetTimelockContract(Address input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(IsAddressValid(input), "Invalid address.");
        State.TimelockContract.Value = input;
        return new Empty();
    }
    
    public override Empty SetTreasuryContract(Address input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(IsAddressValid(input), "Invalid address.");
        State.TreasuryContract.Value = input;
        return new Empty();
    }
    
    public override Empty SetVoteContract(Address input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(IsAddressValid(input), "Invalid address.");
        State.VoteContract.Value = input;
        return new Empty();
    }   
}