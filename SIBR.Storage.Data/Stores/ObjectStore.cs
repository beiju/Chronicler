﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using SIBR.Storage.Data.Models;

namespace SIBR.Storage.Data
{
    public class ObjectStore
    {
        private readonly ConcurrentDictionary<Guid, bool> _knownObjects = new ConcurrentDictionary<Guid, bool>();
        
        public async Task SaveObjects(NpgsqlConnection conn, IEnumerable<IJsonObject> updates)
        {
            var dictionary = new Dictionary<Guid, JToken>();
            foreach (var obj in updates)
            {
                if (_knownObjects.TryAdd(obj.Hash, true))
                    dictionary[obj.Hash] = obj.Data;
            }
            
            var entries = dictionary.ToList();
            
            await conn.ExecuteAsync(
                "insert into objects (hash, data) select unnest(@Hash), unnest(@Data)::jsonb on conflict do nothing",
                new
                {
                    Hash = entries.Select(e => e.Key).ToArray(),
                    Data = entries.Select(e => e.Value.ToString(Formatting.None)).ToArray()
                });
        }
        
        public async Task SaveBinaryObjects(NpgsqlConnection conn, IEnumerable<IHashedObject<byte[]>> updates)
        {
            var dictionary = new Dictionary<Guid, byte[]>();
            foreach (var obj in updates)
                dictionary[obj.Hash] = obj.Data;
            var entries = dictionary.ToList();

            await conn.ExecuteAsync(
                "insert into binary_objects (hash, data) select unnest(@Hash), unnest(@Data) on conflict do nothing",
                new
                {
                    Hash = entries.Select(e => e.Key).ToArray(),
                    Data = entries.Select(e => e.Value).ToArray()
                });
        }
    }
}