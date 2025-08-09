using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Resolvers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using static ReportOnFailure.Tests.Reporters.CustomReporterTests;

namespace ReportOnFailure.Tests.Resolvers;

public class CustomResolverTests
{
    #region Custom File Resolver Tests

    [Fact]
    public async Task CustomFileResolver_ResolveAsync_ReturnsFileListingAsJson()
    {

        var tempDir = CreateTempDirectoryWithFiles();
        var resolver = new CustomFileResolver();
        var reporter = new CustomFileReporter()
            .WithDirectoryPath(tempDir)
            .WithSearchPattern("*.txt")
            .WithRecursive(false)
            .WithResultsFormat(ResultsFormat.Json);

        try
        {

            var result = await resolver.ResolveAsync(reporter);


            Assert.NotNull(result);
            Assert.Contains("\"Name\":", result);
            Assert.Contains("\"Size\":", result);
            Assert.Contains("\"LastModified\":", result);
            Assert.Contains("test1.txt", result);
            Assert.Contains("test2.txt", result);

            var fileList = JsonSerializer.Deserialize<List<FileInfoData>>(result);
            Assert.NotNull(fileList);
            Assert.Equal(2, fileList.Count);
        }
        finally
        {

            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void CustomFileResolver_ResolveSync_ReturnsFileListingAsJson()
    {

        var tempDir = CreateTempDirectoryWithFiles();
        var resolver = new CustomFileResolver();
        var reporter = new CustomFileReporter()
            .WithDirectoryPath(tempDir)
            .WithSearchPattern("*.*")
            .WithRecursive(true);

        try
        {

            var result = resolver.ResolveSync(reporter);


            Assert.NotNull(result);
            Assert.Contains("\"Name\":", result);
            Assert.Contains("test1.txt", result);
            Assert.Contains("test2.txt", result);
            Assert.Contains("nested.log", result);
        }
        finally
        {

            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public async Task CustomFileResolver_WithNonExistentDirectory_ReturnsEmptyResult()
    {

        var resolver = new CustomFileResolver();
        var reporter = new CustomFileReporter()
            .WithDirectoryPath(@"C:\NonExistentDirectory")
            .WithSearchPattern("*.*");


        var result = await resolver.ResolveAsync(reporter);


        Assert.NotNull(result);
        Assert.Contains("Directory not found", result);
    }

    #endregion

    #region Custom Memory Resolver Tests

    [Fact]
    public async Task CustomMemoryResolver_ResolveAsync_ReturnsMemoryInfo()
    {

        var resolver = new CustomMemoryResolver();
        var memoryData = new Dictionary<string, object>
        {
            ["TotalMemory"] = 8192000,
            ["AvailableMemory"] = 4096000,
            ["UsedMemory"] = 4096000
        };

        var reporter = new CustomMemoryReporter()
            .WithMemoryData(memoryData)
            .WithIncludeGCInfo(true);


        var result = await resolver.ResolveAsync(reporter);


        Assert.NotNull(result);
        Assert.Contains("TotalMemory", result);
        Assert.Contains("8192000", result);
        Assert.Contains("GC", result);
    }

    [Fact]
    public void CustomMemoryResolver_ResolveSync_WithoutGCInfo_ExcludesGCData()
    {

        var resolver = new CustomMemoryResolver();
        var memoryData = new Dictionary<string, object>
        {
            ["WorkingSet"] = 1024000
        };

        var reporter = new CustomMemoryReporter()
            .WithMemoryData(memoryData)
            .WithIncludeGCInfo(false);


        var result = resolver.ResolveSync(reporter);


        Assert.NotNull(result);
        Assert.Contains("WorkingSet", result);
        Assert.DoesNotContain("GC", result);
    }

    #endregion

    #region Custom Process Resolver Tests

    [Fact]
    public async Task CustomProcessResolver_ResolveAsync_ReturnsProcessInfo()
    {

        var resolver = new CustomProcessResolver();
        var reporter = new CustomProcessReporter()
            .WithProcessName("dotnet")
            .WithIncludeChildProcesses(false)
            .WithIncludeThreads(true);


        var result = await resolver.ResolveAsync(reporter);


        Assert.NotNull(result);
        Assert.Contains("ProcessName", result);
        Assert.Contains("dotnet", result);
        Assert.Contains("Id", result);
    }

    [Fact]
    public void CustomProcessResolver_ResolveSync_WithNonExistentProcess_ReturnsNotFoundMessage()
    {

        var resolver = new CustomProcessResolver();
        var reporter = new CustomProcessReporter()
            .WithProcessName("NonExistentProcess12345");


        var result = resolver.ResolveSync(reporter);


        Assert.NotNull(result);
        Assert.Contains("No processes found", result);
    }

    #endregion

    #region Custom Network Resolver Tests

    [Fact]
    public async Task CustomNetworkResolver_ResolveAsync_ReturnsNetworkStatus()
    {

        var resolver = new CustomNetworkResolver();
        var reporter = new CustomNetworkReporter()
            .WithHostname("127.0.0.1")
            .WithPort(80)
            .WithTimeout(2000)
            .WithIncludePingTest(true);


        var result = await resolver.ResolveAsync(reporter);


        Assert.NotNull(result);
        Assert.Contains("Hostname", result);
        Assert.Contains("127.0.0.1", result);
        Assert.Contains("Port", result);
        Assert.Contains("PingResult", result);
    }

    [Fact]
    public void CustomNetworkResolver_ResolveSync_WithoutPingTest_ExcludesPingData()
    {

        var resolver = new CustomNetworkResolver();
        var reporter = new CustomNetworkReporter()
            .WithHostname("example.com")
            .WithPort(443)
            .WithIncludePingTest(false);


        var result = resolver.ResolveSync(reporter);


        Assert.NotNull(result);
        Assert.Contains("example.com", result);
        Assert.Contains("443", result);
        Assert.DoesNotContain("PingResult", result);
    }

    #endregion

    #region Resolver Interface Implementation Tests

    [Fact]
    public void CustomResolvers_ImplementRequiredInterfaces()
    {

        Assert.True(typeof(IReportResolverAsync<ICustomFileReporter>).IsAssignableFrom(typeof(CustomFileResolver)));
        Assert.True(typeof(IReportResolverSync<ICustomFileReporter>).IsAssignableFrom(typeof(CustomFileResolver)));

        Assert.True(typeof(IReportResolverAsync<ICustomMemoryReporter>).IsAssignableFrom(typeof(CustomMemoryResolver)));
        Assert.True(typeof(IReportResolverSync<ICustomMemoryReporter>).IsAssignableFrom(typeof(CustomMemoryResolver)));

        Assert.True(typeof(IReportResolverAsync<ICustomProcessReporter>).IsAssignableFrom(typeof(CustomProcessResolver)));
        Assert.True(typeof(IReportResolverSync<ICustomProcessReporter>).IsAssignableFrom(typeof(CustomProcessResolver)));

        Assert.True(typeof(IReportResolverAsync<ICustomNetworkReporter>).IsAssignableFrom(typeof(CustomNetworkResolver)));
        Assert.True(typeof(IReportResolverSync<ICustomNetworkReporter>).IsAssignableFrom(typeof(CustomNetworkResolver)));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CustomResolvers_HandleNullReporter_ThrowsArgumentNullException()
    {

        var fileResolver = new CustomFileResolver();
        var memoryResolver = new CustomMemoryResolver();
        var processResolver = new CustomProcessResolver();
        var networkResolver = new CustomNetworkResolver();


        await Assert.ThrowsAsync<ArgumentNullException>(() => fileResolver.ResolveAsync(null!));
        Assert.Throws<ArgumentNullException>(() => fileResolver.ResolveSync(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => memoryResolver.ResolveAsync(null!));
        Assert.Throws<ArgumentNullException>(() => memoryResolver.ResolveSync(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => processResolver.ResolveAsync(null!));
        Assert.Throws<ArgumentNullException>(() => processResolver.ResolveSync(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => networkResolver.ResolveAsync(null!));
        Assert.Throws<ArgumentNullException>(() => networkResolver.ResolveSync(null!));
    }

    [Fact]
    public async Task CustomResolvers_HandleCancellation_ProperlyCancels()
    {

        var networkResolver = new CustomNetworkResolver();
        var reporter = new CustomNetworkReporter()
            .WithHostname("10.255.255.1")
            .WithPort(12345)
            .WithTimeout(10000);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);


        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            networkResolver.ResolveAsync(reporter, cts.Token));
    }

    #endregion

    #region Helper Methods

    private static string CreateTempDirectoryWithFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);


        File.WriteAllText(Path.Combine(tempDir, "test1.txt"), "Test content 1");
        File.WriteAllText(Path.Combine(tempDir, "test2.txt"), "Test content 2");
        File.WriteAllText(Path.Combine(tempDir, "readme.md"), "# Test README");


        var nestedDir = Path.Combine(tempDir, "nested");
        Directory.CreateDirectory(nestedDir);
        File.WriteAllText(Path.Combine(nestedDir, "nested.log"), "Nested log content");

        return tempDir;
    }

    private static void CleanupTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {

        }
    }

    #endregion

    #region Test Helper Classes - Custom Resolver Implementations

    public class CustomFileResolver : IReportResolverAsync<ICustomFileReporter>, IReportResolverSync<ICustomFileReporter>
    {
        public async Task<string> ResolveAsync(ICustomFileReporter reporter, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(ResolveSync(reporter));
        }

        public string ResolveSync(ICustomFileReporter reporter)
        {
            ArgumentNullException.ThrowIfNull(reporter);

            if (!Directory.Exists(reporter.DirectoryPath))
            {
                return JsonSerializer.Serialize(new { Error = "Directory not found", Path = reporter.DirectoryPath });
            }

            try
            {
                var searchOption = reporter.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(reporter.DirectoryPath, reporter.SearchPattern, searchOption);

                var fileInfos = files.Select(file =>
                {
                    var info = new FileInfo(file);
                    return new FileInfoData
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        Size = info.Length,
                        LastModified = info.LastWriteTime,
                        Extension = info.Extension
                    };
                }).ToList();

                return JsonSerializer.Serialize(fileInfos, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }
    }

    public class CustomMemoryResolver : IReportResolverAsync<ICustomMemoryReporter>, IReportResolverSync<ICustomMemoryReporter>
    {
        public async Task<string> ResolveAsync(ICustomMemoryReporter reporter, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(ResolveSync(reporter));
        }

        public string ResolveSync(ICustomMemoryReporter reporter)
        {
            ArgumentNullException.ThrowIfNull(reporter);

            var result = new Dictionary<string, object>(reporter.MemoryData);

            if (reporter.IncludeGCInfo)
            {
                result["GC_TotalMemory"] = GC.GetTotalMemory(false);
                result["GC_Generation0Collections"] = GC.CollectionCount(0);
                result["GC_Generation1Collections"] = GC.CollectionCount(1);
                result["GC_Generation2Collections"] = GC.CollectionCount(2);
            }

            result["Timestamp"] = DateTime.UtcNow;

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public class CustomProcessResolver : IReportResolverAsync<ICustomProcessReporter>, IReportResolverSync<ICustomProcessReporter>
    {
        public async Task<string> ResolveAsync(ICustomProcessReporter reporter, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(ResolveSync(reporter));
        }

        public string ResolveSync(ICustomProcessReporter reporter)
        {
            ArgumentNullException.ThrowIfNull(reporter);

            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(reporter.ProcessName);

                if (processes.Length == 0)
                {
                    return JsonSerializer.Serialize(new { Message = $"No processes found with name: {reporter.ProcessName}" });
                }

                var processInfos = processes.Select(proc =>
                {
                    var info = new Dictionary<string, object>
                    {
                        ["ProcessName"] = proc.ProcessName,
                        ["Id"] = proc.Id,
                        ["StartTime"] = proc.HasExited ? null : proc.StartTime,
                        ["HasExited"] = proc.HasExited
                    };

                    if (!proc.HasExited)
                    {
                        try
                        {
                            info["WorkingSet"] = proc.WorkingSet64;
                            info["VirtualMemory"] = proc.VirtualMemorySize64;

                            if (reporter.IncludeThreads)
                            {
                                info["ThreadCount"] = proc.Threads.Count;
                            }
                        }
                        catch
                        {

                            info["AccessError"] = "Unable to access process details";
                        }
                    }

                    return info;
                }).ToList();

                return JsonSerializer.Serialize(processInfos, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }
    }

    public class CustomNetworkResolver : IReportResolverAsync<ICustomNetworkReporter>, IReportResolverSync<ICustomNetworkReporter>
    {
        public async Task<string> ResolveAsync(ICustomNetworkReporter reporter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reporter);
            cancellationToken.ThrowIfCancellationRequested();

            var result = new Dictionary<string, object>
            {
                ["Hostname"] = reporter.Hostname,
                ["Port"] = reporter.Port,
                ["Timeout"] = reporter.Timeout,
                ["Timestamp"] = DateTime.UtcNow
            };


            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(reporter.Hostname, reporter.Port, cancellationToken);
                result["PortConnectable"] = true;
                result["ConnectionStatus"] = "Success";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result["PortConnectable"] = false;
                result["ConnectionStatus"] = ex.Message;
            }


            if (reporter.IncludePingTest)
            {
                try
                {
                    using var ping = new Ping();
                    IPAddress? ipAddress = null;


                    if (!IPAddress.TryParse(reporter.Hostname, out ipAddress))
                    {

                        var hostEntry = await System.Net.Dns.GetHostEntryAsync(reporter.Hostname, cancellationToken);
                        ipAddress = hostEntry.AddressList.FirstOrDefault();
                    }

                    if (ipAddress != null)
                    {
                        var reply = await ping.SendPingAsync(ipAddress, reporter.Timeout);
                        result["PingResult"] = new
                        {
                            Status = reply.Status.ToString(),
                            RoundtripTime = reply.RoundtripTime,
                            Success = reply.Status == IPStatus.Success
                        };
                    }
                    else
                    {
                        result["PingResult"] = new
                        {
                            Status = "Failed",
                            Error = "Could not resolve hostname to IP address",
                            Success = false
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result["PingResult"] = new
                    {
                        Status = "Error",
                        Error = ex.Message,
                        Success = false
                    };
                }
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        public string ResolveSync(ICustomNetworkReporter reporter)
        {
            return ResolveAsync(reporter).GetAwaiter().GetResult();
        }
    }

    public class FileInfoData
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Extension { get; set; } = string.Empty;
    }

    #endregion
}
