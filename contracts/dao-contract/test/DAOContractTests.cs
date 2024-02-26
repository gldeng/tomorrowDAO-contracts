using System.Linq;
using System.Threading.Tasks;
using AElf;
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
        var result = await DAOContractStub.Initialize.SendAsync(new InitializeInput
        {
            GovernanceContractAddress = DefaultAddress,
            ElectionContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress,
            VoteContractAddress = DefaultAddress,
            TimelockContractAddress = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var output = await DAOContractStub.GetInitializedContracts.CallAsync(new Empty());
        output.GovernanceContractAddress.ShouldBe(DefaultAddress);
        output.ElectionContractAddress.ShouldBe(DefaultAddress);
        output.TreasuryContractAddress.ShouldBe(DefaultAddress);
        output.VoteContractAddress.ShouldBe(DefaultAddress);
        output.TimelockContractAddress.ShouldBe(DefaultAddress);
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
        var daoName = GenerateRandomString(50);
        var daoLogoUrl = GenerateRandomString(256);
        var daoDescription = GenerateRandomString(240);
        var socialMedias = GenerateRandomMap(20, 16, 64);
        var file = GenerateFile("cid", "name", "url");
        var file2 = GenerateFile("cid2", "name2", "url2");

        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = daoLogoUrl,
                Description = daoDescription,
                SocialMedia = { socialMedias }
            },
            GovernanceToken = "",
            Files =
            {
                file, file2
            }
        });

        Hash daoId;

        {
            var log = GetLogEvent<DAOCreated>(result.TransactionResult);
            log.DaoId.ShouldNotBeNull();
            log.Metadata.Name.ShouldBe(daoName);
            log.Metadata.LogoUrl.ShouldBe(daoLogoUrl);
            log.Metadata.Description.ShouldBe(daoDescription);
            log.Metadata.SocialMedia.Count.ShouldBe(socialMedias.Count);
            log.Metadata.SocialMedia.ShouldBe(socialMedias);
            log.GovernanceToken.ShouldBe("");
            log.Creator.ShouldBe(DefaultAddress);
            log.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.ElectionContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.TreasuryContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.VoteContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.TimelockContractAddress.ShouldBe(DefaultAddress);

            daoId = log.DaoId;
        }
        {
            var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.UploadedFiles.Data.Count.ShouldBe(2);
            log.UploadedFiles.Data[file.Cid].File.ShouldBe(file);
            log.UploadedFiles.Data[file2.Cid].File.ShouldBe(file2);
        }
        {
            var output = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
            output.Creator.ShouldBe(DefaultAddress);
            output.SubsistStatus.ShouldBeTrue();
            output.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.ElectionContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.TreasuryContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.VoteContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.TimelockContractAddress.ShouldBe(DefaultAddress);
            output.DaoId.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetDAOIdByName.CallAsync(new StringValue
            {
                Value = daoName
            });
            output.Value.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(2);
            output.Data[file.Cid].File.ShouldBe(file);
            output.Data[file.Cid].Uploader.ShouldBe(DefaultAddress);
            output.Data[file.Cid].UploadTime.ShouldNotBeNull();
            output.Data[file2.Cid].File.ShouldBe(file2);
            output.Data[file2.Cid].Uploader.ShouldBe(DefaultAddress);
            output.Data[file2.Cid].UploadTime.ShouldNotBeNull();
        }
        {
            var output = await DAOContractStub.GetMetadata.CallAsync(daoId);
            output.Name.ShouldBe(daoName);
            output.LogoUrl.ShouldBe(daoLogoUrl);
            output.Description.ShouldBe(daoDescription);
            output.SocialMedia.Count.ShouldBe(socialMedias.Count);
            output.SocialMedia.ShouldBe(socialMedias);
        }
        {
            var output = await DAOContractStub.GetGovernanceToken.CallAsync(daoId);
            output.Value.ShouldBeEmpty();
        }
        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }
        // {
        //     var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission.Where,
        //         What = permission.What,
        //         Who = DefaultAddress
        //     });
        //     output.Value.ShouldBeFalse();
        // }
        // {
        //     var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission2.Where,
        //         What = permission2.What,
        //         Who = UserAddress
        //     });
        //     output.Value.ShouldBeTrue();
        //     output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission2.Where,
        //         What = permission2.What,
        //         Who = UserAddress
        //     });
        //     output.Value.ShouldBeTrue();
        // }
        // {
        //     var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission3.Where,
        //         What = permission3.What,
        //         Who = UserAddress
        //     });
        //     output.Value.ShouldBeFalse();
        //     output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission3.Where,
        //         What = permission3.What,
        //         Who = DefaultAddress
        //     });
        //     output.Value.ShouldBeTrue();
        // }
        // {
        //     var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission4.Where,
        //         What = permission4.What,
        //         Who = User2Address
        //     });
        //     output.Value.ShouldBeTrue();
        //     output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
        //     {
        //         DaoId = daoId,
        //         Where = permission4.Where,
        //         What = permission4.What,
        //         Who = DefaultAddress
        //     });
        //     output.Value.ShouldBeFalse();
        // }

        daoName = GenerateRandomString(49);
        daoLogoUrl = GenerateRandomString(255);
        daoDescription = GenerateRandomString(239);
        socialMedias = GenerateRandomMap(19, 15, 63);

        result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = daoLogoUrl,
                Description = daoDescription,
                SocialMedia = { socialMedias }
            },
            GovernanceToken = "ELF"
        });

        {
            var log = GetLogEvent<DAOCreated>(result.TransactionResult);
            log.DaoId.ShouldNotBeNull();
            log.Metadata.Name.ShouldBe(daoName);
            log.Metadata.LogoUrl.ShouldBe(daoLogoUrl);
            log.Metadata.Description.ShouldBe(daoDescription);
            log.Metadata.SocialMedia.Count.ShouldBe(socialMedias.Count);
            log.Metadata.SocialMedia.ShouldBe(socialMedias);
            log.GovernanceToken.ShouldBe("ELF");
            log.Creator.ShouldBe(DefaultAddress);
            log.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.ElectionContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.TreasuryContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.VoteContractAddress.ShouldBe(DefaultAddress);
            log.ContractAddressList.TimelockContractAddress.ShouldBe(DefaultAddress);

            daoId = log.DaoId;
        }
        {
            var output = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
            output.Creator.ShouldBe(DefaultAddress);
            output.SubsistStatus.ShouldBeTrue();
            output.ContractAddressList.GovernanceContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.ElectionContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.TreasuryContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.VoteContractAddress.ShouldBe(DefaultAddress);
            output.ContractAddressList.TimelockContractAddress.ShouldBe(DefaultAddress);
            output.DaoId.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetDAOIdByName.CallAsync(new StringValue
            {
                Value = daoName
            });
            output.Value.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.ShouldBeEmpty();
        }
        {
            var output = await DAOContractStub.GetMetadata.CallAsync(daoId);
            output.Name.ShouldBe(daoName);
            output.LogoUrl.ShouldBe(daoLogoUrl);
            output.Description.ShouldBe(daoDescription);
            output.SocialMedia.Count.ShouldBe(socialMedias.Count);
            output.SocialMedia.ShouldBe(socialMedias);
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
                Where = DefaultAddress,
                What = "Function",
                Who = DefaultAddress
            });
            output.Value.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task CreateDAOTests_Fail()
    {
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await InitializeAsync();

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

    [Theory]
    [InlineData(0, 0, 0, "Invalid metadata name.")]
    [InlineData(51, 0, 0, "Invalid metadata name.")]
    [InlineData(5, 0, 0, "Invalid metadata logo url.")]
    [InlineData(5, 257, 0, "Invalid metadata logo url.")]
    [InlineData(5, 5, 0, "Invalid metadata description.")]
    [InlineData(5, 5, 241, "Invalid metadata description.")]
    public async Task CreateDAOTests_Metadata_Fail(int nameLength, int logoUrlLength, int descriptionLength,
        string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = GenerateRandomString(nameLength),
                LogoUrl = GenerateRandomString(logoUrlLength),
                Description = GenerateRandomString(descriptionLength)
            }
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Theory]
    [InlineData(0, 1, 1, "Invalid metadata social media count.")]
    [InlineData(21, 1, 1, "Invalid metadata social media count.")]
    [InlineData(1, 0, 0, "Invalid metadata social media name.")]
    [InlineData(1, 17, 0, "Invalid metadata social media name.")]
    [InlineData(1, 5, 0, "Invalid metadata social media url.")]
    [InlineData(1, 5, 241, "Invalid metadata social media url.")]
    public async Task CreateDAOTests_SocialMedia_Fail(int count, int keyLength, int valueLength, string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "name",
                LogoUrl = "logo",
                Description = "des",
                SocialMedia = { GenerateRandomMap(count, keyLength, valueLength) }
            }
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Theory]
    [InlineData("ABCdE", "Invalid token symbol.")]
    [InlineData("ABCDEFGHIJK", "Invalid token symbol.")]
    [InlineData("TEST", "Token not found.")]
    [InlineData("ELF-1", "Invalid token symbol.")]
    public async Task CreateDAOTests_GovernanceToken_Fail(string symbol, string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "name",
                LogoUrl = "logo",
                Description = "des",
                SocialMedia = { GenerateRandomMap(1, 1, 1) }
            },
            GovernanceToken = symbol
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
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

        // already subsist
        {
            var result = await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = true
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains("SubsistStatusSet"));
            log.ShouldBeNull();
            
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }
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
            
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task SetSubsistStatusTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput
            {
                DaoId = HashHelper.ComputeFrom("test"),
                Status = false
            });
            result.TransactionResult.Error.ShouldContain("DAO not existed.");
        }

        var daoId = await CreateDAOAsync();

        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            });
            result.TransactionResult.Error.ShouldContain("Permission of SetSubsistStatus is not granted");
        }
    }
}