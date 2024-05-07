using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractBaseTests : TestBase
{
    protected readonly Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");
    protected readonly string DefaultGovernanceToken = "ELF";
    protected readonly Hash DefaultVoteSchemeId = HashHelper.ComputeFrom("DefaultVoteSchemeId");
    
    
    protected async Task Initialize(Address daoAddress = null, Address voteAddress = null,
        Address governanceAddress = null)
    {
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress??VoteContractAddress,
            GovernanceContractAddress = governanceAddress??GovernanceContractAddress
        };
        await ElectionContractStub.Initialize.SendAsync(input);
    }
}