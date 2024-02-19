// using System.Threading.Tasks;
// using AElf.Types;
// using Shouldly;
// using Xunit;
//
// namespace TomorrowDAO.Contracts.DAO;
//
// public partial class DAOContractTests
// {
//     [Fact]
//     public async Task EnableHighCouncilTests()
//     {
//         await InitializeAsync();
//         var daoId = await CreateDAOAsync();
//
//         await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "EnableHighCouncil",
//             PermissionType.Highcouncilonly);
//
//         {
//             var result = await DAOContractStub.EnableHighCouncil.SendAsync(new EnableHighCouncilInput
//             {
//                 DaoId = daoId,
//                 ExecutionConfig = true,
//                 HighCouncilConfig = new HighCouncilConfig()
//             });
//             result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//
//             var log = GetLogEvent<HighCouncilEnabled>(result.TransactionResult);
//             log.DaoId.ShouldBe(daoId);
//             log.ExecutionConfig.ShouldBeTrue();
//
//             {
//                 var output = await DAOContractStub.GetHighCouncilStatus.CallAsync(daoId);
//                 output.Value.ShouldBeTrue();
//             }
//             {
//                 var output = await DAOContractStub.GetHighCouncilExecutionConfig.CallAsync(daoId);
//                 output.Value.ShouldBeTrue();
//             }
//             {
//                 var output = await DAOContractStub.GetHighCouncilAddress.CallAsync(daoId);
//                 output.ShouldBe(new Address());
//             }
//         }
//         {
//             var result = await DAOContractStub.DisableHighCouncil.SendAsync(daoId);
//             result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//
//             {
//                 var output = await DAOContractStub.GetHighCouncilStatus.CallAsync(daoId);
//                 output.Value.ShouldBeFalse();
//             }
//             {
//                 var output = await DAOContractStub.GetHighCouncilExecutionConfig.CallAsync(daoId);
//                 output.Value.ShouldBeFalse();
//             }
//             {
//                 var output = await DAOContractStub.GetHighCouncilAddress.CallAsync(daoId);
//                 output.ShouldBe(new Address());
//             }
//         }
//     }
// }