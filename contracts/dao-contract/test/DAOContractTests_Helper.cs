using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Shouldly;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    private T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private async Task InitializeAsync()
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

    private async Task<Hash> CreateDAOAsync()
    {
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

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);

        return log.DaoId;
    }

    private async Task SetPermissionAsync(Hash daoId, Address where, Address who, string what,
        PermissionType permissionType)
    {
        var result = await DAOContractStub.SetPermissions.SendAsync(new SetPermissionsInput
        {
            DaoId = daoId,
            PermissionInfos =
            {
                new PermissionInfo
                {
                    PermissionType = permissionType,
                    Where = where,
                    Who = who,
                    What = what
                }
            }
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task SetSubsistStatusAsync(Hash daoId, bool status)
    {
        await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
        {
            DaoId = daoId,
            Status = status
        });
    }
}