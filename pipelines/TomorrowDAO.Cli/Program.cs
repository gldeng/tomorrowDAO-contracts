using AElf.Scripts;
using AElf.Scripts.Predefined;
using TomorrowDAO.Pipelines.AnonymousVoteDeployment;
using TomorrowDAO.Pipelines.DevChainSetup;
using TomorrowDAO.Pipelines.InitialDeployment;

// Environment.SetEnvironmentVariable(EnvVarNames.AELF_RPC_URL.ToString(), "http://34.42.39.92:34197");
// Environment.SetEnvironmentVariable(EnvVarNames.DEPLOYER_PRIVATE_KEY.ToString(),
//     "1111111111111111111111111111111111111111111111111111111111111111");

{
    // For devnet only, may already initialized, it's ok to fail
    try
    {
        await new InitScript().RunAsync();
    }
    catch
    {
        // Do Nothing
    }
}
{
    var deploy = new V1DeploymentScript();
    await deploy.RunAsync();
}
{
    var deploy = new AnonymousVoteDeploymentScript();
    await deploy.RunAsync();
}
{
    var pipeline = new DevChainTestScript();
    await pipeline.RunAsync();
}