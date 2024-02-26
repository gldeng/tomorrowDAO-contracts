using AElf;
using AElf.Types;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private Hash CalculatePermissionHash(Address where, string what)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(where), HashHelper.ComputeFrom(what));
    }

    private void ProcessPermission(Hash daoId, PermissionInfo info, PermissionType type)
    {
        var permissionHash = CalculatePermissionHash(info.Where, info.What);
        State.PermissionTypeMap[daoId][permissionHash] = type;
    }

    private bool IsGranted(Hash daoId, Address where, string what, Address who)
    {
        var permissionHash = CalculatePermissionHash(where, what);
        var permissionType = State.PermissionTypeMap[daoId][permissionHash];

        return permissionType switch
        {
            PermissionType.Default => State.ReferendumContractMap[daoId] == who ||
                                      State.HighCouncilContractMap[daoId] == who,
            PermissionType.Creator => State.DAOInfoMap[daoId].Creator == who,
            _ => false
        };
    }

    private void AssertPermission(Hash daoId, string what)
    {
        Assert(IsGranted(daoId, Context.Self, what.ToLower(), Context.Sender),
            $"Permission of {what} is not granted for {Context.Sender}.");
    }
}