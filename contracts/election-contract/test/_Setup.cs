using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Standards.ACS0;
using AElf.Testing.TestBase;
using AElf.Types;
using Google.Protobuf;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace TomorrowDAO.Contracts.Election
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<ElectionContract>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }

    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        internal ACS0Container.ACS0Stub GenesisContractStub { get; set; }
        internal Address GovernanceContractAddress { get; set; }
        internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStub { get; set; }

        internal Address ElectionContractAddress { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
        
        internal Address VoteContractAddress { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        
        internal Address DAOContractAddress { get; set; }
        internal DAOContractContainer.DAOContractStub DAOContractStub { get; set; }

        // A key pair that can be used to interact with the contract instance
        internal ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        
        protected Address DefaultAddress => Accounts[0].Address;
        
        internal ECKeyPair UserKeyPair => Accounts[1].KeyPair;
        internal Address UserAddress => Accounts[1].Address;

        protected TestBase()
        {
            GenesisContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

            DeployElectionContract();
            DeployGovernanceContract();
            DeployDaoContract();
            DeployVoteContract();
        }

        private T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair)
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
            ElectionContractStub =
                GetContractStub<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, DefaultKeyPair);
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
            //GovernanceContractStubOther = GetContractStub<GovernanceContractContainer.GovernanceContractStub>(GovernanceContractAddress, UserKeyPair);
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
                Version = 2
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
    }
}