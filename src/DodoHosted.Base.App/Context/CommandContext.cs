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

using DodoHosted.Base.App.Models;
using DodoHosted.Base.Context;
using DodoHosted.Base.Context.Model;

namespace DodoHosted.Base.App.Context;

public record CommandContext(
    ContextBase.Reply Reply,
    ContextBase.ReplyCard ReplyCard,
    ContextBase.PermissionCheck PermissionCheck,
    CommandParsed CommandParsed,
    UserInfo UserInfo,
    EventInfo EventInfo);
