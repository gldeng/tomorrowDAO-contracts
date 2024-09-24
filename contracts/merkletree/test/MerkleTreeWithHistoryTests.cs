using System;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using MerkleTreeWithHistory;
using Xunit;

namespace MainNamespace
{
    // This class is unit test class, and it inherit TestBase. Write your unit test code inside it
    public class MerkleTreeWithHistoryTests : TestBase
    {
        [Fact]
        public async Task Calculate_Root_Test()
        {
            var treeId = new Hash() { Value = ByteString.CopyFromUtf8("tree1") };
            var tx = await Stub.CreateTree.SendAsync(new CreateTreeInput
            {
                TreeId = treeId,
                Levels = 20
            });
            var root = await Stub.GetLastRoot.CallAsync(treeId);
            Assert.Equal(
                Hash.LoadFromHex("0x198622acbd783d1b0d9064105b1fc8e4d8889de95c4c519b3f635809fe6afc05"),
                root
            );
        }

        [Fact]
        public async Task Insert_1()
        {
            var treeId = new Hash() { Value = ByteString.CopyFromUtf8("tree1") };
            {
                var tx = await Stub.CreateTree.SendAsync(new CreateTreeInput
                {
                    TreeId = treeId,
                    Levels = 20
                });
            }
            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 1 })
                });
            }

            var root = await Stub.GetLastRoot.CallAsync(treeId);

            Assert.Equal(
                Hash.LoadFromHex("0x0a8ab16921ac878ebf0edb3883cc1df6e0a443e09588af3cda17e41b4a7fb6f9"),
                root
            );
        }

        [Fact]
        public async Task Insert_2()
        {
            var treeId = new Hash() { Value = ByteString.CopyFromUtf8("tree1") };
            {
                var tx = await Stub.CreateTree.SendAsync(new CreateTreeInput
                {
                    TreeId = treeId,
                    Levels = 20
                });
            }
            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 1 })
                });
            }

            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 2 })
                });
            }
            var root = await Stub.GetLastRoot.CallAsync(treeId);

            Assert.Equal(
                Hash.LoadFromHex("0x2a8f5562e5e3f6c807682f10513c97c6e8f44bb90bcb8a7fb76aea8b4c66e3d8"),
                root
            );
        }

        [Fact]
        public async Task Insert_3()
        {
            var treeId = new Hash() { Value = ByteString.CopyFromUtf8("tree1") };
            {
                var tx = await Stub.CreateTree.SendAsync(new CreateTreeInput
                {
                    TreeId = treeId,
                    Levels = 20
                });
            }
            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 1 })
                });
            }

            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 2 })
                });
            }
            {
                var tx = await Stub.InsertLeaf.SendAsync(new InsertLeafInput
                {
                    TreeId = treeId,
                    Leaf = ByteString.CopyFrom(new byte[] { 3 })
                });
            }
            var root = await Stub.GetLastRoot.CallAsync(treeId);

            Assert.Equal(
                Hash.LoadFromHex("0x156c224f23b580116f1e543fc0b78ce38f1a4aa826f2460852cfbd0860da8dd8"),
                root
            );
        }
    }
}