﻿//-----------------------------------------------------------------------
// <copyright file="ExportSubOptions.cs" company="GitTools Contributors">
//     Copyright (c) 2015 - Present - GitTools Contributors
// </copyright>
//-----------------------------------------------------------------------

namespace GitReleaseManager.Cli.Options
{
    using CommandLine;

    [Verb("export", HelpText = "Exports all the Release Notes in markdown format.")]
    public class ExportSubOptions : BaseVcsOptions
    {
        [Option('f', "fileOutputPath", HelpText = "Path to the file export releases.", Required = true)]
        public string FileOutputPath { get; set; }

        [Option('t', "tagName", HelpText = "The name of the release (Typically this is the generated SemVer Version Number).", Required = false)]
        public string TagName { get; set; }
    }
}