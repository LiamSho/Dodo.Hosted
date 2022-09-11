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

namespace DodoHosted.Open.Plugin.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CmdRunnerAttribute : Attribute
{
    public string[] Path { get; }
    public string PermissionNode { get; }
    public string Description { get; }
    
    public CmdRunnerAttribute(string permissionNode, string description, params string[] path)
    {
        PermissionNode = permissionNode;
        Description = description;
        Path = path;
    }
}
