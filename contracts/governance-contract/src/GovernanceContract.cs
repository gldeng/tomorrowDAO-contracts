using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract : GovernanceContractContainer.GovernanceContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertParams(input.DaoContractAddress);
        State.DaoContract.Value = input.DaoContractAddress;
        State.Initialized.Value = true;
        return new Empty();
    }
}