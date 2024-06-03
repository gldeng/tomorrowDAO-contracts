using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

// Contract class must inherit the base class generated from the proto file
public partial class TreasuryContract : TreasuryContractContainer.TreasuryContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoContractAddress, "DaoContract");
        AssertNotNullOrEmpty(input.GovernanceContractAddress, "GovernanceContract");
        State.DaoContract.Value = input.DaoContractAddress;
        State.GovernanceContract.Value = input.GovernanceContractAddress;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty Pause(Hash input)
    {
        Assert(!State.IsPaused.Value, "Treasury has bean paused.");

        var treasuryInfo = State.TreasuryInfoMap[input];
        Assert(treasuryInfo != null, "Treasury has not bean created yet.");
        
        var daoTreasuryPaused = State.TreasuryPausedMap[treasuryInfo!.TreasuryAddress];
        Assert(!daoTreasuryPaused, "Treasury has bean paused.");
        
        var daoInfo = State.DaoContract.GetDAOInfo.Call(input);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender || daoInfo.Creator == Context.Sender, "No permission.");

        var pausedAll = false;
        if (daoInfo.Creator == Context.Sender)
        {
            State.TreasuryPausedMap[treasuryInfo.TreasuryAddress] = true;
        }
        else if (contractInfo.Deployer == Context.Sender)
        {
            State.IsPaused.Value = true;
            pausedAll = true;
        }

        Context.Fire(new Paused
        {
            Account = Context.Sender,
            DaoId = input,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            PausedAll = pausedAll
        });
        return new Empty();
    }

    public override Empty Unpause(Hash input)
    {
        var treasuryInfo = State.TreasuryInfoMap[input];
        Assert(treasuryInfo != null, "Treasury has not bean created yet.");
        
        var daoTreasuryPaused = State.TreasuryPausedMap[treasuryInfo!.TreasuryAddress];
        Assert(State.IsPaused.Value || daoTreasuryPaused, "Treasury is not paused.");
        
        var daoInfo = State.DaoContract.GetDAOInfo.Call(input);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender || daoInfo.Creator == Context.Sender, "No permission.");

        var unpausedAll = false;
        if (daoInfo.Creator == Context.Sender)
        {
            State.TreasuryPausedMap.Remove(treasuryInfo.TreasuryAddress);
        }
        else if (contractInfo.Deployer == Context.Sender)
        {
            State.IsPaused.Value = false;
            unpausedAll = true;
        }

        Context.Fire(new Unpaused
        {
            Account = Context.Sender,
            DaoId = input,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            UnpausedAll = unpausedAll
        });
        return new Empty();
    }
}