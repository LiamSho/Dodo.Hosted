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

using DodoHosted.Base.App.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DodoHosted.Base.App;

public class PluginConfigurationManager
{
    private readonly string _pluginId;
    private const string IdentifierField = "plugin-identifier";

    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly FilterDefinition<BsonDocument> _filter;

    public PluginConfigurationManager(IMongoDatabase mongoDatabase, string pluginId)
    {
        _pluginId = pluginId;
        _collection = mongoDatabase.GetCollection<BsonDocument>(HostConstants.MONGO_COLLECTION_PLUGIN_OPTIONS);

        _filter = Builders<BsonDocument>.Filter.Eq(IdentifierField, pluginId);

        if (_collection.Find(_filter).CountDocuments() == 0)
        {
            _collection.InsertOne(new BsonDocument { { IdentifierField, pluginId } });
        }
    }

    #region String

    public async Task SetStringValue(string key, string value)
    {
        await SetValue(key, value);
    }
    
    public async Task<string> GetStringValue(string key)
    {
        return (await GetValue<BsonString>(key)).AsString;
    }
    
    #endregion
    
    #region Int32

    public async Task SetIntValue(string key, int value)
    {
        await SetValue(key, value);
    }
    
    public async Task<int> GetIntValue(string key)
    {
        return (await GetValue<BsonInt32>(key)).AsInt32;
    }

    #endregion
    
    #region Int64

    public async Task SetLongValue(string key, long value)
    {
        await SetValue(key, value);
    }
    
    public async Task<long> GetLongValue(string key)
    {
        return (await GetValue<BsonInt64>(key)).AsInt64;
    }

    #endregion
    
    #region Double

    public async Task SetDoubleValue(string key, double value)
    {
        await SetValue(key, value);
    }
    
    public async Task<double> GetDoubleValue(string key)
    {
        return (await GetValue<BsonDouble>(key)).AsDouble;
    }

    #endregion
    
    #region Bool

    public async Task SetBoolValue(string key, bool value)
    {
        await SetValue(key, value);
    }
    
    public async Task<bool> GetBoolValue(string key)
    {
        return (await GetValue<BsonBoolean>(key)).AsBoolean;
    }

    #endregion

    #region Object

    public async Task SetObjectValue<T>(string key, T obj)
    {
        var document = obj.ToBsonDocument();
        await SetValue(key, document);
    }

    public async Task<BsonDocument> GetObjectValue(string key)
    {
        return (await GetValue<BsonDocument>(key)).AsBsonDocument;
    }

    public async Task<T> GetObjectValue<T>(string key)
    {
        var document = await GetObjectValue(key);
        return BsonSerializer.Deserialize<T>(document);
    }

    #endregion
    
    #region BsonValue

    public async Task SetValue(string key, BsonValue value)
    {
        var record = await GetRecord();
        CheckKey(key);
        record.Set(key, value);
        await UpdateRecord(record);
    }

    public async Task<T> GetValue<T>(string key) where T : BsonValue
    {
        var value = await GetValue(key);
        
        if (value is T v)
        {
            return v;
        }
        
        throw new PluginConfigurationException(_pluginId, key, typeof(T));
    }

    public async Task<BsonValue> GetValue(string key)
    {
        var record = await GetRecord();

        try
        {
            return record.GetValue(key);
        }
        catch (Exception)
        {
            throw new PluginConfigurationException(_pluginId, key);
        }
    }

    public bool TryGetValue(string key, out BsonValue? value)
    {
        try
        {
            value = GetValue(key).ConfigureAwait(false).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }

    public bool TryGetValue<T>(string key, out T? value) where T : BsonValue
    {
        try
        {
            value = GetValue<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }
    
    #endregion

    private async Task<BsonDocument> GetRecord()
    { 
        var document = await _collection.Find(_filter).FirstOrDefaultAsync();
        if (document is null)
        {
            throw new InvalidOperationException("找不到插件配置");
        }

        return document;
    }

    private async Task UpdateRecord(BsonDocument document)
    {
        await _collection.ReplaceOneAsync(_filter, document);
    }

    private static void CheckKey(string name)
    {
        if (name == IdentifierField)
        {
            throw new PluginConfigurationException(name);
        }
    }
}
