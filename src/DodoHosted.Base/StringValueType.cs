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

namespace DodoHosted.Base;

public abstract class StringValueType
{
    private readonly string _value;

    protected StringValueType(string value)
    {
        _value = value;
    }
    
    public override string ToString()
    {
        return _value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is StringValueType value)
        {
            return this.Equals(value);
        }

        return false;
    }

    private bool Equals(StringValueType other)
    {
        return _value == other._value;
    }
    
    public override int GetHashCode()
    {
        return this._value.GetHashCode();
    }

    public static bool operator ==(StringValueType? a, StringValueType? b) => a is not null && b is not null && a.Equals(b);
    public static bool operator !=(StringValueType? a, StringValueType? b) => a is not null && b is not null && !a.Equals(b);

    public static implicit operator string(StringValueType value) => value._value;

    public static T? Parse<T>(string? value) where T : StringValueType, IStringValueType<T>
    {
        return T.SupportedValues.FirstOrDefault(supportedValue => supportedValue._value == value);
    }
}
