using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.DAO
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<DAOContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly DAOContractContainer.DAOContractStub DAOContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            DAOContractStub = GetDAOContractStub(DefaultKeyPair);
        }

        private DAOContractContainer.DAOContractStub GetDAOContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<DAOContractContainer.DAOContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}