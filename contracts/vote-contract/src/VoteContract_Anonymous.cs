using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Groth16Verifier;
using MerkleTreeWithHistory;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract
{
    private void OnNewAnonymousVotingItemAdded(VotingItem votingItem)
    {
        GetAnonymousVotingState().MerkleTreeWithHistoryContract.CreateTree.Send(new CreateTreeInput()
        {
            Levels = 20,
            Owner = Context.Self,
            TreeId = votingItem.VotingItemId
        });
    }
    
    private uint Commit(Hash proposalId, Hash commitment)
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
        return leafIndex.Value;
    }

    private Hash GetMerkleTreeRoot(Hash proposalId)
    {
        return GetAnonymousVotingState().MerkleTreeWithHistoryContract.GetLastRoot.Call(proposalId);
    }

    private void Nullify(Hash proposalId, int voteOption, Hash nullifier, VoteInput.Types.Proof proof)
    {
        Assert(!GetAnonymousVotingState().Nullifiers[proposalId][nullifier], "Nullifier already exists.");

        var root = GetMerkleTreeRoot(proposalId);
        var input = new VerifyProofInput()
        {
            Proof = VerifyProofInput.Types.Proof.Parser.ParseFrom(proof.ToByteArray()),
            Input =
            {
                BigIntValue.FromBigEndianBytes(root.Value.ToByteArray()).Value,
                BigIntValue.FromBigEndianBytes(nullifier.Value.ToByteArray()).Value,
                voteOption.ToString(),
                "0",
                "0",
                "0"
            }
        };

        var verified = GetAnonymousVotingState().Groth16VerifierContract.VerifyProof.Call(input);
        Assert(verified.Value, "Proof is invalid.");
        GetAnonymousVotingState().Nullifiers[proposalId][nullifier] = true;
    }

    private AnonymousVotingState GetAnonymousVotingState()
    {
        return State.AnonymousVoting;
    }

    private void AssertPerformedByDeployer()
    {
        // TODO: Should this be changed to governance contract
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender,
            "Operation can only be performed by the deployer.");
    }

    public override Empty SetGroth16VerifierAddress(Address input)
    {
        AssertPerformedByDeployer();
        GetAnonymousVotingState().Groth16VerifierContract.Value = input;
        return new Empty();
    }

    public override Address GetGroth16VerifierAddress(Empty input)
    {
        return GetAnonymousVotingState().Groth16VerifierContract.Value;
    }

    public override Empty SetMerkleTreeHistoryContractAddress(Address input)
    {
        AssertPerformedByDeployer();
        GetAnonymousVotingState().MerkleTreeWithHistoryContract.Value = input;
        return new Empty();
    }

    public override Address GetMerkleTreeHistoryContractAddress(Empty input)
    {
        return GetAnonymousVotingState().MerkleTreeWithHistoryContract.Value;
    }
}