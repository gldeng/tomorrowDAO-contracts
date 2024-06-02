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
        
        var daoTreasuryPaused = State.DaoTreasuryPaused[treasuryInfo!.TreasuryAddress];
        Assert(!daoTreasuryPaused, "Treasury has bean paused.");
        
        var daoInfo = State.DaoContract.GetDAOInfo.Call(input);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender || daoInfo.Creator == Context.Sender, "No permission.");

        var pausedAll = false;
        if (daoInfo.Creator == Context.Sender)
        {
            State.DaoTreasuryPaused[treasuryInfo.TreasuryAddress] = true;
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
        
        var daoTreasuryPaused = State.DaoTreasuryPaused[treasuryInfo!.TreasuryAddress];
        Assert(State.IsPaused.Value || daoTreasuryPaused, "Treasury is not paused.");
        
        var daoInfo = State.DaoContract.GetDAOInfo.Call(input);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender || daoInfo.Creator == Context.Sender, "No permission.");

        var unpausedAll = false;
        if (daoInfo.Creator == Context.Sender)
        {
            State.DaoTreasuryPaused.Remove(treasuryInfo.TreasuryAddress);
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


    public override Empty StakeToken(StakeTokenInput input)
    {
        return base.StakeToken(input);
    }

    public override Empty RequestTransfer(RequestTransferInput input)
    {
        return base.RequestTransfer(input);
    }

    public override Empty ReleaseTransfer(ReleaseTransferInput input)
    {
        return base.ReleaseTransfer(input);
    }

    public override Empty UnlockToken(Hash input)
    {
        return base.UnlockToken(input);
    }

    public override Empty TransferInEmergency(TransferInEmergencyInput input)
    {
        return base.TransferInEmergency(input);
    }

    public override FundInfo GetFundInfo(GetFundInfoInput input)
    {
        return base.GetFundInfo(input);
    }

    public override FundInfo GetTotoalFundInfo(GetTotoalFundInfoInput input)
    {
        return base.GetTotoalFundInfo(input);
    }

    public override LockInfo GetLockInfo(Hash input)
    {
        return base.GetLockInfo(input);
    }
}