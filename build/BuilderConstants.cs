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

// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;

namespace DodoHosted.Builder;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class BuilderConstants
{
    public const string BUILDER_CONFIGURATION = "Release";
    public const string DOCKER_IMAGE_NAME = "dodo-hosted";
}
