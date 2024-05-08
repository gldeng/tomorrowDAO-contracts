using System.Threading.Tasks;
using AElf;
using AElf.Types;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public class VoteContractTestBase : TestBase
{
    protected Hash ProposalId = HashHelper.ComputeFrom("ProposalId");
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected string TokenElf = "ELF";
    
    public async Task InitializeAll()
    {
        //init governance contrct
        await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress
        });
        
        //init dao contract
        await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });

        // UniqueVoteVoteSchemeId = await InitializeVoteScheme();
        // DefaultDaoId= await InitializeDao();
    }
    
    private async Task<Hash> InitializeVoteScheme(string voteMechanismString)
    {
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.UniqueVote
        });

        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(VoteContractAddress),
            HashHelper.ComputeFrom(voteMechanismString));
    }
}