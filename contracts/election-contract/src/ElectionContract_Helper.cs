using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Types;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    private void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Contract not initialized.");
    }
    
    private void AssertSenderDaoContract()
    {
        Assert(State.DaoContract.Value == Context.Sender, "No permission.");
    }

    private void AssertSenderDaoOrGovernanceContract()
    {
        Assert(State.DaoContract.Value == Context.Sender || State.GovernanceContract.Value == Context.Sender,
            "No permission.");
    }

    private void AssertNotNullOrEmpty(object input, string message = null)
    {
        message = string.IsNullOrWhiteSpace(message) ? "Invalid input." : $"{message} cannot be null or empty.";
        Assert(input != null, message);
        switch (input)
        {
            case Address address:
                Assert(address.Value.Any(), message);
                break;
            case Hash hash:
                Assert(hash != Hash.Empty, message);
                break;
            case string s:
                Assert(!string.IsNullOrEmpty(s), message);
                break;
        }
    }

    private Hash GetVotingResultHash(Hash votingItemId, long snapshotNumber)
    {
        return new VotingResult
        {
            VotingItemId = votingItemId,
            SnapshotNumber = snapshotNumber
        }.GetHash();
    }

    private TokenInfo GetTokenInfo(string symbol)
    {
        return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol
        });
    }
}