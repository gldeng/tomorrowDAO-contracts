using AElf.Sdk.CSharp;
using AElf.Types;
using AnonymousVoteAdmin;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Groth16Verifier;
using MerkleTreeWithHistory;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract
{
    private void Commit(Hash proposalId, Hash commitment)
    {
        Assert(!GetAnonymousVotingState().Commitments[proposalId][commitment], "Commitment already exists.");
        GetAnonymousVotingState().Commitments[proposalId][commitment] = true;
        var leafIndex =
            GetAnonymousVotingState().MerkleTreeWithHistoryContract
                .GetNextIndex.Call(proposalId); // Indirectly confirms the tree exists
        GetAnonymousVotingState().MerkleTreeWithHistoryContract.InsertLeaf.Send(new InsertLeafInput()
        {
            TreeId = proposalId,
            Leaf = commitment.Value
        });
        Context.Fire(new Commit()
        {
            ProposalId = proposalId,
            Commitment = commitment,
            LeafIndex = leafIndex.Value,
            Timestamp = Context.CurrentBlockTime
        });
    }

    private Hash GetMerkleTreeRoot(Hash proposalId)
    {
       return GetAnonymousVotingState().MerkleTreeWithHistoryContract.GetLastRoot.Call(proposalId);
    }

    private void Nullify(Hash proposalId, Hash nullifier, VoteInput.Types.Proof proof)
    {
        Assert(!GetAnonymousVotingState().Nullifiers[proposalId][nullifier], "Nullifier already exists.");

        var root = GetMerkleTreeRoot(proposalId);

        var verified = GetAnonymousVotingState().Groth16VerifierContract.VerifyProof.Call(new VerifyProofInput()
        {
            Proof = VerifyProofInput.Types.Proof.Parser.ParseFrom(proof.ToByteArray()),
            Input =
            {
                BigIntValue.FromBigEndianBytes(nullifier.Value.ToByteArray()).Value,
                BigIntValue.FromBigEndianBytes(root.Value.ToByteArray()).Value
            }
        });
        Assert(verified.Value, "Proof is invalid.");
        GetAnonymousVotingState().Nullifiers[proposalId][nullifier] = true;
    }

    private AnonymousVotingState GetAnonymousVotingState()
    {
        return State.AnonymousVoting;
    }

    private void AssertPerformedByGovernanceContract()
    {
        Assert(Context.Sender == State.GovernanceContract.Value,
            "Operation can only be performed via governance contract.");
    }

    public override Empty SetGroth16VerifierAddress(Address input)
    {
        AssertPerformedByGovernanceContract();
        GetAnonymousVotingState().Groth16VerifierContract.Value = input;
        return new Empty();
    }

    public override Address GetGroth16VerifierAddress(Empty input)
    {
        return GetAnonymousVotingState().Groth16VerifierContract.Value;
    }

    public override Empty SetMerkleTreeHistoryContractAddress(Address input)
    {
        AssertPerformedByGovernanceContract();
        return new Empty();
    }

    public override Address GetMerkleTreeHistoryContractAddress(Empty input)
    {
        return GetAnonymousVotingState().MerkleTreeWithHistoryContract.Value;
    }
}