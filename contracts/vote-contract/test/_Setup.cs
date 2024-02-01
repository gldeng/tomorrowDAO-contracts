using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.Vote
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<VoteContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly VoteContractContainer.VoteContractStub VoteContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            VoteContractStub = GetVoteContractStub(DefaultKeyPair);
        }

        private VoteContractContainer.VoteContractStub GetVoteContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}