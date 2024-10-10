using AElf.Scripts;
using AElf.Types;

namespace DeployTomorrowDaoSuite;

public class QueryContract : Script
{
    public override async Task RunAsync()
    {
        var reg = await Genesis.GetSmartContractRegistrationByCodeHash.CallAsync(
            Hash.LoadFromHex("f0d6aada9c515b8d14ba1fae1d8b24932004b569bebabebbba007442974889c1"));
        
        Console.WriteLine($"Reg is : {reg}");
    }
}