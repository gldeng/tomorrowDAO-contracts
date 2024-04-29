using AElf.Contracts.MultiToken;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{


    internal void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Contract not initialized.");
    }


    internal void AssertSenderDaoContract()
    {
        Assert(State.DaoContractAddress.Value == Context.Sender, "No permission.");
    }


    internal TokenInfo GetTokenInfo(string symbol)
    {
        return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol
        });
    }


}