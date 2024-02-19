using System.Collections.Generic;
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
    public async Task UploadFileInfosTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();
        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "UploadFileInfos",
            PermissionType.Specificaddress);

        {
            var result = await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid2",
                        Name = "name2",
                        Url = "url2"
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.UploadedFiles.Data.Count.ShouldBe(1);
            log.UploadedFiles.Data["cid2"].File.Cid.ShouldBe("cid2");
            log.UploadedFiles.Data["cid2"].File.Name.ShouldBe("name2");
            log.UploadedFiles.Data["cid2"].File.Url.ShouldBe("url2");
            log.UploadedFiles.Data["cid2"].Uploader.ShouldBe(DefaultAddress);
            log.UploadedFiles.Data["cid2"].UploadTime.ShouldNotBeNull();

            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(2);
            output.Data["cid2"].File.Cid.ShouldBe("cid2");
            output.Data["cid2"].File.Name.ShouldBe("name2");
            output.Data["cid2"].File.Url.ShouldBe("url2");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid2",
                        Name = "name2",
                        Url = "url2"
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(FileInfosUploaded)));

            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task UploadFileInfosTests_Fail()
    {
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
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
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("DAO not subsisted.");
        }

        await SetSubsistStatusAsync(daoId, true);

        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "UploadFileInfos",
            PermissionType.Specificaddress);

        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input files.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input files.");
        }
        {
            var files = new List<File>();
            for (var i = 0; i < 21; i++)
            {
                files.Add(new File());
            }

            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { files }
            });
            result.TransactionResult.Error.ShouldContain("Too many files.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { new File() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
        {
            var cid = "";
            for (var i = 0; i < 65; i++)
            {
                cid += "a";
            }

            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = cid
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid"
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file name.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid"
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file name.");
        }
        {
            var name = "";
            for (var i = 0; i < 129; i++)
            {
                name += "a";
            }
            
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid",
                        Name = name
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file name.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid",
                        Name = "name"
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file url.");
        }
        {
            var url = "";
            for (int i = 0; i < 257; i++)
            {
                url += "a";
            }
            
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid",
                        Name = "name",
                        Url = url
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file url.");
        }
        {
            var files = new List<File>();
            for (var i = 0; i < 19; i++)
            {
                files.Add(new File
                {
                    Cid = i.ToString(),
                    Name = "name",
                    Url = "url"
                });
            }

            await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { files }
            });

            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files =
                {
                    new File
                    {
                        Cid = "cid",
                        Name = "name",
                        Url = "url"
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Too many files.");
        }
    }

    [Fact]
    public async Task RemoveFileInfosTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();
        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "RemoveFileInfos",
            PermissionType.Specificaddress);
        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<FileInfosRemoved>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.RemovedFiles.Data.Count.ShouldBe(1);
            log.RemovedFiles.Data["cid"].File.Cid.ShouldBe("cid");
            log.RemovedFiles.Data["cid"].File.Name.ShouldBe("name");
            log.RemovedFiles.Data["cid"].File.Url.ShouldBe("url");

            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(0);
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid2" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(FileInfosRemoved)));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task RemoveFileInfosTests_Fail()
    {
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
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
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("DAO not subsisted.");
        }

        await SetSubsistStatusAsync(daoId, true);

        await SetPermissionAsync(daoId, DAOContractAddress, DefaultAddress, "RemoveFileInfos",
            PermissionType.Specificaddress);

        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        {
            var cids = new List<string>();
            for (int i = 0; i < 21; i++)
            {
                cids.Add(i.ToString());
            }
            
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { cids }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        
        {
            var cids = new List<string>();
            var cid = "";
            for (int i = 0; i < 65; i++)
            {
                cid += "a";
            }
            cids.Add(cid);
            
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { cids }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
    }
}