﻿//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="GitTools Contributors">
//     Copyright (c) 2015 - Present - GitTools Contributors
// </copyright>
//-----------------------------------------------------------------------

namespace GitReleaseManager.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using AutoMapper;
    using CommandLine;
    using GitReleaseManager.Cli.Options;
    using GitReleaseManager.Core;
    using GitReleaseManager.Core.Configuration;
    using GitReleaseManager.Core.Helpers;
    using FileMode = System.IO.FileMode;

    public static class Program
    {
        private static readonly StringBuilder _log = new StringBuilder();
        private static FileSystem _fileSystem;
        private static Config _configuration;
        private static IMapper _mapper;
        private static IVcsProvider _vcsProvider;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Not required")]
        private static Task<int> Main(string[] args)
        {
            // Just add the TLS 1.2 protocol to the Service Point manager until
            // we've upgraded to latest Octokit.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            _fileSystem = new FileSystem();

            _mapper = AutoMapperConfiguration.Configure();
            
            return Parser.Default.ParseArguments<CreateSubOptions, DiscardSubOptions, AddAssetSubOptions, CloseSubOptions, PublishSubOptions, ExportSubOptions, InitSubOptions, ShowConfigSubOptions, LabelSubOptions>(args)
                .WithParsed<BaseSubOptions>(CreateFiglet)
                .MapResult(
                  (CreateSubOptions opts) => CreateReleaseAsync(opts),
                  (DiscardSubOptions opts) => DiscardReleaseAsync(opts),
                  (AddAssetSubOptions opts) => AddAssetsAsync(opts),
                  (CloseSubOptions opts) => CloseMilestoneAsync(opts),
                  (PublishSubOptions opts) => PublishReleaseAsync(opts),
                  (ExportSubOptions opts) => ExportReleasesAsync(opts),
                  (InitSubOptions opts) => CreateSampleConfigFileAsync(opts),
                  (ShowConfigSubOptions opts) => ShowConfigAsync(opts),
                  (LabelSubOptions opts) => CreateLabelsAsync(opts),
                  errs => Task.FromResult(1));
        }

        private static void CreateFiglet(BaseSubOptions options)
        {
            if (options.NoLogo)
            {
                return;
            }

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            if (version.IndexOf('+') > 0)
            {
                version = version.Substring(0, version.IndexOf('+'));
            }
            var shortFormat = @"
   ____ ____  __  __ 
  / ___|  _ \|  \/  |
 | |  _| |_) | |\/| |
 | |_| |  _ <| |  | |
  \____|_| \_\_|  |_|
{0,21}
";
            var longFormat = @"
   ____ _ _   ____      _                     __  __
  / ___(_) |_|  _ \ ___| | ___  __ _ ___  ___|  \/  | __ _ _ __   __ _  __ _  ___ _ __
 | |  _| | __| |_) / _ \ |/ _ \/ _` / __|/ _ \ |\/| |/ _` | '_ \ / _` |/ _` |/ _ \ '__|
 | |_| | | |_|  _ <  __/ |  __/ (_| \__ \  __/ |  | | (_| | | | | (_| | (_| |  __/ |
  \____|_|\__|_| \_\___|_|\___|\__,_|___/\___|_|  |_|\__,_|_| |_|\__,_|\__, |\___|_|
                                                                       |___/
{0,87}
";
            if (Console.WindowWidth > 87)
            {
                Console.WriteLine(longFormat, version);
            }
            else
            {
                Console.WriteLine(shortFormat, version);
            }
        }

        private static async Task<int> CreateReleaseAsync(CreateSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                Core.Model.Release release;
                if (!string.IsNullOrEmpty(subOptions.Milestone))
                {
                    var releaseName = subOptions.Name;
                    if (string.IsNullOrWhiteSpace(releaseName))
                    {
                        releaseName = subOptions.Milestone;
                    }

                    release = await _vcsProvider.CreateReleaseFromMilestone(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.Milestone, releaseName, subOptions.TargetCommitish, subOptions.AssetPaths, subOptions.Prerelease).ConfigureAwait(false);
                }
                else
                {
                    release = await _vcsProvider.CreateReleaseFromInputFile(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.Name, subOptions.InputFilePath, subOptions.TargetCommitish, subOptions.AssetPaths, subOptions.Prerelease).ConfigureAwait(false);
                }

                Console.WriteLine(release.HtmlUrl);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task<int> DiscardReleaseAsync(DiscardSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                await _vcsProvider.DiscardRelease(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.Milestone);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }
        
        private static async Task<int> AddAssetsAsync(AddAssetSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                await _vcsProvider.AddAssets(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.TagName, subOptions.AssetPaths).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task<int> CloseMilestoneAsync(CloseSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                await _vcsProvider.CloseMilestone(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.Milestone).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task<int> PublishReleaseAsync(PublishSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                await _vcsProvider.PublishRelease(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.TagName).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task<int> ExportReleasesAsync(ExportSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                var releasesMarkdown = await _vcsProvider.ExportReleases(subOptions.RepositoryOwner, subOptions.RepositoryName, subOptions.TagName).ConfigureAwait(false);

                using (var sw = new StreamWriter(File.Open(subOptions.FileOutputPath, FileMode.OpenOrCreate)))
                {
                    sw.Write(releasesMarkdown);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static Task<int> CreateSampleConfigFileAsync(InitSubOptions subOptions)
        {
            ConfigureLogging(subOptions.LogFilePath);

            ConfigurationProvider.WriteSample(subOptions.TargetDirectory ?? Environment.CurrentDirectory, _fileSystem);
            return Task.FromResult(0);
        }

        private static Task<int> ShowConfigAsync(ShowConfigSubOptions subOptions)
        {
            ConfigureLogging(subOptions.LogFilePath);

            Console.WriteLine(ConfigurationProvider.GetEffectiveConfigAsString(subOptions.TargetDirectory ?? Environment.CurrentDirectory, _fileSystem));
            return Task.FromResult(0);
        }

        private static async Task<int> CreateLabelsAsync(LabelSubOptions subOptions)
        {
            try
            {
                ConfigureLogging(subOptions.LogFilePath);

                _vcsProvider = GetVcsProvider(subOptions);

                await _vcsProvider.CreateLabels(subOptions.RepositoryOwner, subOptions.RepositoryName).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static IVcsProvider GetVcsProvider(BaseVcsOptions subOptions)
        {
            _configuration = ConfigurationProvider.Provide(subOptions.TargetDirectory ?? Environment.CurrentDirectory, _fileSystem);
            return new GitHubProvider(_mapper, _configuration, subOptions.UserName, subOptions.Password, subOptions.Token);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Not required.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is required here.")]
        private static void ConfigureLogging(string logFilePath)
        {
            var writeActions = new List<Action<string>>
            {
                s => _log.AppendLine(s)
            };

            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                    if (File.Exists(logFilePath))
                    {
                        using (File.CreateText(logFilePath))
                        {
                        }
                    }

                    writeActions.Add(x => WriteLogEntry(logFilePath, x));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to configure logging: " + ex.Message);
                }
            }
            else
            {
                // if nothing else is specified, write to console
                writeActions.Add(Console.WriteLine);
            }

            Logger.WriteInfo = s => writeActions.ForEach(a => a(s));
            Logger.WriteWarning = s => writeActions.ForEach(a => a(s));
            Logger.WriteError = s => writeActions.ForEach(a => a(s));
        }

        private static void WriteLogEntry(string logFilePath, string s)
        {
            var contents = string.Format(CultureInfo.InvariantCulture, "{0}\t\t{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), s);
            File.AppendAllText(logFilePath, contents);
        }
    }
}
