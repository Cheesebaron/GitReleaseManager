﻿//-----------------------------------------------------------------------
// <copyright file="PublishSubOptions.cs" company="GitTools Contributors">
//     Copyright (c) 2015 - Present - GitTools Contributors
// </copyright>
//-----------------------------------------------------------------------

namespace GitReleaseManager.Cli.Options
{
    using CommandLine;

    [Verb("publish", HelpText = "Publishes the Release.")]
    public class PublishSubOptions : BaseVcsOptions
    {
        [Option('t', "tagName", HelpText = "The name of the release (Typically this is the generated SemVer Version Number).", Required = true)]
        public string TagName { get; set; }
    }
}