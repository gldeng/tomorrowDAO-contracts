using System.Linq;
using System.Text.RegularExpressions;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO
{
    public partial class DAOContract : DAOContractContainer.DAOContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            Assert(input != null, "Invalid input.");

            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender,
                "No permission.");

            InitializeContract(input);
            

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.Initialized.Value = true;
            return new Empty();
        }

        private void InitializeContract(InitializeInput input)
        {
            Assert(IsAddressValid(input.GovernanceContractAddress), $"Invalid governance contract address.");
            State.GovernanceContract.Value = input.GovernanceContractAddress;
            
            Assert(IsAddressValid(input.ElectionContractAddress), $"Invalid election contract address.");
            State.ElectionContract.Value = input.ElectionContractAddress;
            
            Assert(IsAddressValid(input.TimelockContractAddress), $"Invalid timelock contract address.");
            State.TimelockContract.Value = input.TimelockContractAddress;
            
            Assert(IsAddressValid(input.TreasuryContractAddress), $"Invalid treasury contract address.");
            State.TreasuryContract.Value = input.TreasuryContractAddress;
            
            Assert(IsAddressValid(input.VoteContractAddress), $"Invalid vote contract address.");
            State.VoteContract.Value = input.VoteContractAddress;
        }

        public override Empty CreateDAO(CreateDAOInput input)
        {
            CheckInitialized();

            var daoId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),
                HashHelper.ComputeFrom(Context.Sender));

            Assert(State.DAOInfoMap[daoId] == null, "DAO already exists.");

            ProcessDAOBaseInfo(daoId, input);
            ProcessDAOComponents(daoId, input);

            return new Empty();
        }

        private void ProcessDAOBaseInfo(Hash daoId, CreateDAOInput input)
        {
            var daoInfo = new DAOInfo
            {
                DaoId = daoId,
                Creator = Context.Sender,
                SubsistStatus = true,
                ContractAddressList = new ContractAddressList
                {
                    GovernanceContractAddress = State.GovernanceContract.Value
                }
            };

            State.DAOInfoMap[daoId] = daoInfo;

            ProcessMetadata(daoId, input.Metadata);
            ProcessGovernanceToken(daoId, input.GovernanceToken);

            Context.Fire(new DAOCreated
            {
                DaoId = daoId,
                Metadata = input.Metadata,
                Creator = Context.Sender,
                GovernanceToken = input.GovernanceToken,
                ContractAddressList = daoInfo.ContractAddressList
            });
        }

        private void ProcessMetadata(Hash daoId, Metadata metadata)
        {
            Assert(metadata != null, "Invalid metadata.");
            Assert(IsStringValid(metadata.Name) && metadata.Name.Length <= DAOContractConstants.MaxNameLength,
                "Invalid metadata name.");
            Assert(State.DAONameMap[metadata.Name] == null, "DAO name already exists.");
            Assert(IsStringValid(metadata.LogoUrl) && metadata.LogoUrl.Length <= DAOContractConstants.MaxLogoUrlLength,
                "Invalid metadata logo url.");
            Assert(
                IsStringValid(metadata.Description) &&
                metadata.Description.Length <= DAOContractConstants.MaxDescriptionLength,
                "Invalid metadata description.");

            Assert(
                metadata.SocialMedia.Count > 0 &&
                metadata.SocialMedia.Count <= DAOContractConstants.MaxSocialMediaListCount,
                "Invalid metadata social media count.");

            foreach (var socialMedia in metadata.SocialMedia.Keys)
            {
                Assert(
                    IsStringValid(socialMedia) && socialMedia.Length <= DAOContractConstants.MaxSocialMediaNameLength,
                    "Invalid social media name.");
                Assert(
                    IsStringValid(metadata.SocialMedia[socialMedia]) && metadata.SocialMedia[socialMedia].Length <=
                    DAOContractConstants.MaxSocialMediaUrlLength, "Invalid social media url.");
            }

            State.MetadataMap[daoId] = metadata;
            State.DAONameMap[metadata.Name] = daoId;
        }

        private void ProcessGovernanceToken(Hash daoId, string governanceToken)
        {
            if (IsStringValid(governanceToken))
            {
                Assert(governanceToken.Length <= DAOContractConstants.SymbolMaxLength &&
                       governanceToken.All(IsValidTokenChar), "Invalid token symbol.");

                var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
                {
                    Symbol = governanceToken
                });

                Assert(!string.IsNullOrWhiteSpace(tokenInfo.Symbol), "Token not found.");

                State.DAOInfoMap[daoId].GovernanceToken = governanceToken;
            }
        }

        private void ProcessDAOComponents(Hash daoId, CreateDAOInput input)
        {
            ProcessGovernanceMechanism(daoId, input.GovernanceSchemeInput);
            // ProcessHighCouncil(daoId, input.HighCouncilInput);
            // ProcessVoteContract(daoId);
            // ProcessTreasuryContract(daoId, input.IsTreasuryContractNeeded);
            ProcessFileUploads(daoId, input.Files);
            ProcessPermissions(daoId, input.PermissionInfos);
        }

        private void ProcessGovernanceMechanism(Hash daoId, GovernanceSchemeInput input)
        {
            if (input != null)
            {
                Assert(IsHashValid(input.GovernanceSchemeId), "Invalid governance scheme id.");
                Assert(input.GovernanceSchemeThreshold != null, "Invalid governance scheme threshold.");

                var governanceSchemeThreshold = new Governance.GovernanceSchemeThreshold
                {
                    MinimalVoteThreshold = input.GovernanceSchemeThreshold.MinimalVoteThreshold,
                    MinimalRequiredThreshold = input.GovernanceSchemeThreshold.MinimalRequiredThreshold,
                    MinimalApproveThreshold = input.GovernanceSchemeThreshold.MinimalApproveThreshold,
                    MaximalAbstentionThreshold = input.GovernanceSchemeThreshold.MaximalAbstentionThreshold,
                    MaximalRejectionThreshold = input.GovernanceSchemeThreshold.MaximalRejectionThreshold
                };

                State.GovernanceContract.AddGovernanceSubScheme.Send(new AddGovernanceSubSchemeInput
                {
                    SchemeThreshold = governanceSchemeThreshold,
                    SchemeId = input.GovernanceSchemeId,
                    DaoId = daoId
                });

                State.GovernanceContract.CreateOrganization.Send(new CreateOrganizationInput
                {
                    Symbol = State.DAOInfoMap[daoId].GovernanceToken,
                    GovernanceSchemeId = input.GovernanceSchemeId,
                    SchemeThreshold = governanceSchemeThreshold,
                    OrganizationName = State.MetadataMap[daoId].Name,
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = { Context.Sender }
                    }
                });
            }
            // TODO Need Governance Contract to support this
            // else
            // {
            //     var schemeId = State.GovernanceContract.GetReferendumSchemeId.Call(new Empty());
            //     State.GovernanceContract.CreateOrganization.Send(new CreateOrganizationInput
            //     {
            //         Symbol = State.DAOInfoMap[daoId].GovernanceToken,
            //         GovernanceSchemeId = schemeId,
            //         OrganizationName = State.MetadataMap[daoId].Name
            //     });
            // }
        }

        // private void ProcessHighCouncil(Hash daoId, HighCouncilInput input)
        // {
        //     var schemeId = State.GovernanceContract.GetParliamentSchemeId.Call(new Empty());
        //     var createOrganizationInput = new CreateOrganizationInput
        //     {
        //         Symbol = State.DAOInfoMap[daoId].GovernanceToken,
        //         GovernanceSchemeId = schemeId,
        //         OrganizationName = State.MetadataMap[daoId].Name
        //     };
        //
        //     State.GovernanceContract.CreateOrganization.Send(createOrganizationInput);
        //
        //     var address = State.GovernanceContract.CalculateOrganizationAddress.Call(createOrganizationInput);
        //
        //     State.HighCouncilAddressMap[daoId] = address;
        //
        //     if (input != null && IsStringValid(State.DAOInfoMap[daoId].GovernanceToken))
        //     {
        //         ProcessEnableHighCouncil(daoId, input.HighCouncilConfig, input.IsRequireHighCouncilForExecution);
        //     }
        // }

        private void ProcessTreasuryContract(Hash daoId, bool isTreasuryNeeded)
        {
            if (isTreasuryNeeded)
            {
                // TODO
            }
        }

        private void ProcessVoteContract(Hash daoId)
        {
            // TODO
        }

        public override Empty SetSubsistStatus(SetSubsistStatusInput input)
        {
            Assert(input != null, "Invalid input.");
            Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
            CheckDAOExists(input.DaoId);
            AssertPermission(input.DaoId, nameof(SetSubsistStatus));

            if (State.DAOInfoMap[input.DaoId].SubsistStatus == input.Status) return new Empty();

            State.DAOInfoMap[input.DaoId].SubsistStatus = input.Status;

            Context.Fire(new SubsistStatusSet
            {
                DaoId = input.DaoId,
                Status = input.Status
            });

            return new Empty();
        }
    }
}