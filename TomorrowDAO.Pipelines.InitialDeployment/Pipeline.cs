using AElf;
using AElf.Scripts;
using AElf.Scripts.Predefined;
using Microsoft.Extensions.Logging;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Treasury;
using TomorrowDAO.Contracts.Vote;
using InitializeInput = TomorrowDAO.Contracts.Governance.InitializeInput;

namespace TomorrowDAO.Pipelines.InitialDeployment;

public class Pipeline : Script
{
    // TODO: Reference the assemblies instead of using the path
    public DeployContractScript DeployVote = new(
        "../../../../contracts/vote-contract/src/bin/Debug/net8.0/TomorrowDAO.Contracts.Vote.dll.patched"
    );

    public DeployContractScript DeployGovernance = new(
        "../../../../contracts/governance-contract/src/bin/Debug/net6.0/TomorrowDAO.Contracts.Governance.dll.patched"
    );

    public DeployContractScript DeployDao = new(
        "../../../../contracts/dao-contract/src/bin/Debug/net6.0/TomorrowDAO.Contracts.DAO.dll.patched"
    );

    public DeployContractScript DeployElection = new(
        "../../../../contracts/election-contract/src/bin/Debug/net6.0/TomorrowDAO.Contracts.Election.dll.patched"
    );

    public DeployContractScript DeployTreasury = new(
        "../../../../contracts/treasury-contract/src/bin/Debug/net6.0/TomorrowDAO.Contracts.Treasury.dll.patched"
    );

    public DeployContractScript DeployTimelock = new(
        "../../../../contracts/timelock-contract/src/bin/Debug/net6.0/TomorrowDAO.Contracts.Timelock.dll.patched"
    );


    public VoteContractContainer.VoteContractStub VoteContractStub { get; private set; }
    public GovernanceContractContainer.GovernanceContractStub GovernanceContractStub { get; private set; }
    public DAOContractContainer.DAOContractStub DaoContractStub { get; private set; }
    public ElectionContractContainer.ElectionContractStub ElectionContractStub { get; private set; }
    public TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; private set; }

    public override async Task RunAsync()
    {
        await DeployAllAsync();
        InitializeStubs();
        await InitializeGovernanceAsync();
        await InitializeDaoAsync();
        await InitializeElectionAsync();
        await InitializeVoteAsync();
    }

    private async Task DeployAllAsync()
    {
        NextSalt = HashHelper.ComputeFrom("tmrdao.vote");
        await DeployVote.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.governance");
        await DeployGovernance.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.dao");
        await DeployDao.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.election");
        await DeployElection.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.treasury");
        await DeployTreasury.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.timelock");
        await DeployTimelock.RunAsync();
        NextSalt = HashHelper.ComputeFrom("tmrdao.merkletree");

        Logger.LogInformation($"Deployed Governance at: {DeployGovernance.DeployedAddress}");
        Logger.LogInformation($"Deployed Dao        at: {DeployDao.DeployedAddress}");
        Logger.LogInformation($"Deployed Election   at: {DeployElection.DeployedAddress}");
        Logger.LogInformation($"Deployed Treasury   at: {DeployTreasury.DeployedAddress}");
        Logger.LogInformation($"Deployed Vote       at: {DeployVote.DeployedAddress}");
        Logger.LogInformation($"Deployed Timelock   at: {DeployTimelock.DeployedAddress}");
    }

    private void InitializeStubs()
    {
        Logger.LogTrace("Initializing stubs.");
        VoteContractStub = this.GetInstance<VoteContractContainer.VoteContractStub>(DeployVote.DeployedAddress!);
        GovernanceContractStub =
            this.GetInstance<GovernanceContractContainer.GovernanceContractStub>(DeployGovernance.DeployedAddress!);
        DaoContractStub = this.GetInstance<DAOContractContainer.DAOContractStub>(DeployDao.DeployedAddress!);
        ElectionContractStub =
            this.GetInstance<ElectionContractContainer.ElectionContractStub>(DeployElection.DeployedAddress!);
        TreasuryContractStub =
            this.GetInstance<TreasuryContractContainer.TreasuryContractStub>(DeployTreasury.DeployedAddress!);
        Logger.LogTrace("Initialized stubs.");
    }

    private async Task InitializeGovernanceAsync()
    {
        Logger.LogTrace("Initializing governance.");
        await GovernanceContractStub.Initialize.SendAsync(new InitializeInput()
        {
            DaoContractAddress = DeployDao.DeployedAddress,
            ElectionContractAddress = DeployElection.DeployedAddress,
            VoteContractAddress = DeployVote.DeployedAddress
        });
        Logger.LogTrace("Initialized governance.");
    }

    private async Task InitializeDaoAsync()
    {
        Logger.LogTrace("Initializing dao.");
        await DaoContractStub.Initialize.SendAsync(new TomorrowDAO.Contracts.DAO.InitializeInput()
        {
            ElectionContractAddress = DeployElection.DeployedAddress,
            GovernanceContractAddress = DeployGovernance.DeployedAddress,
            TreasuryContractAddress = DeployTreasury.DeployedAddress,
            VoteContractAddress = DeployVote.DeployedAddress,
            TimelockContractAddress = DeployTimelock.DeployedAddress
        });
        Logger.LogTrace("Initialized dao.");
    }

    private async Task InitializeElectionAsync()
    {
        Logger.LogTrace("Initializing election.");
        await ElectionContractStub.Initialize.SendAsync(new TomorrowDAO.Contracts.Election.InitializeInput()
        {
            DaoContractAddress = DeployDao.DeployedAddress,
            GovernanceContractAddress = DeployGovernance.DeployedAddress,
            VoteContractAddress = DeployVote.DeployedAddress,
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        });
        Logger.LogTrace("Initialized election.");
    }

    private async Task InitializeVoteAsync()
    {
        Logger.LogTrace("Initializing vote.");
        await VoteContractStub.Initialize.SendAsync(new TomorrowDAO.Contracts.Vote.InitializeInput()
        {
            DaoContractAddress = DeployDao.DeployedAddress,
            GovernanceContractAddress = DeployGovernance.DeployedAddress,
            ElectionContractAddress = DeployElection.DeployedAddress
        });
        Logger.LogTrace("Initialized vote.");
    }
}