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

namespace DodoHosted.Open.Plugin;

/// <summary>
/// 插件实例
/// </summary>
public abstract class DodoHostedPluginConfiguration
{
    /// <summary>
    /// 注册 MongoDb Collection
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<Type, string> RegisterMongoDbCollection()
    {
        return new Dictionary<Type, string>();
    }
}
