using System.Threading.Tasks;
using AElf;
using AElf.Types;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public class VoteContractTestBase : TestBase
{
    protected Hash ProposalId = HashHelper.ComputeFrom("ProposalId");
    protected Hash UniqueVoteVoteSchemeId = HashHelper.ConcatAndCompute(
        HashHelper.ComputeFrom(Address.FromBase58("Ny5byXSUvKn2Ce7AS8CpPiCjwC3sbWPvkXMB27sXC4UG7aVHL")), 
        HashHelper.ComputeFrom(VoteMechanism.UniqueVote.ToString()));
    protected string TokenElf = "ELF";
}