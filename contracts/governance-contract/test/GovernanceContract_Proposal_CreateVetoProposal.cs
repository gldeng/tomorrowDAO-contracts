using System;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.CSharp.Core.Extension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalCreateVetoProposal : GovernanceContractTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    // private readonly IResetBlockTimeProvider _resetBlockTime;
    private readonly ResetBlockTimeProviderProxy _resetBlockTime;

    private readonly IServiceProvider _serviceProvider;

    public GovernanceContractProposalCreateVetoProposal(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // _resetBlockTime = (ResetBlockTimeProviderProxy)resetBlockTime;
        _resetBlockTime = (ResetBlockTimeProviderProxy)GetRequiredService<IResetBlockTimeProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        BlockTimeProvider1.SetBlockTime(DateTime.UtcNow.AddDays(3).ToTimestamp());
        services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        // services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProviderProxy>();
        // context.Services.Replace(ServiceDescriptor
        //     .Singleton<IBlockTimeProvider, DelayBlockTimeProvider>());
    }

    [Fact]
    public async Task CreateVetoProposalTest()
    {
        var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();
        blockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(2));
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(2));
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false);
        var vetoProposalId = executionResult.Output;

        //_resetBlockTime.StepMilliseconds = 3600000;
        BlockTimeProvider.SetBlockTime(3600000);

        var vetoProposalInput = MockCreateVetoProposalInput();
        vetoProposalInput.VetoProposalId = vetoProposalId;
        var result = await CreateVetoProposalAsync(vetoProposalInput, false);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine(result.Output.ToString());
    }
}