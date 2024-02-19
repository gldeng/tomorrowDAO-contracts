using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

// This class is unit test class, and it inherit TestBase. Write your unit test code inside it
public partial class DAOContractTests : TestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        {
            var result = await DAOContractStub.Initialize.SendAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TreasuryContractAddress = DefaultAddress,
                VoteContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await UserDAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid election contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid election contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid timelock contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid timelock contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid treasury contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid treasury contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = DefaultAddress,
                VoteContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }

        await InitializeAsync();

        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task CreateDAOTests()
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "TestDAO",
                LogoUrl = "logo_url",
                Description = "Description",
                SocialMedia =
                {
                    new Dictionary<string, string>
                    {
                        { "X", "twitter" },
                        { "Facebook", "facebook" },
                        { "Telegram", "telegram" },
                        { "Discord", "discord" },
                        { "Reddit", "reddit" }
                    }
                }
            },
            GovernanceToken = "ELF",
            IsTreasuryContractNeeded = false,
            Files =
            {
                new File
                {
                    Cid = "cid",
                    Name = "name",
                    Url = "url"
                }
            },
            PermissionInfos =
            {
                new PermissionInfo
                {
                    Where = DAOContractAddress,
                    Who = DefaultAddress,
                    What = "SetPermissions",
                    PermissionType = PermissionType.Specificaddress
                }
            }
        });

        Hash daoId;

        {
            var log = GetLogEvent<DAOCreated>(result.TransactionResult);
            log.DaoId.ShouldNotBeNull();
            log.Metadata.Name.ShouldBe("TestDAO");
            log.Metadata.LogoUrl.ShouldBe("logo_url");
            log.Metadata.Description.ShouldBe("Description");
            log.Metadata.SocialMedia.Count.ShouldBe(5);
            log.Metadata.SocialMedia["X"].ShouldBe("twitter");
            log.Metadata.SocialMedia["Facebook"].ShouldBe("facebook");
            log.Metadata.SocialMedia["Telegram"].ShouldBe("telegram");
            log.Metadata.SocialMedia["Discord"].ShouldBe("discord");
            log.Metadata.SocialMedia["Reddit"].ShouldBe("reddit");
            log.GovernanceToken.ShouldBe("ELF");
            log.Creator.ShouldBe(DefaultAddress);
            log.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);

            daoId = log.DaoId;
        }
        {
            var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.UploadedFiles.Data.Count.ShouldBe(1);
            log.UploadedFiles.Data["cid"].File.Cid.ShouldBe("cid");
            log.UploadedFiles.Data["cid"].File.Name.ShouldBe("name");
            log.UploadedFiles.Data["cid"].File.Url.ShouldBe("url");
        }
        {
            var log = GetLogEvent<PermissionsSet>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.Here.ShouldBe(DefaultAddress);
            log.PermissionInfoList.PermissionInfos.Count.ShouldBe(1);
            log.PermissionInfoList.PermissionInfos[0].Where.ShouldBe(DAOContractAddress);
            log.PermissionInfoList.PermissionInfos[0].Who.ShouldBe(DefaultAddress);
            log.PermissionInfoList.PermissionInfos[0].What.ShouldBe("SetPermissions");
        }
        {
            var output = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
            output.Creator.ShouldBe(DefaultAddress);
            output.SubsistStatus.ShouldBeTrue();
            output.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);
            output.DaoId.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetDAOIdByName.CallAsync(new StringValue
            {
                Value = "TestDAO"
            });
            output.Value.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(1);
            output.Data["cid"].File.Cid.ShouldBe("cid");
            output.Data["cid"].File.Name.ShouldBe("name");
            output.Data["cid"].File.Url.ShouldBe("url");
        }
        {
            var output = await DAOContractStub.GetMetadata.CallAsync(daoId);
            output.Name.ShouldBe("TestDAO");
            output.LogoUrl.ShouldBe("logo_url");
            output.Description.ShouldBe("Description");
            output.SocialMedia.Count.ShouldBe(5);
            output.SocialMedia["X"].ShouldBe("twitter");
            output.SocialMedia["Facebook"].ShouldBe("facebook");
            output.SocialMedia["Telegram"].ShouldBe("telegram");
            output.SocialMedia["Discord"].ShouldBe("discord");
            output.SocialMedia["Reddit"].ShouldBe("reddit");
        }
        {
            var output = await DAOContractStub.GetGovernanceToken.CallAsync(daoId);
            output.Value.ShouldBe("ELF");
        }
        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }
        {
            var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                What = "SetPermissions",
                Who = DefaultAddress
            });
            output.Value.ShouldBeTrue();
        }
        // {
        //     var output = await DAOContractStub.GetHighCouncilStatus.CallAsync(daoId);
        //     output.Value.ShouldBeFalse();
        // }
    }

    [Fact]
    public async Task CreateDAOTests_Fail()
    {
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput());
            result.TransactionResult.Error.ShouldContain("Invalid metadata.");
        }
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata()
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata name.");
        }
        {
            var name = "";
            for (var i = 0; i < 51; i++)
            {
                name += "A";
            }

            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = name
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata name.");
        }
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO"
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata logo url.");
        }
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url"
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata description.");
        }
        {
            var description = "";
            for (var i = 0; i < 241; i++)
            {
                description += "A";
            }

            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = description
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata description.");
        }
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = "description"
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata social media count.");
        }
        {
            var socialMedia = new Dictionary<string, string>();
            for (var i = 0; i < 21; i++)
            {
                socialMedia.Add(i.ToString(), "url");
            }

            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = "description",
                    SocialMedia = { socialMedia }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata social media count.");
        }
        {
            var url = "";
            for (var i = 0; i < 65; i++)
            {
                url += "A";
            }

            var socialMedia = new Dictionary<string, string> { { "x", url } };

            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = "description",
                    SocialMedia = { socialMedia }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid social media url.");
        }
        {
            var socialMedia = new Dictionary<string, string> { { "x", "url" } };

            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = "description",
                    SocialMedia = { socialMedia }
                },
                GovernanceToken = "TEST"
            });
            result.TransactionResult.Error.ShouldContain("Token not found.");
        }

        await CreateDAOAsync();

        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO"
                }
            });
            result.TransactionResult.Error.ShouldContain("DAO name already exists.");
        }
    }

    [Fact]
    public async Task SetSubsistStatusTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }

        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "SetSubsistStatus",
            PermissionType.Specificaddress);

        {
            var result = await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubsistStatusSet>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.Status.ShouldBeFalse();

            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeFalse();
        }
        {
            var result = await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains("SubsistStatusSet"));
            log.ShouldBeNull();
        }
    }
}