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

using System.Text.RegularExpressions;
using DodoHosted.Lib.Plugin.Models;

namespace DodoHosted.Lib.Plugin.Helper;

public static class CommandParser
{
    public static CommandParsed? GetCommandArgs(this string commandMessage)
    {
        if (string.IsNullOrEmpty(commandMessage) || commandMessage.Length < 2)
        {
            return null;
        }
        
        var args = new List<string>();
        var command = commandMessage[1..].TrimEnd().AsSpan();
        var startPointer = 0;
    
        var inQuote = false;
            
        // /cmd "some thing \"in\" quote" -f value
        // cmd | some thing "in" quote | -f | value
            
        for (var movePointer = 0; movePointer < command.Length; movePointer++)
        {
            // 若遇到引号
            if (command[movePointer] == '"')
            {
                // 若引号在第一位，不合法，返回 NULL
                if (movePointer == 0)
                {
                    return null;
                }
                
                // 若引号前一位是转义符，表示是转义引号，跳过
                if (command[movePointer - 1] == '\\')
                {
                    continue;
                }
                
                // 其他状态，切换引号状态
                inQuote = !inQuote;
            }
            
            // 若遇到非空格，继续下一位
            if (command[movePointer] != ' ')
            {
                continue;
            }
    
            // 若在引号内，继续下一位
            if (inQuote)
            {
                continue;
            }
    
            // 若当前的上一位是引号
            if (command[movePointer - 1] == '"')
            {
                // 取出引号内的内容，并消除转义
                var str = command[(startPointer + 1)..^1].ToString();
                args.Add(Regex.Unescape(str));
            }
            else
            {
                // 取出空格前的上一段内容
                args.Add(command.Slice(startPointer, movePointer - startPointer).ToString());
            }
            
            // 开始节点移动到当前位置
            startPointer = movePointer + 1;
        }

        // 结算
        if (command[^1] == '\"')
        {
            var str = command[(startPointer + 1)..^1].ToString();
            args.Add(Regex.Unescape(str));
        }
        else
        {
            args.Add(command[startPointer..].ToString());
        }

        // 指令名称是第一位
        // 第一位是保证存在的
        var cmdName = args[0];

        if (args.Count == 1)
        {
            return new CommandParsed
            {
                CommandName = cmdName, Path = Array.Empty<string>(), Arguments = new Dictionary<string, string>()
            };
        }

        int i;
        for (i = 1; i < args.Count; i++)
        {
            if (args[i].StartsWith("-"))
            {
                break;
            }
        }

        var path = args.GetRange(1, i - 1);
        var arguments = new Dictionary<string, string>();
        
        for (var j = i; j < args.Count; j++)
        {
            if (args[j].StartsWith("-") is false)
            {
                continue;
            }

            if (j + 1 < args.Count && args[j + 1].StartsWith("-") is false)
            {
                arguments.Add(args[j][1..], args[j + 1]);
                j++;
            }
            else
            {
                arguments.Add(args[j][1..], "true");
            }
        }

        return new CommandParsed { CommandName = cmdName, Path = path.ToArray(), Arguments = arguments };
    }
}
