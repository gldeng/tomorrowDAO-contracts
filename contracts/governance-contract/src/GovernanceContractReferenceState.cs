using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal DAOContractContainer.DAOContractReferenceState DaoContract { get; set; }

    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }

    internal VoteContractContainer.VoteContractReferenceState VoteContract { get; set; }
}