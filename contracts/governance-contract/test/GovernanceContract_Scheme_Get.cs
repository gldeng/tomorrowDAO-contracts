using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceSchemeContractSchemeGet : GovernanceContractTestBase
{
    [Fact]
    public async Task GetGovernanceSchemeTest()
    {
        await Initialize(DefaultAddress);
        var address = await AddGovernanceScheme();
        var scheme = await GovernanceContractStub.GetGovernanceScheme.CallAsync(address);
        scheme.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDaoGovernanceSchemeAddressListTest()
    {
        await Initialize(DefaultAddress);
        await AddGovernanceScheme();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(DefaultDaoId);
        addressList.ShouldNotBeNull();
        addressList.Value.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetDaoGovernanceSchemeListTest()
    {
        await Initialize(DefaultAddress);
        await AddGovernanceScheme();
        var schemeList = await GovernanceContractStub.GetDaoGovernanceSchemeList.CallAsync(DefaultDaoId);
        schemeList.ShouldNotBeNull();
        schemeList.Value.ShouldNotBeNull();
        schemeList.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetProposalSnapShotSchemeTest()
    {
        await Initialize(DefaultAddress);
        var scheme = await AddGovernanceScheme();
        var proposal = await CreateProposal(scheme);

        var threshold = await GovernanceContractStub.GetProposalSnapShotScheme.CallAsync(proposal);
        threshold.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProposalStatusTest()
    {
        await Initialize(DefaultAddress);
        var scheme = await AddGovernanceScheme();
        var proposal = await CreateProposal(scheme);
        var proposalStatusOutput = await GovernanceContractStub.GetProposalStatus.CallAsync(proposal);
        proposalStatusOutput.ShouldNotBeNull();
    }
}