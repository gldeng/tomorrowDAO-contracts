using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.Treasury
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<TreasuryContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            TreasuryContractStub = GetTreasuryContractStub(DefaultKeyPair);
        }

        private TreasuryContractContainer.TreasuryContractStub GetTreasuryContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}