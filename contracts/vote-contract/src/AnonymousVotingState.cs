using AElf.Sdk.CSharp.State;
using AElf.Types;
using Groth16Verifier;
using MerkleTreeWithHistory;

namespace TomorrowDAO.Contracts.Vote;

public class AnonymousVotingState : StructuredState
{
    internal Groth16VerifierContainer.Groth16VerifierReferenceState Groth16VerifierContract { get; set; }

    // proposal_id -> nullifier -> bool
    public MappedState<Hash, Hash, bool> Nullifiers { get; set; }

    // proposal_id -> commitment -> bool
    public MappedState<Hash, Hash, bool> Commitments { get; set; }
 
    //@formatter:off
    internal MerkleTreeWithHistoryContainer.MerkleTreeWithHistoryReferenceState MerkleTreeWithHistoryContract { get; set; }
    //@formatter:on
}