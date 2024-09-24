using System;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace MainNamespace
{
    public class MainContractState : ContractState
    {
        public MappedState<Hash, TreeInfo> TreeInfos { get; set; }

        // TreeId -> Level -> Hash
        public MappedState<Hash, UInt32, BytesValue> FilledSubtrees { get; set; }
        public MappedState<Hash,UInt32, Hash> Roots { get; set; }
    }
}