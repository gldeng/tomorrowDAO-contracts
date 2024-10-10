using System;
using System.Threading.Tasks;
using TomorrowDAO.Contracts.Governance;
using Xunit;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AnonymousVoteAdmin;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContractTest
{
    public class SampleAndProof
    {
        public Deposit Deposit { get; set; }
        public Input Input { get; set; }
        public Proof Proof { get; set; }
    }

    public class Deposit
    {
        public string Secret { get; set; }
        public string Nullifier { get; set; }
        public int Recipient { get; set; }
        public string Commitment { get; set; }
    }

    public class Input
    {
        public string Root { get; set; }
        public string NullifierHash { get; set; }
        public string Nullifier { get; set; }
        public string Relayer { get; set; }
        public string Recipient { get; set; }
        public string Fee { get; set; }
        public string Refund { get; set; }
        public string Secret { get; set; }
        public string[] PathElements { get; set; }
        public int[] PathIndices { get; set; }
    }

    public class Proof
    {
        public string[] Pi_a { get; set; }
        public string[][] Pi_b { get; set; }
        public string[] Pi_c { get; set; }
        public string[] PublicSignals { get; set; }
    }

    //@formatter:off
    private Dictionary<string,Func<Task>> ProposalCreators => new Dictionary<string,Func<Task>>()
    {
        {"GovernanceO1A1VProposalId", async() =>{ GovernanceO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, OSchemeAddress, UniqueVoteVoteSchemeId, anonymous:true); }},
        {"GovernanceR1T1VProposalId", async() =>{ GovernanceR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"GovernanceHc1T1VProposalId", async() =>{ GovernanceHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"AdvisoryO1A1VProposalId", async() =>{ AdvisoryO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Advisory, OSchemeAddress, UniqueVoteVoteSchemeId, anonymous:true); }},
        {"AdvisoryR1T1VProposalId", async() =>{ AdvisoryR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"AdvisoryHc1T1VProposalId", async() =>{ AdvisoryHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, HcSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"NetworkDaoGovernanceR1T1VProposalId", async() =>{ NetworkDaoGovernanceR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"NetworkDaoGovernanceHc1T1VProposalId", async() =>{ NetworkDaoGovernanceHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"NetworkDaoAdvisoryR1T1VProposalId", async() =>{ NetworkDaoAdvisoryR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"NetworkDaoAdvisoryHc1T1VProposalId", async() =>{ NetworkDaoAdvisoryHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId, anonymous:true); }},
        {"AdvisoryR1T1VProposalId_NoLock_DayVote", async() =>{ AdvisoryR1T1VProposalId_NoLock_DayVote = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId_NoLock_DayVote, anonymous:true); }},
    };
    //@formatter:on


    private List<SampleAndProof> LoadSamplesAndProofs(string filename)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", filename);
        var jsonContent = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<List<SampleAndProof>>(jsonContent);
    }

    private static byte[] PadTo32Bytes(byte[] input)
    {
        if (input.Length >= 32)
        {
            return input;
        }

        var result = new byte[32];
        var padding = 32 - input.Length;

        // Copy the input to the end of the result array
        Array.Copy(input, 0, result, padding, input.Length);

        // The first 'padding' bytes will be left as zeros

        return result;
    }

    private Hash ToHash(string decimalValue)
    {
        return new Hash()
        {
            Value = ByteString.CopyFrom(PadTo32Bytes(new BigIntValue()
            {
                Value = decimalValue
            }.ToBigEndianBytes()))
        };
    }

    private Vote.VoteInput.Types.Proof ConvertProof(Proof proof)
    {
        return new VoteInput.Types.Proof()
        {
            A = new VoteInput.Types.G1Point()
            {
                X = proof.Pi_a[0],
                Y = proof.Pi_a[1],
            },
            B = new VoteInput.Types.G2Point()
            {
                X = new VoteInput.Types.Fp2()
                {
                    First = proof.Pi_b[0][1],
                    Second = proof.Pi_b[0][0],
                },
                Y = new VoteInput.Types.Fp2()
                {
                    First = proof.Pi_b[1][1],
                    Second = proof.Pi_b[1][0],
                }
            },
            C = new VoteInput.Types.G1Point()
            {
                X = proof.Pi_c[0],
                Y = proof.Pi_c[1],
            }
        };
    }

    protected InterestedEvent GetInterestedEvent<T>(Address address, T protoType) where T : IEvent<T>, new()
    {
        var logEvent = protoType.ToLogEvent(address);
        return new InterestedEvent
        {
            LogEvent = logEvent,
            Bloom = logEvent.GetBloom()
        };
    }

    [Fact]
    public async Task RegisterAnonymousVote_Test()
    {
        var sample = LoadSamplesAndProofs("1_sample_approved.json").First();

        await InitializeAll();
        await InitializeAnonymousVoteAsync();
        var creator = ProposalCreators["GovernanceR1T1VProposalId"];
        await creator();
        var votingItem = await VoteContractStub.GetVotingItem.CallAsync(GovernanceR1T1VProposalId);
        BlockTimeProvider.SetBlockTime(votingItem.StartTimestamp.AddSeconds(1));

        await ApproveElf(OneElf * 10, VoteContractAddress);
        {
            var commitment = ToHash(sample.Deposit.Commitment);
            var blockTime = BlockTimeProvider.GetBlockTime();
            var tx =
                await this.VoteContractStub.RegisterCommitment.SendAsync(new RegisterCommitmentInput()
                {
                    VoteAmount = OneElf,
                    VotingItemId = GovernanceR1T1VProposalId,
                    Commitment = commitment,
                });

            var expectedCommit = new Committed()
            {
                DaoId = DaoId,
                ProposalId = GovernanceR1T1VProposalId,
                Commitment = commitment,
                LeafIndex = 0,
                Timestamp = blockTime
            };
            tx.ShouldContainEvent(expectedCommit, VoteContractAddress);
        }


        // Make Voting started
        var duration = votingItem.EndTimestamp - votingItem.StartTimestamp;
        var halfDuration = Duration.FromTimeSpan(duration.ToTimeSpan().Divide(2));
        BlockTimeProvider.SetBlockTime((votingItem.StartTimestamp + halfDuration).AddSeconds(1));

        {
            var tx = await VoteContractStub.Vote.SendAsync(new VoteInput
            {
                VoteOption = (int)VoteOption.Approved,
                VotingItemId = GovernanceR1T1VProposalId,
                Memo = "memo",
                AnonymousVoteExtraInfo = new VoteInput.Types.AnonymousVoteExtraInfo()
                {
                    Nullifier = ToHash(sample.Input.NullifierHash),
                    Proof = ConvertProof(sample.Proof)
                }
            });
            tx.TransactionResult.Error.ShouldBe("");
        }

        {
            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(GovernanceR1T1VProposalId);
            votingResult.ApproveCounts.ShouldBe(1);
            votingResult.RejectCounts.ShouldBe(0);
            votingResult.AbstainCounts.ShouldBe(0);
            votingResult.TotalVotersCount.ShouldBe(1);
        }


        // await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
        // await Vote(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId);
        // await Vote(OneElf, VoteOption.Approved, AdvisoryHc1T1VProposalId);
        //     
        // // organization
        // await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceO1A1VProposalId);
        //     
        // // NetworkDao
        // await Vote(OneElf, VoteOption.Abstained, NetworkDaoGovernanceR1T1VProposalId);
        // await Vote(OneElf, VoteOption.Rejected, NetworkDaoGovernanceHc1T1VProposalId);
        // await Vote(OneElf, VoteOption.Abstained, NetworkDaoAdvisoryR1T1VProposalId);
        // await Vote(OneElf, VoteOption.Rejected, NetworkDaoAdvisoryHc1T1VProposalId);
    }
}