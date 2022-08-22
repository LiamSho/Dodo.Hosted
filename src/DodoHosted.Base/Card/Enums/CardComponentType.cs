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

using DodoHosted.Base.JsonExtension;

namespace DodoHosted.Base.Card.Enums;

/// <summary>
/// 卡片组件类型
/// </summary>
[StringValueTypeWriteConvertor<CardComponentType>]
public class CardComponentType : StringValueType
{
    private CardComponentType(string value) : base(value) { }
    
    /// <summary>
    /// 内容组件 - 标题
    /// </summary>
    public static readonly CardComponentType Header = new("header");
    
    /// <summary>
    /// 内容组件 - 文本
    /// </summary>
    public static readonly CardComponentType Text = new("section");
    
    /// <summary>
    /// 内容组件 - 多栏文本
    /// </summary>
    public static readonly CardComponentType MultilineText = new("section");
        
    /// <summary>
    /// 内容组件 - 备注
    /// </summary>
    public static readonly CardComponentType Remark = new("remark");
        
    /// <summary>
    /// 内容组件 - 图片
    /// </summary>
    public static readonly CardComponentType Image = new("image");
        
    /// <summary>
    /// 内容组件 - 多图
    /// </summary>
    public static readonly CardComponentType ImageGroup = new("image-group");
        
    /// <summary>
    /// 内容组件 - 视频
    /// </summary>
    public static readonly CardComponentType Video = new("video");
        
    /// <summary>
    /// 内容组件 - 倒计时
    /// </summary>
    public static readonly CardComponentType Countdown = new("countdown");
        
    /// <summary>
    /// 内容组件 - 分割线
    /// </summary>
    public static readonly CardComponentType Divider = new("divider");
    
        
    /// <summary>
    /// 交互组件 - 按钮
    /// </summary>
    public static readonly CardComponentType ButtonGroup = new("button-group");
        
    /// <summary>
    /// 交互组件 - 列表选择器
    /// </summary>
    public static readonly CardComponentType ListSelector = new("list-selector");
        
    /// <summary>
    /// 交互组件 - 文字 + 模块
    /// </summary>
    public static readonly CardComponentType TextWithModule = new("section");
}
