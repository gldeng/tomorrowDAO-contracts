using System;
using System.Diagnostics;
using AElf;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Vote;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using TimestampHelper = AElf.Contracts.Election.TimestampHelper;

namespace TomorrowDAO.Contracts.Governance
{
    // The Module class load the context required for unit testing
    [DependsOn(typeof(ContractTestModule))]
    public class Module : AElf.Testing.TestBase.ContractTestModule<GovernanceContract>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);

            //context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProviderProxy>();
            // context.Services.Replace(ServiceDescriptor
            //     .Singleton<IBlockTimeProvider, DelayBlockTimeProvider>());
            
        }
    }

    public class ResetBlockTimeProviderProxy : IResetBlockTimeProvider
    {
        public bool Enabled { get; } = true;
        public int StepMilliseconds { get; set; }
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : AElf.Testing.TestBase.ContractTestBase<Module>
    {
        internal ACS0Container.ACS0Stub GenesisContractStub;
        
        internal Address GovernanceContractAddress { get; set; }
        // The Stub class for unit testing
        internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStub;
        
        internal Address GovernanceContractAddressOther { get; set; }
        internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStubOther;
        
        internal Address DAOContractAddress { get; set; }
        internal DAOContractContainer.DAOContractStub DAOContractStub;
        
        internal Address VoteContractAddress { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub;
        
        internal Address ElectionContractAddress { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub;

        // A key pair that can be used to interact with the contract instance
        internal ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        
        protected Address DefaultAddress => Accounts[0].Address;
        
        internal ECKeyPair UserKeyPair => Accounts[1].KeyPair;
        internal Address UserAddress => Accounts[1].Address;

        protected TestBase()
        {
            var blockTimeProvider = Application.Services.GetRequiredService<IBlockTimeProvider>();
            blockTimeProvider.SetBlockTime(DateTime.UtcNow.AddDays(1).ToTimestamp());
            
            GenesisContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);
            
            DeployGovernanceContract();
            DeployDaoContract();
            DeployVoteContract();
            DeployElectionContract();
        }
        
        protected override void AfterAddApplication(IServiceCollection services)
        {
            base.AfterAddApplication(services);
            
            var blockTimeProvider = this.Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
            blockTimeProvider.SetBlockTime(DateTime.UtcNow.AddDays(3).ToTimestamp());
            // services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProviderProxy>();
            // context.Services.Replace(ServiceDescriptor
            //     .Singleton<IBlockTimeProvider, DelayBlockTimeProvider>());
        }

        internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair)
            where T : ContractStubBase, new()
        {
            return GetTester<T>(contractAddress, senderKeyPair);
        }
        
        private ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
        {
            var dataHash = HashHelper.ComputeFrom(contractOperation);
            var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
            return ByteStringHelper.FromHexString(signature.ToHex());
        }

        private void DeployGovernanceContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(GovernanceContract).Assembly.Location);
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("governance"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));
            GovernanceContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            GovernanceContractStub =
                GetContractStub<GovernanceContractContainer.GovernanceContractStub>(GovernanceContractAddress, DefaultKeyPair);
            GovernanceContractStubOther = GetContractStub<GovernanceContractContainer.GovernanceContractStub>(GovernanceContractAddress, UserKeyPair);
        }

        private void DeployDaoContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(DAOContract).Assembly.Location);
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("dao"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));

            DAOContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            DAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, DefaultKeyPair);
        }
        
        private void DeployVoteContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(VoteContract).Assembly.Location);
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("vote"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));

            VoteContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            VoteContractStub = GetContractStub<VoteContractContainer.VoteContractStub>(VoteContractAddress, DefaultKeyPair);
        }
        
        private void DeployElectionContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(ElectionContract).Assembly.Location);
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("election"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));

            ElectionContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            ElectionContractStub = GetContractStub<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, DefaultKeyPair);
        }
    }
}