﻿using AElf.Scripts;
using AElf.Scripts.Predefined;
using DeployTomorrowDaoSuite;
using TomorrowDAO.Pipelines.InitialDeployment;

Environment.SetEnvironmentVariable(EnvVarNames.AELF_RPC_URL.ToString(), "http://34.27.181.65:8000");
Environment.SetEnvironmentVariable(EnvVarNames.DEPLOYER_PRIVATE_KEY.ToString(),
    "1111111111111111111111111111111111111111111111111111111111111111");

{
    await new InitScript().RunAsync();

    var deploy = new Pipeline();
    await deploy.RunAsync();

    Console.WriteLine($"Deployed Governance at: {deploy.DeployGovernance.DeployedAddress}");
    Console.WriteLine($"Deployed Dao        at: {deploy.DeployDao.DeployedAddress}");
    Console.WriteLine($"Deployed Election   at: {deploy.DeployElection.DeployedAddress}");
    Console.WriteLine($"Deployed Treasury   at: {deploy.DeployTreasury.DeployedAddress}");
    Console.WriteLine($"Deployed Vote       at: {deploy.DeployVote.DeployedAddress}");
}
{
    var n = new TomorrowDAO.Pipelines.AnonymousVoteDeployment.Pipeline();
    await n.RunAsync();
}

{
    var pipeline =new  TomorrowDAO.Pipelines.DevChainSetup.Pipeline();
    await pipeline.RunAsync();
}


// {
//     var temp = new QueryContract();
//     await temp.RunAsync();
// }