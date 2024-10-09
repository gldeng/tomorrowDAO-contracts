extern alias AElfScript;
using AElf;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElfScript::AElf.Scripts;
using AElf.Types;
using AnonymousVote;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism;

namespace TomorrowDAO.Pipelines.DevChainSetup;

public class Pipeline : ScriptWithConfig<Config>
{
    internal AnonymousVoteContractContainer.AnonymousVoteContractStub AnonymousVoteContractStub { get; private set; }
    internal VoteContractContainer.VoteContractStub VoteContractStub { get; private set; }
    internal DAOContractContainer.DAOContractStub DAOContractStub { get; private set; }
    internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStub { get; private set; }

    public Hash UniqueVoteVoteSchemeId { get; private set; }
    public Hash TokenBallotVoteSchemeId_NoLock_DayVote { get; private set; }
    public Hash TokenBallotVoteSchemeId { get; private set; }
    public Hash NetworkDaoId { get; private set; }
    public Hash OrganizationDaoId { get; private set; }
    public Hash DaoId { get; private set; }
    public Address NetworkDaoHcSchemeAddress { get; private set; }
    public Hash NetworkDaoHcSchemeId { get; private set; }
    public Address HcSchemeAddress { get; private set; }
    public Hash HcSchemeId { get; private set; }
    public Address NetworkDaoRSchemeAddress { get; private set; }
    public Hash NetworkDaoRSchemeId { get; private set; }
    public Address RSchemeAddress { get; private set; }
    public Hash RSchemeId { get; private set; }
    public Address OSchemeAddress { get; private set; }
    public Hash OSchemeId { get; private set; }
    internal ProposalCreated GovernanceR1T1VProposal { get; private set; }

    public override async Task RunAsync()
    {
        InitializeStubs();
        await CreateVoteSchemeAsync(VoteMechanism.UniqueVote);
        await CreateVoteSchemeAsync(VoteMechanism.TokenBallot);
        await CreateVoteSchemeAsync(VoteMechanism.TokenBallot, true, VoteStrategy.DayDistinct);
        await CreateDaoAsync("DAO", true);
        await CreateDaoAsync("NetworkDAO");
        await CreateDaoAsync("Organization DAO", false, 2);
        GovernanceR1T1VProposal = await CreateProposal(
            DaoId,
            ProposalType.Governance,
            RSchemeAddress,
            TokenBallotVoteSchemeId,
            anonymous: true
        );
        await DoTestAsync();
    }

    private void InitializeStubs()
    {
        AnonymousVoteContractStub = this.GetInstance<AnonymousVoteContractContainer.AnonymousVoteContractStub>(
            Config.VoteContractAddress
        );
        VoteContractStub =
            this.GetInstance<VoteContractContainer.VoteContractStub>(
                Config.VoteContractAddress
            );
        DAOContractStub = this.GetInstance<DAOContractContainer.DAOContractStub>(
            Config.DaoContractAddress
        );
        GovernanceContractStub = this.GetInstance<GovernanceContractContainer.GovernanceContractStub>(
            Config.GovernanceContractAddress
        );
    }


    private async Task CreateVoteSchemeAsync(VoteMechanism voteMechanism, bool withoutLockToken = false,
        VoteStrategy voteStrategy = VoteStrategy.ProposalDistinct)
    {
        var input = new CreateVoteSchemeInput
        {
            VoteMechanism = voteMechanism, WithoutLockToken = withoutLockToken, VoteStrategy = voteStrategy
        };
        var voteSchemeId = HashHelper.ConcatAndCompute(
            HashHelper.ComputeFrom(Config.VoteContractAddress),
            HashHelper.ComputeFrom(input));

        var existing = await VoteContractStub.GetVoteScheme.CallAsync(voteSchemeId);
        if (existing == null || existing.Equals(new VoteScheme()))
        {
            var result = await VoteContractStub.CreateVoteScheme.SendAsync(input);
            var log = result.GetLogEvents<VoteSchemeCreated>().FirstOrDefault();
            voteSchemeId = log.VoteSchemeId;
        }


        switch (voteMechanism)
        {
            case VoteMechanism.UniqueVote:
                Logger.LogInformation($"Unique vote scheme created: {voteSchemeId}");
                UniqueVoteVoteSchemeId = voteSchemeId;
                break;
            case VoteMechanism.TokenBallot:
                if (withoutLockToken && VoteStrategy.DayDistinct == voteStrategy)
                {
                    Logger.LogInformation($"Token ballot scheme created: {voteSchemeId}");
                    TokenBallotVoteSchemeId_NoLock_DayVote = voteSchemeId;
                }
                else
                {
                    TokenBallotVoteSchemeId = voteSchemeId;
                    Logger.LogInformation($"Token ballot scheme created: {voteSchemeId}");
                }

                break;
        }
    }

