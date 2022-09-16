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

using System.Reflection;

namespace DodoHosted.Base.App.Exceptions;

public class CommandNodeException : Exception
{
    public CommandNodeException(string message) : base(message) { }

    public CommandNodeException(MemberInfo method, string message)
        : base($"类 {method.DeclaringType?.FullName ?? "NULL"} 指令执行器方法 {method.Name}: {message}") { } 
}
