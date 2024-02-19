using System.Linq;
using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty SetPermissions(SetPermissionsInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        
        CheckDAOExists(input.DaoId);
        CheckDaoSubsistStatus(input.DaoId);
        
        AssertPermission(input.DaoId, nameof(SetPermissions));
        Assert(input.PermissionInfos != null && input.PermissionInfos.Count > 0, "Invalid input permission infos.");

        ProcessPermissions(input.DaoId, input.PermissionInfos);

        return new Empty();
    }

    private Hash CalculatePermissionHash(Address where, string what)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(where), HashHelper.ComputeFrom(what));
    }

    private void ProcessPermissions(Hash daoId, RepeatedField<PermissionInfo> permissionInfos)
    {
        if (permissionInfos.Count == 0) return;

        var permissionList = new PermissionInfoList();
        var permissionInfoList = permissionInfos.Distinct();

        foreach (var permissionInfo in permissionInfoList)
        {
            ValidatePermissionInfo(permissionInfo);

            var permissionHash = CalculatePermissionHash(permissionInfo.Where, permissionInfo.What.ToLower());

            UpdatePermission(daoId, permissionHash, permissionInfo, permissionList);
        }

        if (permissionList.PermissionInfos.Count == 0) return;

        Context.Fire(new PermissionsSet
        {
            DaoId = daoId,
            Here = Context.Sender,
            PermissionInfoList = permissionList
        });
    }

    private void UpdatePermission(Hash daoId, Hash permissionHash, PermissionInfo permissionInfo,
        PermissionInfoList permissionList)
    {
        var oldPermissionType = State.PermissionTypeMap[daoId][permissionHash];

        if (oldPermissionType == permissionInfo.PermissionType &&
            oldPermissionType != PermissionType.Specificaddress) return;

        // Remove old permission if has specific address
        if (oldPermissionType == PermissionType.Specificaddress)
        {
            if (State.PermissionSpecificAddressMap[daoId][permissionHash] == permissionInfo.Who) return;
            State.PermissionSpecificAddressMap[daoId].Remove(permissionHash);
        }

        State.PermissionTypeMap[daoId][permissionHash] = permissionInfo.PermissionType;

        if (permissionInfo.PermissionType == PermissionType.Specificaddress)
        {
            State.PermissionSpecificAddressMap[daoId][permissionHash] = permissionInfo.Who;
        }

        permissionList.PermissionInfos.Add(permissionInfo);
    }

    private void ValidatePermissionInfo(PermissionInfo permissionInfo)
    {
        Assert(IsAddressValid(permissionInfo.Where), "Invalid input permission info where.");
        Assert(IsStringValid(permissionInfo.What) && permissionInfo.What.Length <= DAOContractConstants.MaxWhatLength,
            "Invalid input permission info what.");
        Assert(
            permissionInfo.PermissionType != PermissionType.Specificaddress || IsAddressValid(permissionInfo.Who),
            "Invalid input permission info who.");
    }

    private bool IsGranted(Hash daoId, Address where, string what, Address who)
    {
        var permissionHash = CalculatePermissionHash(where, what);
        var permissionType = State.PermissionTypeMap[daoId][permissionHash];

        return permissionType switch
        {
            PermissionType.None => false,
            PermissionType.Everyone => true,
            PermissionType.Highcouncilonly => CheckHighCouncilPermissionGranted(daoId, who),
            PermissionType.Specificaddress => State.PermissionSpecificAddressMap[daoId][permissionHash] == who,
            _ => false
        };
    }

    private bool CheckHighCouncilPermissionGranted(Hash daoId, Address who)
    {
        // TODO need high council member list
        return State.DAOInfoMap[daoId].Creator == who;
    }

    private void AssertPermission(Hash daoId, string what)
    {
        Assert(IsGranted(daoId, Context.Self, what.ToLower(), Context.Sender),
            $"Permission of {what} is not granted for {Context.Sender}.");
    }
}