    public async Task CreateDaoAsync(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        var input = GetCreateDAOInput(daoName, isNetworkDao, governanceMechanism);
        var daoId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),
            HashHelper.ComputeFrom(Address.FromPublicKey(DeployerKey.PublicKey)));

        var existing = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
        if (existing == null || existing.Equals(new DAOInfo()))
        {
            var result =
                await DAOContractStub.CreateDAO.SendAsync(input);
            var dAOCreatedLog = result.GetLogEvents<DAOCreated>().FirstOrDefault();
            daoId = dAOCreatedLog.DaoId;
        }


        if (isNetworkDao)
        {
            NetworkDaoId = daoId;
        }
        else
        {
            if (governanceMechanism == 2)
            {
                OrganizationDaoId = daoId;
            }
            else
            {
                DaoId = daoId;
            }
        }


        var schemeHash = HashHelper.ConcatAndCompute(
            HashHelper.ComputeFrom(daoId),
            HashHelper.ComputeFrom(Config.GovernanceContractAddress),
            HashHelper.ComputeFrom(((GovernanceMechanism)governanceMechanism).ToString())
        );
        var schemeAddress = Config.GovernanceContractAddress.DeriveVirtualAddress(schemeHash);

        switch (governanceMechanism)
        {
            case (int)GovernanceMechanism.HighCouncil when isNetworkDao:
                Logger.LogInformation($"High council scheme created: {schemeAddress}");
                Logger.LogInformation($"High council scheme id: {schemeHash}");
                NetworkDaoHcSchemeAddress = schemeAddress;
                NetworkDaoHcSchemeId = schemeHash;
                break;
            case (int)GovernanceMechanism.HighCouncil:
                Logger.LogInformation($"High council scheme created: {schemeAddress}");
                Logger.LogInformation($"High council scheme id: {schemeHash}");
                HcSchemeAddress = schemeAddress;
                HcSchemeId = schemeHash;
                break;
            case (int)GovernanceMechanism.Referendum when isNetworkDao:
                Logger.LogInformation($"Referendum scheme created: {schemeAddress}");
                Logger.LogInformation($"Referendum scheme id: {schemeHash}");
                NetworkDaoRSchemeAddress = schemeAddress;
                NetworkDaoRSchemeId = schemeHash;
                break;
            case (int)GovernanceMechanism.Referendum:
                RSchemeAddress = schemeAddress;
                RSchemeId = schemeHash;
                Logger.LogInformation($"Referendum scheme created: {schemeAddress}");
                Logger.LogInformation($"Referendum scheme id: {schemeHash}");
                break;
            case (int)GovernanceMechanism.Organization:
                OSchemeAddress = schemeAddress;
                OSchemeId = schemeHash;
                Logger.LogInformation($"Organization scheme created: {schemeAddress}");
                Logger.LogInformation($"Organization scheme id: {schemeHash}");
                break;
        }
    }

    private CreateDAOInput GetCreateDAOInput(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        return new CreateDAOInput
        {
            Metadata = new()
            {
                Name = daoName,
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string> { { "aa", "bb" } }
                }
            },
            GovernanceToken = governanceMechanism == 2 ? "" : "ELF",
            GovernanceSchemeThreshold = new Contracts.DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 0,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0
            },
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = new HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 2,
                    MaxHighCouncilCandidateCount = 20,
                    ElectionPeriod = 7,
                    StakingAmount = 100000000
                },
                GovernanceSchemeThreshold = new Contracts.DAO.GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2000,
                    MaximalAbstentionThreshold = 2000
                },
                HighCouncilMembers = new Contracts.DAO.AddressList()
                {
                    Value =
                    {
                        new[]
                        {
                            Address.FromPublicKey(DeployerKey.PublicKey),
                        }
                    }
                },
                IsHighCouncilElectionClose = false
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao,
            GovernanceMechanism = governanceMechanism,
            Members = new Contracts.DAO.AddressList
            {
                Value =
                {
                    Address.FromPublicKey(DeployerKey.PublicKey)
                }
            }
        };
    }

    async Task<ProposalCreated> CreateProposal(
        Hash DaoId,
        ProposalType proposalType,
        Address schemeAddress,
        Hash voteSchemeId,
        string error = "",
        bool anonymous = false
    )
    {
        IExecutionResult<Hash> result;
        if (string.IsNullOrEmpty(error))
        {
            var input = await GetCreateProposalInputAsync(DaoId, proposalType, schemeAddress, voteSchemeId, anonymous);
            result = await GovernanceContractStub.CreateProposal.SendAsync(input);
            return result.GetLogEvents<ProposalCreated>().FirstOrDefault();
        }

        return null;
    }

    private async Task<CreateProposalInput> GetCreateProposalInputAsync(Hash DaoId, ProposalType proposalType,
        Address schemeAddress,
        Hash voteSchemeId, bool anonymous = false)
    {
        var chainStatus = await Client.GetChainStatusAsync();
        var block = await Client.GetBlockByHeightAsync(chainStatus.LongestChainHeight);

        var startTime = block.Header.Time.ToTimestamp().AddSeconds(30);
        return new CreateProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteSchemeId,
                ActiveStartTime = startTime.Seconds,
                ActiveEndTime = startTime.AddMinutes(2).Seconds,
                IsAnonymous = anonymous
            },
            ProposalType = (int)proposalType,
            Transaction = new ExecuteTransaction
            {
                ContractMethodName = "ContractMethodName",
                ToAddress = Address.FromPublicKey(DeployerKey.PublicKey),
                Params = ByteStringHelper.FromHexString(StringExtensions.GetBytes("Params").ToHex())
            }
        };
    }

    private async Task DoTestAsync()
    {
        var sample = JsonConvert.DeserializeObject<List<SampleAndProof>>(SampeApproved)!.First();

        var votingItem = await VoteContractStub.GetVotingItem.CallAsync(GovernanceR1T1VProposal.ProposalId);
        const long OneElf = 1_00000000;
        var wait = (votingItem.StartTimestamp - DateTime.UtcNow.ToTimestamp());
        if (wait.CompareTo(new Duration()) > 0)
        {
            await Task.Delay(wait.ToTimeSpan());
        }

        var myBalance = await this.GetBalanceAsync(DeployerKey.GetAddress());
        Console.WriteLine(myBalance);
        await this.ApproveTokenAllowanceAsync(Config.VoteContractAddress, OneElf * 10);

        var commitment = ToHash(sample.Deposit.Commitment);
        var tx0 =
            await this.AnonymousVoteContractStub.RegisterCommitment.SendAsync(new RegisterCommitmentInput()
            {
                VoteAmount = OneElf,
                VotingItemId = GovernanceR1T1VProposal.ProposalId,
                Commitment = commitment,
            });
        var commits = tx0.GetLogEvents<Commit>();


        // Wait until Voting starts
        var duration = votingItem.EndTimestamp - votingItem.StartTimestamp;
        var halfDuration = Duration.FromTimeSpan(duration.ToTimeSpan().Divide(2));
        var voteStart = (votingItem.StartTimestamp + halfDuration).AddSeconds(1);
        var delayUntilStart = voteStart - DateTime.UtcNow.ToTimestamp();
        if (delayUntilStart.CompareTo(new Duration()) > 0)
        {
            await Task.Delay(delayUntilStart.ToTimeSpan());
        }

        var tx1 = await VoteContractStub.Vote.SendAsync(new VoteInput
        {
            VoteOption = (int)VoteOption.Approved,
            VotingItemId = GovernanceR1T1VProposal.ProposalId,
            Memo = "memo",
            AnonymousVoteExtraInfo = new VoteInput.Types.AnonymousVoteExtraInfo()
            {
                Nullifier = ToHash(sample.Input.NullifierHash),
                Proof = ConvertProof(sample.Proof)
            }
        });
        var voted = tx1.GetLogEvents<Voted>().FirstOrDefault();

        var votingResult = await VoteContractStub.GetVotingResult.CallAsync(GovernanceR1T1VProposal.ProposalId);
        Logger.LogInformation($"Voting Result: {votingResult}");


        Hash ToHash(string decimalValue)
        {
            return new Hash()
            {
                Value = ByteString.CopyFrom(PadTo32Bytes(new BigIntValue()
                {
                    Value = decimalValue
                }.ToBigEndianBytes()))
            };
        }

        byte[] PadTo32Bytes(byte[] input)
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

        VoteInput.Types.Proof ConvertProof(Proof proof)
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
    }

    protected async Task ApproveElf(long amount, Address spender)
    {
        await this.ApproveTokenAllowanceAsync(spender, amount);
    }

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

    private const string SampeApproved = @"[
          {
            ""deposit"": {
              ""secret"": ""38119288996748608266309159908750020573504693372175816296968645583907767289"",
              ""nullifier"": ""184027911048417246671915402050208449546762063210127966789015092921990337030"",
              ""recipient"": 0,
              ""commitment"": ""14892736337423961035925868381169536231117376300470621188673855627659182525229""
            },
            ""input"": {
              ""root"": ""21474851287001869773600300600800399197958653595561850934351078519039126668746"",
              ""nullifierHash"": ""7951850806096850951421127266320076936732701977695686457616108537454720495869"",
              ""nullifier"": ""184027911048417246671915402050208449546762063210127966789015092921990337030"",
              ""relayer"": ""0"",
              ""recipient"": 0,
              ""fee"": ""0"",
              ""refund"": ""0"",
              ""secret"": ""38119288996748608266309159908750020573504693372175816296968645583907767289"",
              ""pathElements"": [
                ""21663839004416932945382355908790599225266501822907911457504978515578255421292"",
                ""16923532097304556005972200564242292693309333953544141029519619077135960040221"",
                ""7833458610320835472520144237082236871909694928684820466656733259024982655488"",
                ""14506027710748750947258687001455876266559341618222612722926156490737302846427"",
                ""4766583705360062980279572762279781527342845808161105063909171241304075622345"",
                ""16640205414190175414380077665118269450294358858897019640557533278896634808665"",
                ""13024477302430254842915163302704885770955784224100349847438808884122720088412"",
                ""11345696205391376769769683860277269518617256738724086786512014734609753488820"",
                ""17235543131546745471991808272245772046758360534180976603221801364506032471936"",
                ""155962837046691114236524362966874066300454611955781275944230309195800494087"",
                ""14030416097908897320437553787826300082392928432242046897689557706485311282736"",
                ""12626316503845421241020584259526236205728737442715389902276517188414400172517"",
                ""6729873933803351171051407921027021443029157982378522227479748669930764447503"",
                ""12963910739953248305308691828220784129233893953613908022664851984069510335421"",
                ""8697310796973811813791996651816817650608143394255750603240183429036696711432"",
                ""9001816533475173848300051969191408053495003693097546138634479732228054209462"",
                ""13882856022500117449912597249521445907860641470008251408376408693167665584212"",
                ""6167697920744083294431071781953545901493956884412099107903554924846764168938"",
                ""16572499860108808790864031418434474032816278079272694833180094335573354127261"",
                ""11544818037702067293688063426012553693851444915243122674915303779243865603077""
              ],
              ""pathIndices"": [
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0
              ]
            },
            ""proof"": {
              ""pi_a"": [
                ""12262637740489390184722454577101971546828182529975435995311263150571394542966"",
                ""16140745942799655021327346073313116980500267322490165033750824145982663540484"",
                ""1""
              ],
              ""pi_b"": [
                [
                  ""12101380638910074070004794483809083024894242985378737936565160590089341402833"",
                  ""15173002752532806305799630478081987098530274061106007747538388676137814728844""
                ],
                [
                  ""13597606347463452334514301677866757811992142499777902902153009956458605261846"",
                  ""8356709898674920675519409911731016412359943004270358508906502040331221525228""
                ],
                [
                  ""1"",
                  ""0""
                ]
              ],
              ""pi_c"": [
                ""10700150255470959314794477554029313389282648887797052318653022409028899769110"",
                ""6050267718067077734711655080039563003476702420376718989682015581173434862551"",
                ""1""
              ],
              ""publicSignals"": [
                ""21474851287001869773600300600800399197958653595561850934351078519039126668746"",
                ""7951850806096850951421127266320076936732701977695686457616108537454720495869"",
                ""0"",
                ""0"",
                ""0"",
                ""0""
              ]
            }
          }
        ]";
}