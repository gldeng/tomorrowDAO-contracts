using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.Election
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<ElectionContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly ElectionContractContainer.ElectionContractStub ElectionContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            ElectionContractStub = GetElectionContractStub(DefaultKeyPair);
        }

        private ElectionContractContainer.ElectionContractStub GetElectionContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}