using AElf.Types;

namespace TomorrowDAO.Pipelines.AnonymousVoteDeployment;

public class Config
{
    public Address VoteContractAddress { get; set; }
    public string MerkleTreeContractCodePath { get; set; }
}