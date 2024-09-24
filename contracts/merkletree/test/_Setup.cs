using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;
using StubType = MerkleTreeWithHistory.MerkleTreeWithHistoryContainer.MerkleTreeWithHistoryStub;

namespace MainNamespace
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<MainContract>
    {
    }

    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly StubType Stub;

        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            Stub = GetStub(DefaultKeyPair);
        }

        private StubType GetStub(ECKeyPair senderKeyPair)
        {
            return GetTester<StubType>(ContractAddress, senderKeyPair);
        }
    }
}