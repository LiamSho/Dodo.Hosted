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

using Nuke.Common.CI.GitHubActions;

namespace DodoHosted.Builder;

[GitHubActions(
    name: "release",
    image: GitHubActionsImage.UbuntuLatest,
    OnPushTags = new[] { "*" },
    InvokedTargets = new[] { nameof(UploadDockerImage) },
    ImportSecrets = new [] { nameof(_nugetApiKey), nameof(_dockerHubUsername), nameof(_dockerHubApiKey) },
    EnableGitHubToken = true,
    PublishArtifacts = true,
    AutoGenerate = false
)]
public partial class Build { }
