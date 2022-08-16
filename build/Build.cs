// This file is a part of Dodo.Hosted project.
// 
// Copyright (C) 2022 LiamSho and all Contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MinVer;
using Serilog;

namespace DodoHosted.Builder;

public partial class Build : NukeBuild
{
    public static void Main() => Execute<Build>(_ => _.Default);

    [Solution] Solution _solution;
    [CI] GitHubActions _gitHubActions;
    [MinVer] MinVer _minVer;
    [Parameter] [Secret] string _nugetApiKey;
    [Parameter] [Secret] string _dockerHubUsername;
    [Parameter] [Secret] string _dockerHubApiKey;

    Project AppProject => _solution.GetProject("DodoHosted.App");

    IEnumerable<Project> PackedProjects => _solution
        .GetProjects("*")
        .Except(new[] { AppProject, _solution.GetProject("DodoHosted.Builder") });

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    readonly List<string> _dockerImages = new();

    Target Default => _ => _
        .Executes(() =>
        {
            Assert.Fail("You should specify a target to run.");
        });

    Target Version => _ => _
        .Executes(() =>
        {
            Log.Logger.Information("Package Version: {PackageVersion}", _minVer.PackageVersion);
            Log.Logger.Information("Assembly Version: {NuGetVersion}", _minVer.AssemblyVersion);
            Log.Logger.Information("File Version: {NuGetVersion}", _minVer.FileVersion);
        });
    
    Target Clean => _ => _
        .DependsOn(Version)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(x => x
                .SetProcessWorkingDirectory(RootDirectory)
                .SetConfiguration(BuilderConstants.BUILDER_CONFIGURATION));
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(x => x
                .SetProcessWorkingDirectory(RootDirectory));
        });
    
    Target Building => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(x => x
                .SetProcessWorkingDirectory(RootDirectory)
                .SetVersion(_minVer.PackageVersion)
                .SetAssemblyVersion(_minVer.AssemblyVersion)
                .SetFileVersion(_minVer.FileVersion)
                .SetInformationalVersion(_minVer.PackageVersion)
                .SetConfiguration(BuilderConstants.BUILDER_CONFIGURATION));
        });

    Target Pack => _ => _
        .DependsOn(Building)
        .Executes(() =>
        {
            FileSystemTasks.EnsureCleanDirectory(ArtifactsDirectory);
            
            foreach (var project in PackedProjects)
            {
                DotNetTasks.DotNetPack(x => x
                    .SetProject(project)
                    .SetVersion(_minVer.PackageVersion)
                    .SetConfiguration(BuilderConstants.BUILDER_CONFIGURATION)
                    .SetOutputDirectory(ArtifactsDirectory));
            }
        });

    Target PublishArtifact => _ => _
        .DependsOn(Pack)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .OnlyWhenStatic(() => _gitHubActions != null);

    Target PublishRelease => _ => _
        .DependsOn(Pack)
        .OnlyWhenStatic(() => _gitHubActions != null)
        .Executes(() =>
        {
            foreach (var package in ArtifactsDirectory.GlobFiles("*.nupkg"))
            {
                DotNetTasks.DotNetNuGetPush(x => x
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(_nugetApiKey)
                    .SetTargetPath(package));
            }
        });

    Target BuildDockerImage => _ => _
        .DependsOn(PublishRelease)
        .OnlyWhenStatic(() => _gitHubActions != null)
        .Executes(() =>
        {
            FileSystemTasks.EnsureCleanDirectory(RootDirectory / "publish");
            DotNetTasks.DotNetPublish(x => x
                .EnableNoBuild()
                .SetProject(AppProject)
                .SetConfiguration(BuilderConstants.BUILDER_CONFIGURATION)
                .SetOutput(RootDirectory / "publish"));
            
            var platforms = new[] { "linux/amd64", "linux/arm64" };
            foreach (var platform in platforms)
            {
                var tag = $"{_dockerHubUsername}/{BuilderConstants.DOCKER_IMAGE_NAME}:{_minVer.PackageVersion}-{platform.Split("/")[1]}";
                DockerTasks.DockerBuildxBuild(x => x
                    .EnableLoad()
                    .SetPath(RootDirectory)
                    .SetPlatform(platform)
                    .SetTag(tag)
                    .SetBuildArg($"APP_VERSION={_minVer.PackageVersion}")
                    .SetFile(RootDirectory / "Dockerfile"));
                _dockerImages.Add(tag);
            }
        });

    Target UploadDockerImage => _ => _
        .DependsOn(BuildDockerImage)
        .OnlyWhenStatic(() => _gitHubActions != null)
        .Executes(() =>
        {
            var currentVersionManifest = $"{_dockerHubUsername}/{BuilderConstants.DOCKER_IMAGE_NAME}:{_minVer.PackageVersion}";
            var latestVersionManifest = $"{_dockerHubUsername}/{BuilderConstants.DOCKER_IMAGE_NAME}:latest";

            DockerTasks.DockerLogin(x => x
                .SetUsername(_dockerHubUsername)
                .SetPassword(_dockerHubApiKey));

            foreach (var image in _dockerImages)
            {
                DockerTasks.DockerImagePush(x => x
                    .SetName(image));
            }

            DockerTasks.DockerManifestCreate(x => x
                .SetManifestList(currentVersionManifest)
                .SetManifests(_dockerImages));

            DockerTasks.DockerManifestCreate(x => x
                .SetProcessArgumentConfigurator(a => a.Add("--amend"))
                .SetManifestList(latestVersionManifest)
                .SetManifests(_dockerImages));

            DockerTasks.DockerManifestPush(x => x
                .SetManifestList(currentVersionManifest));

            DockerTasks.DockerManifestPush(x => x
                .SetManifestList(latestVersionManifest));
        });
}
