using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty EnableHighCouncil(EnableHighCouncilInput input)
    {
        // Assert(input != null, "Invalid input.");
        // Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        // Assert(State.HighCouncilEnabledStatusMap[input.DaoId] == false, "High council already enabled.");
        //
        // CheckDAOExists(input.DaoId);
        // CheckDaoSubsistStatus(input.DaoId);
        // AssertPermission(input.DaoId, nameof(EnableHighCouncil));
        //
        // ProcessEnableHighCouncil(input.DaoId, input.HighCouncilConfig, input.ExecutionConfig);

        return new Empty();
    }

    private void ProcessEnableHighCouncil(Hash daoId, HighCouncilConfig highCouncilConfig, bool executionConfig)
    {
        State.HighCouncilEnabledStatusMap[daoId] = true;
        State.HighCouncilExecutionConfigMap[daoId] = executionConfig;

        // TODO call election contract

        Context.Fire(new HighCouncilEnabled
        {
            DaoId = daoId,
            ExecutionConfig = executionConfig,
            HighCouncilAddress = State.HighCouncilAddressMap[daoId]
        });
    }

    // private void AssertHighCouncilConfig(long maxHighCouncilMemberCount, long maxHighCouncilCandidateCount,
    //     long electionPeriod)
    // {
    //     Assert(
    //         maxHighCouncilMemberCount > 0 &&
    //         maxHighCouncilMemberCount <= DAOContractConstants.MaxHighCouncilMemberCount,
    //         "Invalid max high council member count.");
    //     Assert(
    //         maxHighCouncilCandidateCount > maxHighCouncilMemberCount &&
    //         maxHighCouncilCandidateCount <= DAOContractConstants.MaxHighCouncilCandidateCount,
    //         "Invalid max high council candidate count.");
    //     Assert(electionPeriod > 0 && electionPeriod <= DAOContractConstants.MaxElectionPeriod,
    //         "Invalid election period.");
    // }

    public override Empty DisableHighCouncil(Hash input)
    {
        // Assert(input != null, "Invalid input.");
        // Assert(IsHashValid(input), "Invalid input dao id.");
        // Assert(State.HighCouncilEnabledStatusMap[input] == true, "High council already disabled.");
        //
        // CheckDAOExists(input);
        // CheckDaoSubsistStatus(input);
        // AssertPermission(input, nameof(EnableHighCouncil));
        //
        // State.HighCouncilEnabledStatusMap[input] = false;
        //
        // Context.Fire(new HighCouncilDisabled
        // {
        //     DaoId = input
        // });
        //
        //
        return new Empty();
    }

    // public override Empty SetHighCouncilConfig(SetHighCouncilConfigInput input)
    // {
    //     Assert(input != null, "Invalid input.");
    //     Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
    //     CheckDaoSubsisted(input.DaoId);
    //     AssertPermission(input.DaoId, nameof(SetHighCouncilConfig));
    //     Assert(State.HighCouncilEnabledStatusMap[input.DaoId], "High council not enabled.");
    //
    //     AssertHighCouncilConfig(input.MaxHighCouncilMemberCount, input.MaxHighCouncilCandidateCount,
    //         input.ElectionPeriod);
    //
    //     var highCouncilConfig = State.HighCouncilConfigMap[input.DaoId];
    //
    //     if (highCouncilConfig.MaxHighCouncilMemberCount == input.MaxHighCouncilMemberCount &&
    //         highCouncilConfig.MaxHighCouncilCandidateCount == input.MaxHighCouncilCandidateCount &&
    //         highCouncilConfig.ElectionPeriod == input.ElectionPeriod)
    //     {
    //         return new Empty();
    //     }
    //
    //     highCouncilConfig.MaxHighCouncilMemberCount = input.MaxHighCouncilMemberCount;
    //     highCouncilConfig.MaxHighCouncilCandidateCount = input.MaxHighCouncilCandidateCount;
    //     highCouncilConfig.ElectionPeriod = input.ElectionPeriod;
    //
    //     Context.Fire(new HighCouncilConfigSet
    //     {
    //         DaoId = input.DaoId,
    //         HighCouncilConfig = highCouncilConfig
    //     });
    //
    //     return new Empty();
    // }

    public override Empty SetHighCouncilExecutionConfig(SetHighCouncilExecutionConfigInput input)
    {
        // Assert(input != null, "Invalid input.");
        // Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        // CheckDAOExists(input.DaoId);
        // CheckDaoSubsistStatus(input.DaoId);
        // AssertPermission(input.DaoId, nameof(SetHighCouncilExecutionConfig));
        // Assert(State.HighCouncilEnabledStatusMap[input.DaoId], "High council not enabled.");
        //
        // if (State.HighCouncilExecutionConfigMap[input.DaoId] == input.ExecutionConfig)
        // {
        //     return new Empty();
        // }
        //
        // State.HighCouncilExecutionConfigMap[input.DaoId] = input.ExecutionConfig;
        //
        // Context.Fire(new HighCouncilExecutionConfigSet
        // {
        //     DaoId = input.DaoId,
        //     ExecutionConfig = input.ExecutionConfig
        // });
        //
        return new Empty();
    }
}