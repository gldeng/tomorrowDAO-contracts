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
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.TestContracts.Governance;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace TomorrowDAO.Contracts.DAO;

public class Module : ContractTestModule<DAOContract>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}

public class TestBase : ContractTestBase<Module>
{
    internal ACS0Container.ACS0Stub GenesisContractStub;
    internal DAOContractContainer.DAOContractStub DAOContractStub;
    internal DAOContractContainer.DAOContractStub UserDAOContractStub;
    internal DAOContractContainer.DAOContractStub OtherDAOContractStub;
    internal TestGovernanceContractContainer.TestGovernanceContractStub GovernanceContractStub;

    internal ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    internal Address DefaultAddress => Accounts[0].Address;

    internal ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    internal Address UserAddress => Accounts[1].Address;

    internal Address ReferendumAddress => Accounts[10].Address;
    internal Address HighCouncilAddress => Accounts[11].Address;
    internal ECKeyPair OtherKeyPair => Accounts[12].KeyPair;
    internal Address OtherAddress => Accounts[12].Address;
    
    internal Address DAOContractAddress;
    internal Address GovernanceContractAddress;

    public TestBase()
    {
        GenesisContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        // deploy dao contract
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

        // deploy test governance contract
        code = System.IO.File.ReadAllBytes(typeof(GovernanceContract).Assembly.Location);
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("test_governance"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));

        GovernanceContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        DAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, DefaultKeyPair);
        UserDAOContractStub =
            GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, UserKeyPair);
        OtherDAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, OtherKeyPair);
        GovernanceContractStub =
            GetContractStub<TestGovernanceContractContainer.TestGovernanceContractStub>(GovernanceContractAddress,
                DefaultKeyPair);
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
}