using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Fact]
    public async Task SetPermissionsTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var result = await DAOContractStub.SetPermissions.SendAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        PermissionType = PermissionType.Everyone,
                        Where = DAOContractAddress,
                        Who = DefaultAddress,
                        What = "uploadfileinfos"
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<PermissionsSet>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.Here.ShouldBe(DefaultAddress);
            log.PermissionInfoList.PermissionInfos.Count.ShouldBe(1);
            log.PermissionInfoList.PermissionInfos[0].PermissionType.ShouldBe(PermissionType.Everyone);
            log.PermissionInfoList.PermissionInfos[0].Where.ShouldBe(DAOContractAddress);
            log.PermissionInfoList.PermissionInfos[0].Who.ShouldBe(DefaultAddress);
            log.PermissionInfoList.PermissionInfos[0].What.ShouldBe("uploadfileinfos");

            var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = UserAddress,
                What = "uploadfileinfos"
            });
            output.Value.ShouldBeTrue();
        }
        {
            var result = await DAOContractStub.SetPermissions.SendAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        PermissionType = PermissionType.Specificaddress,
                        Where = DAOContractAddress,
                        Who = DefaultAddress,
                        What = "uploadfileinfos"
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = UserAddress,
                What = "uploadfileinfos"
            });
            output.Value.ShouldBeFalse();
        }
        {
            var result = await DAOContractStub.SetPermissions.SendAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        PermissionType = PermissionType.Specificaddress,
                        Where = DAOContractAddress,
                        Who = DefaultAddress,
                        What = "uploadfileinfos"
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(PermissionsSet)));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task SetPermissionsTests_Fail()
    {
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = HashHelper.ComputeFrom("test")
            });
            result.TransactionResult.Error.ShouldContain("DAO not existed.");
        }

        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "SetSubsistStatus",
            PermissionType.Specificaddress);
        await SetSubsistStatusAsync(daoId, false);

        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("DAO not subsisted.");
        }

        await SetSubsistStatusAsync(daoId, true);

        {
            var result = await UserDAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Permission of SetPermissions is not granted for");
        }
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input permission infos.");
        }
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos = { new PermissionInfo() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input permission info where.");
        }
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        Where = DAOContractAddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input permission info what.");
        }
        {
            var what = "";
            for (var i = 0; i < 65; i++)
            {
                what += "a";
            }
            
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        Where = DAOContractAddress,
                        What = what
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input permission info what.");
        }
        {
            var result = await DAOContractStub.SetPermissions.SendWithExceptionAsync(new SetPermissionsInput
            {
                DaoId = daoId,
                PermissionInfos =
                {
                    new PermissionInfo
                    {
                        Where = DAOContractAddress,
                        What = "UploadFileInfos",
                        PermissionType = PermissionType.Specificaddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input permission info who.");
        }
    }
}