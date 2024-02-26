using AElf;
using AElf.Types;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsStringValid(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    private void CheckDAOExists(Hash input)
    {
        Assert(State.DAOInfoMap[input] != null, "DAO not existed.");
    }

    private void CheckDaoSubsistStatus(Hash input)
    {
        Assert(State.DAOInfoMap[input].SubsistStatus, "DAO not subsisted.");
    }

    private void CheckDAOExistsAndSubsist(Hash input)
    {
        CheckDAOExists(input);
        CheckDaoSubsistStatus(input);
    }

    private bool IsValidTokenChar(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    private Governance.GovernanceSchemeThreshold ConvertToGovernanceSchemeThreshold(GovernanceSchemeThreshold input)
    {
        return new Governance.GovernanceSchemeThreshold
        {
            MinimalVoteThreshold = input.MinimalVoteThreshold,
            MinimalRequiredThreshold = input.MinimalRequiredThreshold,
            MinimalApproveThreshold = input.MinimalApproveThreshold,
            MaximalAbstentionThreshold = input.MaximalAbstentionThreshold,
            MaximalRejectionThreshold = input.MaximalRejectionThreshold
        };
    }
}