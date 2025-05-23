// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Text.Json;
using ManifestReaderTests;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Cli.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;
using static Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadResolver;
using Microsoft.DotNet.Cli.Commands.Workload.Install;
using Microsoft.DotNet.Cli.Commands.Workload;

namespace Microsoft.DotNet.Cli.Workload.Install.Tests
{
    public class GivenFileBasedWorkloadInstall : SdkTest
    {
        private readonly BufferedReporter _reporter;
        private readonly string _manifestPath;

        public GivenFileBasedWorkloadInstall(ITestOutputHelper log) : base(log)
        {
            _reporter = new BufferedReporter();
            _manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "Sample2.json");
        }

        [Fact]
        public void InstallStateUpdatesWorkProperly()
        {
            (string dotnetRoot, FileBasedInstaller installer, _, _) = GetTestInstaller();
            var stringFeatureBand = "6.0.300"; // This is hard-coded in the test installer, so if that changes, update this, too.
            var sdkFeatureBand = new SdkFeatureBand(stringFeatureBand);
            var path = Path.Combine(dotnetRoot, "metadata", "workloads", RuntimeInformation.ProcessArchitecture.ToString(), stringFeatureBand, "InstallState", "default.json");

            installer.UpdateInstallMode(sdkFeatureBand, true);
            var installState = InstallStateContents.FromString(File.ReadAllText(path));
            installState.Manifests.Should().BeNull();
            installState.UseWorkloadSets.Should().BeTrue();

            installer.SaveInstallStateManifestVersions(sdkFeatureBand, new Dictionary<string, string>()
            {
                { "first", "second" },
                { "third", "fourth" },
            });

            installState = InstallStateContents.FromString(File.ReadAllText(path));
            installState.Manifests.Count.Should().Be(2);
            installState.Manifests["first"].Should().Be("second");
            installState.Manifests["third"].Should().Be("fourth");
            installState.UseWorkloadSets.Should().BeTrue();

            installer.UpdateInstallMode(sdkFeatureBand, false);
            installState = InstallStateContents.FromString(File.ReadAllText(path));
            installState.UseWorkloadSets.Should().BeFalse();
            installState.Manifests.Count.Should().Be(2);

            installer.RemoveManifestsFromInstallState(sdkFeatureBand);
            installState = InstallStateContents.FromString(File.ReadAllText(path));
            installState.Manifests.Should().BeNull();
            installState.UseWorkloadSets.Should().BeFalse();
        }

        [Fact]
        public void GivenManagedInstallItCanGetFeatureBandsWhenFilesArePresent()
        {
            SdkFeatureBand[] versions = new[]
            {
                new SdkFeatureBand("6.0.100"),
                new SdkFeatureBand("6.0.300"),
                new SdkFeatureBand("7.0.100")
            };
            (string dotnetRoot, FileBasedInstaller installer, _, _) = GetTestInstaller();

            // Write fake workloads
            foreach (SdkFeatureBand version in versions)
            {
                string path = Path.Combine(dotnetRoot, "metadata", "workloads", version.ToString(), "InstalledWorkloads");
                Directory.CreateDirectory(path);
                File.Create(Path.Combine(path, "6.0.100")).Close();
            }

            IEnumerable<SdkFeatureBand> featureBands = installer.GetWorkloadInstallationRecordRepository().GetFeatureBandsWithInstallationRecords();
            featureBands.Should().BeEquivalentTo(versions);
        }

        [Fact]
        public void GivenManagedInstallItCanNotGetFeatureBandsWhenFilesAreNotPresent()
        {
            string[] versions = new[] { "6.0.100", "6.0.300", "7.0.100" };
            (string dotnetRoot, FileBasedInstaller installer, _, _) = GetTestInstaller();

            // Write fake workloads
            foreach (string version in versions)
            {
                string path = Path.Combine(dotnetRoot, "metadata", "workloads", version, "InstalledWorkloads");
                Directory.CreateDirectory(path);
            }

            IEnumerable<SdkFeatureBand> featureBands = installer.GetWorkloadInstallationRecordRepository().GetFeatureBandsWithInstallationRecords();
            featureBands.Should().BeEmpty();
        }

