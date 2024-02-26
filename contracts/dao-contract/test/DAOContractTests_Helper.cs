using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            GovernanceToken = "ELF"
        });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);

        return log.DaoId;
    }

    private async Task SetSubsistStatusAsync(Hash daoId, bool status)
    {
        await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
        {
            DaoId = daoId,
            Status = status
        });
    }

    private string GenerateRandomString(int length)
    {
        if (length <= 0) return "";

        const string chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-*/!@#$%^&*()_+{}|:<>?[];',./`~";

        var random = new Random();

        var stringBuilder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    private Dictionary<string, string> GenerateRandomMap(int length, int keyLength, int valueLength)
    {
        var map = new Dictionary<string, string>();
        for (var i = 0; i < length; i++)
        {
            var key = GenerateRandomString(keyLength);
            if (map.ContainsKey(key))
            {
                i--;
                continue;
            }
            var value = GenerateRandomString(valueLength);

            map.Add(key, value);
        }

        return map;
    }
    
    private File GenerateFile(string cid, string name, string url)
    {
        return new File
        {
            Cid = cid,
            Name = name,
            Url = url
        };
    }
}