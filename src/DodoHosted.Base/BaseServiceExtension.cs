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

using DodoHosted.Base.Interfaces;
using DodoHosted.Base.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Base;

public static class BaseServiceExtension
{
    public static IServiceCollection AddBaseServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IChannelLogger, ChannelLogger>();

        return serviceCollection;
    }
}
