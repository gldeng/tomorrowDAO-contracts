using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract : GovernanceContractContainer.GovernanceContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        //Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        State.AEDPoSContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertParams(input.DaoContractAddress);
        State.DaoContract.Value = input.DaoContractAddress;
        State.VoteContract.Value = input.VoteContractAddress;
        State.Initialized.Value = true;
        return new Empty();
    }
}