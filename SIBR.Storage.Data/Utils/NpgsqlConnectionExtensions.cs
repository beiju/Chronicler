﻿using System.Collections.Generic;
using System.Data.Common;
using Dapper;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System;

namespace SIBR.Storage.Data.Utils
{
    public static class NpgsqlConnectionExtensions
    {
        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(this NpgsqlConnection conn, string sql, object param)
        {
            await using var reader = await conn.ExecuteReaderAsync(sql, param);
            var parser = reader.GetRowParser<T>();
            
            while (await reader.ReadAsync())
                yield return parser(reader); 
        }

        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(this Database db, string sql, object param)
        {
            await using var conn = await db.Obtain();
            
            await foreach (var value in conn.QueryStreamAsync<T>(sql, param))
                yield return value;
        }

        public static async IAsyncEnumerable<T> QueryKataAsync<T>(this NpgsqlConnection conn, SqlKata.Query query)
        {
            var compiled = new PostgresCompiler().Compile(query);
            Console.WriteLine(compiled.Sql);
            await using var reader = await conn.ExecuteReaderAsync(compiled.Sql, compiled.NamedBindings);

            var parser = reader.GetRowParser<T>();
            while (await reader.ReadAsync())
                yield return parser(reader); 
        }
        
        public static async IAsyncEnumerable<T> QueryKataAsync<T>(this Database db, SqlKata.Query query)
        {
            await using var conn = await db.Obtain();
            
            await foreach (var value in conn.QueryKataAsync<T>(query))
                yield return value;
        }
    }
}