        [Fact]
        public void GivenManagedInstallItCanGetInstalledWorkloads()
        {
            var version = "6.0.100";
            var workloads = new WorkloadId[] { new WorkloadId("test-workload-1"), new WorkloadId("test-workload-2"), new WorkloadId("test-workload3") };
            var (dotnetRoot, installer, _, _) = GetTestInstaller();

            // Write fake workloads
            var net6Path = Path.Combine(dotnetRoot, "metadata", "workloads", version, "InstalledWorkloads");
            Directory.CreateDirectory(net6Path);
            foreach (var workload in workloads)
            {
                File.WriteAllText(Path.Combine(net6Path, workload), string.Empty);
            }
            var net7Path = Path.Combine(dotnetRoot, "metadata", "workloads", "7.0.100", "InstalledWorkloads");
            Directory.CreateDirectory(net7Path);
            File.WriteAllText(Path.Combine(net7Path, workloads.First()), string.Empty);

            var installedWorkloads = installer.GetWorkloadInstallationRecordRepository().GetInstalledWorkloads(new SdkFeatureBand(version));
            installedWorkloads.Should().BeEquivalentTo(workloads);
        }

        [Fact]
        public void GivenManagedInstallItCanWriteInstallationRecord()
        {
            var workloadId = new WorkloadId("test-workload");
            var version = "6.0.100";
            var (dotnetRoot, installer, _, _) = GetTestInstaller();
            installer.GetWorkloadInstallationRecordRepository().WriteWorkloadInstallationRecord(workloadId, new SdkFeatureBand(version));
            var expectedPath = Path.Combine(dotnetRoot, "metadata", "workloads", version, "InstalledWorkloads", workloadId.ToString());
            File.Exists(expectedPath).Should().BeTrue();
        }

        static PackInfo CreatePackInfo(string id, string version, WorkloadPackKind kind, string path, string resolvedPackageId)
            => new(new WorkloadPackId(id), version, kind, path, resolvedPackageId);

