using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.Governance
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<GovernanceContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly GovernanceContractContainer.GovernanceContractStub GovernanceContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            GovernanceContractStub = GetGovernanceContractStub(DefaultKeyPair);
        }

        private GovernanceContractContainer.GovernanceContractStub GetGovernanceContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<GovernanceContractContainer.GovernanceContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}