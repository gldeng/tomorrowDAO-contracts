using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Empty RequestTransfer(RequestTransferInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Symbol, "Symbol");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        AssertNotNullOrEmpty(input.Recipient, "Recipient");
        AssertNotNullOrEmpty(input.ProposalInfo, "ProposalInfo");
        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);

        //lock token
        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] ?? new FundInfo();
        Assert(fundInfo.AvailableFunds >= input.Amount, "The Treasury has insufficient available funds.");
        fundInfo.AvailableFunds -= input.Amount;
        fundInfo.LockedFunds += input.Amount;
        State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] = fundInfo;

        var lockInfo = CreateProposal(treasuryInfo, input);

        //update total funds
        var totalFundInfo = State.TotalFundInfoMap[input.Symbol] ?? new FundInfo();
        Assert(totalFundInfo.AvailableFunds >= input.Amount,
            "The Treasury has insufficient available funds(total).");
        totalFundInfo!.AvailableFunds -= input.Amount;
        totalFundInfo.LockedFunds += input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;

        Context.Fire(new TreasuryTokenLocked
        {
            LockInfo = lockInfo,
            Proposer = Context.Sender
        });
        return new Empty();
    }

    public override Empty ReleaseTransfer(ReleaseTransferInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Symbol, "Symbol");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        AssertNotNullOrEmpty(input.Recipient, "Recipient");

        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);
        var lockInfo = State.LockInfoMap[input.LockId];
        Assert(lockInfo != null && lockInfo.Symbol == input.Symbol && lockInfo.Amount == input.Amount,
            "The input information is inconsistent with the locked token information");

        var referendumAddress = State.DaoContract.GetReferendumAddress.Call(input.DaoId);
        var highCouncilAddress = State.DaoContract.GetHighCouncilAddress.Call(input.DaoId);
        Assert(Context.Sender == referendumAddress || Context.Sender == highCouncilAddress, "No permission.");

        TransferFromTreasury(input);

        State.ProposalLockMap.Remove(lockInfo!.ProposalId);
        State.LockInfoMap.Remove(input.LockId);

        //update funds
        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol];
        Assert(fundInfo != null, "FundInfo not exist.");
        fundInfo!.LockedFunds -= input.Amount;
        State.FundInfoMap[treasuryInfo.TreasuryAddress][input.Symbol] = fundInfo;

        var totalFundInfo = State.TotalFundInfoMap[input.Symbol];
        Assert(totalFundInfo != null, "TotalFundInfo not exist.");
        totalFundInfo!.LockedFunds -= input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;

        Context.Fire(new TreasuryTransferReleased
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Recipient = input.Recipient,
            Memo = input.Memo,
            Executor = Context.Sender,
            ProposalId = lockInfo.ProposalId,
            LockId = input.LockId
        });
        return new Empty();
    }

    public override Empty UnlockToken(Hash input)
    {
        AssertNotNullOrEmpty(input);

        var lockId = State.ProposalLockMap[input];
        Assert(lockId != null && lockId != Hash.Empty, "Locked token information not exist.");
        
        var lockInfo = State.LockInfoMap[lockId];
        Assert(lockInfo != null, "Locked token information not exist.");

        var proposalInfo = State.GovernanceContract.GetProposalInfo.Call(input);
        Assert(proposalInfo != null, $"Proposal {input} not exist.");
        Assert(Context.CurrentBlockTime > proposalInfo.ExecuteEndTime, "The token is in a locked period.");

        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(lockInfo!.DaoId);

        //update funds
        var fundInfo = State.FundInfoMap[treasuryInfo.TreasuryAddress][lockInfo.Symbol];
        Assert(fundInfo != null, "FundInfo not exist.");
        fundInfo!.AvailableFunds += lockInfo.Amount;
        fundInfo!.LockedFunds -= lockInfo.Amount;
        State.FundInfoMap[treasuryInfo.TreasuryAddress][lockInfo.Symbol] = fundInfo;

        var totalFundInfo = State.TotalFundInfoMap[lockInfo.Symbol];
        Assert(totalFundInfo != null, "TotalFundInfo not exist.");
        totalFundInfo!.LockedFunds -= lockInfo.Amount;
        totalFundInfo!.AvailableFunds += lockInfo.Amount;
        State.TotalFundInfoMap[lockInfo.Symbol] = totalFundInfo;
        
        State.LockInfoMap.Remove(lockId);
        State.ProposalLockMap.Remove(input);

        Context.Fire(new TreasuryTokenUnlocked
        {
            LockInfo = lockInfo,
            Executor = Context.Sender
        });
        return new Empty();
    }
    
    public override Empty TransferInEmergency(TransferInEmergencyInput input)
    {
        return base.TransferInEmergency(input);
    }
    
    private LockInfo CreateProposal(TreasuryInfo treasuryInfo, RequestTransferInput input)
    {
        var lockId = GenerateLockId(input, Context.TransactionId);
        var lockInfo = State.LockInfoMap[lockId];
        Assert(lockInfo == null, "The token lock information already exists.");

        var createProposalInput = new CreateProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = input.DaoId,
                ProposalTitle = input.ProposalInfo.ProposalTitle,
                ProposalDescription = input.ProposalInfo.ProposalDescription,
                ForumUrl = input.ProposalInfo.ForumUrl,
                SchemeAddress = input.ProposalInfo.SchemeAddress,
                VoteSchemeId = input.ProposalInfo.VoteSchemeId
            },
            ProposalType = 1,
            Transaction = new ExecuteTransaction
            {
                ContractMethodName = nameof(ReleaseTransfer),
                ToAddress = Context.Self,
                Params = new ReleaseTransferInput
                {
                    DaoId = input.DaoId,
                    Amount = input.Amount,
                    Symbol = input.Symbol,
                    Recipient = input.Recipient,
                    Memo = "transfer treasury funds",
                    LockId = lockId
                }.ToByteString()
            },
            Token = Context.TransactionId
        };
        State.GovernanceContract.CreateProposal.Send(createProposalInput);

        var proposalId = GenerateLockId(createProposalInput, Context.TransactionId, State.GovernanceContract.Value);

        lockInfo = new LockInfo();
        lockInfo.DaoId = input.DaoId;
        lockInfo.TreasuryAddress = treasuryInfo.TreasuryAddress;
        lockInfo.Symbol = input.Symbol;
        lockInfo.Amount = input.Amount;
        lockInfo.ProposalId = proposalId;
        lockInfo.LockId = lockId;
        State.LockInfoMap[lockId] = lockInfo;
        State.ProposalLockMap[proposalId] = lockId;

        return lockInfo;
    }

    private void TransferFromTreasury(ReleaseTransferInput input)
    {
        var treasuryHash = GenerateTreasuryHash(input.DaoId, Context.Self);
        State.TokenContract.Transfer.VirtualSend(treasuryHash, new TransferInput
        {
            To = input.Recipient,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = "transfer from treasury"
        });
    }
}