        [Fact]
        public void GivenManagedInstallItCanInstallDirectoryPacks()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";
            var version = "6.0.100";
            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context));

            var mockNugetInstaller = nugetInstaller as MockNuGetPackageDownloader;
            mockNugetInstaller.DownloadCallParams.Count.Should().Be(1);
            mockNugetInstaller.DownloadCallParams[0].Should().BeEquivalentTo((new PackageId(packId), new NuGetVersion(packVersion), null as DirectoryPath?, null as PackageSourceLocation));
            mockNugetInstaller.ExtractCallParams.Count.Should().Be(1);
            mockNugetInstaller.ExtractCallParams[0].Item1.Should().Be(mockNugetInstaller.DownloadCallResult[0]);
            mockNugetInstaller.ExtractCallParams[0].Item2.ToString().Should().Contain($"{packId}-{packVersion}-extracted");

            var installationRecordPath = Path.Combine(dotnetRoot, "metadata", "workloads", "InstalledPacks", "v1", packId, packVersion, version);
            File.Exists(installationRecordPath).Should().BeTrue();

            Directory.Exists(Path.Combine(dotnetRoot, "packs", packId, packVersion)).Should().BeTrue();
        }

        [Fact]
        public void GivenManagedInstallItCanInstallSingleFilePacks()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var packId = "Xamarin.Android.Templates";
            var packVersion = "1.0.3";

            var version = "6.0.100";
            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-templates-workload") }, new SdkFeatureBand(version), context));

            (nugetInstaller as MockNuGetPackageDownloader).DownloadCallParams.Count.Should().Be(1);
            (nugetInstaller as MockNuGetPackageDownloader).DownloadCallParams[0].Should().BeEquivalentTo((new PackageId(packId), new NuGetVersion(packVersion), null as DirectoryPath?, null as PackageSourceLocation));
            (nugetInstaller as MockNuGetPackageDownloader).ExtractCallParams.Count.Should().Be(0);

            var installationRecordPath = Path.Combine(dotnetRoot, "metadata", "workloads", "InstalledPacks", "v1", packId, packVersion, version);
            File.Exists(installationRecordPath).Should().BeTrue();
            var content = File.ReadAllText(installationRecordPath);
            content.Should().Contain(packId.ToString());
            content.Should().Contain(packVersion.ToString());

            File.Exists(Path.Combine(dotnetRoot, "template-packs", $"{packId}.{packVersion}.nupkg".ToLowerInvariant())).Should().BeTrue();
        }

        [Fact]
        public void GivenManagedInstallItCanInstallPacksWithAliases()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var mockNugetInstaller = nugetInstaller as MockNuGetPackageDownloader;
            //  Test runs xplat, but in WorkloadResolver.CreateForTests we are using Windows RIDs for tests
            var packId = "Xamarin.Android.BuildTools.WinHost";
            var packVersion = "8.4.7";

            var version = "6.0.100";
            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-buildtools-workload") }, new SdkFeatureBand(version), context));

            mockNugetInstaller.DownloadCallParams.Count.Should().Be(1);
            mockNugetInstaller.DownloadCallParams[0].Should().BeEquivalentTo((new PackageId(packId), new NuGetVersion(packVersion), null as DirectoryPath?, null as PackageSourceLocation));
            mockNugetInstaller.ExtractCallParams.Count.Should().Be(1);
            mockNugetInstaller.ExtractCallParams[0].Item1.Should().Be(mockNugetInstaller.DownloadCallResult[0]);
            mockNugetInstaller.ExtractCallParams[0].Item2.ToString().Should().Contain($"{packId}-{packVersion}-extracted");

            Directory.Exists(Path.Combine(dotnetRoot, "packs", packId, packVersion)).Should().BeTrue();
        }

        [Fact]
        public void GivenManagedInstallItHonorsNuGetSources()
        {
            var packageSource = new PackageSourceLocation(new FilePath("mock-file"));
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller(packageSourceLocation: packageSource);
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";

            var version = "6.0.100";
            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context));

            var mockNugetInstaller = nugetInstaller as MockNuGetPackageDownloader;
            mockNugetInstaller.DownloadCallParams.Count.Should().Be(1);
            mockNugetInstaller.DownloadCallParams[0].Should().BeEquivalentTo((new PackageId(packId), new NuGetVersion(packVersion), null as DirectoryPath?, packageSource));
        }

        [Fact]
        public void GivenManagedInstallItDetectsInstalledPacks()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";
            var version = "6.0.100";

            // Mock installing the pack
            Directory.CreateDirectory(Path.Combine(dotnetRoot, "packs", packId, packVersion));

            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context));

            (nugetInstaller as MockNuGetPackageDownloader).DownloadCallParams.Count.Should().Be(0);
        }

        [Fact]
        public void GivenManagedInstallItCanRollBackInstallFailures()
        {
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";
            var version = "6.0.100";
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller(failingInstaller: true);

            var exceptionThrown = Assert.Throws<Exception>(() =>
            {
                CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context));
            });
            exceptionThrown.Message.Should().Be("Test Failure");
            var failingNugetInstaller = nugetInstaller as FailingNuGetPackageDownloader;
            // Nupkgs should be removed
            Directory.GetFiles(failingNugetInstaller.MockPackageDir).Should().BeEmpty();
            // Packs should be removed
            Directory.Exists(Path.Combine(dotnetRoot, "packs", packId, packVersion)).Should().BeFalse();
        }

        [Fact]
        public void GivenManagedInstallItDoesNotRemovePacksWithInstallRecords()
        {
            var (dotnetRoot, installer, _, getResolver) = GetTestInstaller();
            var packs = new PackInfo[]
            {
                CreatePackInfo("Xamarin.Android.Sdk", "8.4.7", WorkloadPackKind.Library, Path.Combine(dotnetRoot, "packs", "Xamarin.Android.Sdk", "8.4.7"), "Xamarin.Android.Sdk"),
                CreatePackInfo("Xamarin.Android.Framework", "8.4.0", WorkloadPackKind.Framework, Path.Combine(dotnetRoot, "packs", "Xamarin.Android.Framework", "8.4.0"), "Xamarin.Android.Framework")
            };
            var packsToBeGarbageCollected = new PackInfo[]
            {
                CreatePackInfo("Test.Pack.A", "1.0.0", WorkloadPackKind.Sdk, Path.Combine(dotnetRoot, "packs", "Test.Pack.A", "1.0.0"), "Test.Pack.A"),
                CreatePackInfo("Test.Pack.B", "2.0.0", WorkloadPackKind.Framework, Path.Combine(dotnetRoot, "packs", "Test.Pack.B", "2.0.0"), "Test.Pack.B"),
            };
            var sdkVersions = new WorkloadId[] { new WorkloadId("6.0.100"), new WorkloadId("6.0.300") };

            // Write fake packs
            var installedPacksPath = Path.Combine(dotnetRoot, "metadata", "workloads", "InstalledPacks", "v1");
            foreach (var sdkVersion in sdkVersions)
            {
                Directory.CreateDirectory(Path.Combine(dotnetRoot, "metadata", "workloads", sdkVersion, "InstalledWorkloads"));
                foreach (var pack in packs.Concat(packsToBeGarbageCollected))
                {
                    var packRecordPath = Path.Combine(installedPacksPath, pack.Id, pack.Version, sdkVersion);
                    Directory.CreateDirectory(Path.GetDirectoryName(packRecordPath));
                    var packRecordContents = JsonSerializer.Serialize<WorkloadResolver.PackInfo>(pack);
                    File.WriteAllText(packRecordPath, packRecordContents);
                    Directory.CreateDirectory(pack.Path);
                }
            }
            // Write fake workload install record for 6.0.300
            var installedWorkloadsPath = Path.Combine(dotnetRoot, "metadata", "workloads", sdkVersions[1], "InstalledWorkloads", "xamarin-android-build");
            File.WriteAllText(installedWorkloadsPath, string.Empty);

            installer.GarbageCollect(getResolver);

            Directory.EnumerateFileSystemEntries(installedPacksPath)
                .Should()
                .NotBeEmpty();
            foreach (var pack in packs)
            {
                Directory.Exists(pack.Path)
                    .Should()
                    .BeTrue();

                var expectedRecordPath = Path.Combine(installedPacksPath, pack.Id, pack.Version, sdkVersions[1]);
                File.Exists(expectedRecordPath)
                    .Should()
                    .BeTrue();
            }

            foreach (var pack in packsToBeGarbageCollected)
            {
                Directory.Exists(pack.Path)
                    .Should()
                    .BeFalse();
            }
        }

        [Fact]
        public void GivenManagedInstallItCanInstallManifestVersion()
        {
            var (_, installer, nugetDownloader, _) = GetTestInstaller(manifestDownload: true);
            var featureBand = new SdkFeatureBand("6.0.100");
            var manifestId = new ManifestId("test-manifest-1");
            var manifestVersion = new ManifestVersion("5.0.0");

            var manifestUpdate = new ManifestVersionUpdate(manifestId, manifestVersion, featureBand.ToString());

            CliTransaction.RunNew(context => installer.InstallWorkloadManifest(manifestUpdate, context));

            var mockNugetInstaller = nugetDownloader as MockNuGetPackageDownloader;
            mockNugetInstaller.DownloadCallParams.Count.Should().Be(1);
            mockNugetInstaller.DownloadCallParams[0].Should().BeEquivalentTo((new PackageId($"{manifestId}.manifest-{featureBand}"),
                new NuGetVersion(manifestVersion.ToString()), null as DirectoryPath?, null as PackageSourceLocation));
        }

        [Fact]
        public void GivenManagedInstallItCanGetDownloads()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var version = "6.0.100";
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";

            // android-sdk-workload - installed
            // android-buildtools-workload - not installed

            //  Write pack install record
            var installedPacksPath = Path.Combine(dotnetRoot, "metadata", "workloads", "InstalledPacks", "v1");
            var packRecordPath = Path.Combine(installedPacksPath, packId, packVersion, version);
            Directory.CreateDirectory(Path.GetDirectoryName(packRecordPath));
            File.WriteAllText(packRecordPath, string.Empty);

            // Create pack directory
            Directory.CreateDirectory(Path.Combine(dotnetRoot, "packs", packId, packVersion));

            // Write workload install record
            var workloadsRecordPath = Path.Combine(dotnetRoot, "metadata", "workloads", version, "InstalledWorkloads");
            Directory.CreateDirectory(workloadsRecordPath);
            File.Create(Path.Combine(workloadsRecordPath, "android-sdk-workload")).Close();

            var downloads = installer.GetDownloads(new[] { new WorkloadId("android-sdk-workload"), new WorkloadId("android-buildtools-workload") }, new SdkFeatureBand(version), false).ToList();

            var mockNugetInstaller = nugetInstaller as MockNuGetPackageDownloader;
            mockNugetInstaller.DownloadCallParams.Count.Should().Be(0);
            mockNugetInstaller.ExtractCallParams.Count.Should().Be(0);

            downloads.Count.Should().Be(1);

            string expectedPackId = "Xamarin.Android.BuildTools.WinHost";
            downloads[0].NuGetPackageId.Should().Be(expectedPackId);
            downloads[0].NuGetPackageVersion.Should().Be("8.4.7");
        }

        [Fact]
        public void GivenManagedInstallItCanInstallPacksFromOfflineCache()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";
            var version = "6.0.100";
            var cachePath = Path.Combine(dotnetRoot, "MockCache");

            // Write mock cache
            Directory.CreateDirectory(cachePath);
            var nupkgPath = Path.Combine(cachePath, $"{packId}.{packVersion}.nupkg");
            File.Create(nupkgPath).Close();

            CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context, new DirectoryPath(cachePath)));
            var mockNugetInstaller = nugetInstaller as MockNuGetPackageDownloader;

            // We shouldn't download anything, use the cache
            mockNugetInstaller.DownloadCallParams.Count.Should().Be(0);

            // Otherwise install should be normal
            mockNugetInstaller.ExtractCallParams.Count.Should().Be(1);
            mockNugetInstaller.ExtractCallParams[0].Item1.Should().Be(nupkgPath);
            mockNugetInstaller.ExtractCallParams[0].Item2.ToString().Should().Contain($"{packId}-{packVersion}-extracted");
            var installationRecordPath = Path.Combine(dotnetRoot, "metadata", "workloads", "InstalledPacks", "v1", packId, packVersion, version);
            File.Exists(installationRecordPath).Should().BeTrue();
            Directory.Exists(Path.Combine(dotnetRoot, "packs", packId, packVersion)).Should().BeTrue();
        }

        [Fact]
        public void GivenManagedInstallItCanErrorsWhenMissingOfflineCache()
        {
            var (dotnetRoot, installer, nugetInstaller, _) = GetTestInstaller();
            var packId = "Xamarin.Android.Sdk";
            var packVersion = "8.4.7";
            var version = "6.0.100";
            var cachePath = Path.Combine(dotnetRoot, "MockCache");

            var exceptionThrown = Assert.Throws<AggregateException>(() =>
                CliTransaction.RunNew(context => installer.InstallWorkloads(new[] { new WorkloadId("android-sdk-workload") }, new SdkFeatureBand(version), context, new DirectoryPath(cachePath))));
            exceptionThrown.InnerException.Message.Should().Contain(packId);
            exceptionThrown.InnerException.Message.Should().Contain(packVersion);
            exceptionThrown.InnerException.Message.Should().Contain(cachePath);
        }

        private (string, FileBasedInstaller, INuGetPackageDownloader, Func<string, IWorkloadResolver>) GetTestInstaller([CallerMemberName] string testName = "", bool failingInstaller = false, string identifier = "", bool manifestDownload = false,
            PackageSourceLocation packageSourceLocation = null)
        {
            var testDirectory = _testAssetsManager.CreateTestDirectory(testName, identifier: identifier).Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            INuGetPackageDownloader nugetInstaller = failingInstaller ? new FailingNuGetPackageDownloader(testDirectory) : new MockNuGetPackageDownloader(dotnetRoot, manifestDownload);
            var workloadResolver = CreateForTests(new MockManifestProvider(new[] { _manifestPath }), dotnetRoot);
            var sdkFeatureBand = new SdkFeatureBand("6.0.300");

            IWorkloadResolver GetResolver(string workloadSetVersion)
            {
                if (workloadSetVersion != null && !sdkFeatureBand.Equals(new SdkFeatureBand(workloadSetVersion)))
                {
                    throw new NotSupportedException("Mock doesn't support creating resolver for different feature bands: " + workloadSetVersion);
                }
                return workloadResolver;
            }

            var installer = new FileBasedInstaller(_reporter, sdkFeatureBand, workloadResolver, userProfileDir: testDirectory, nugetInstaller, dotnetRoot, packageSourceLocation: packageSourceLocation);

            return (dotnetRoot, installer, nugetInstaller, GetResolver);
        }
    }
}
