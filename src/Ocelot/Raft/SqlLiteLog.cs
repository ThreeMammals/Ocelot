using System.IO;
using Rafty.Log;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using Rafty.Infrastructure;
using System.Collections.Generic;

namespace Ocelot.Raft
{
    [ExcludeFromCoverage]
    public class SqlLiteLog : ILog
    {
        private string _path;
        private readonly object _lock = new object();

        public SqlLiteLog(NodeId nodeId)
        {
            _path = $"{nodeId.Id.Replace("/","").Replace(":","")}.db";
            if(!File.Exists(_path))
            {
                lock(_lock)
                {
                    FileStream fs = File.Create(_path);
                    fs.Dispose();
                }
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    var sql = @"create table logs (
                        id integer primary key,
                        data text not null
                    )";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var result = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public int LastLogIndex
        {
            get
            {
                lock(_lock)
                {
                    var result = 1;
                    using(var connection = new SqliteConnection($"Data Source={_path};"))
                    {
                        connection.Open();
                        var sql = @"select id from logs order by id desc limit 1";
                        using(var command = new SqliteCommand(sql, connection))
                        {
                            var index = Convert.ToInt32(command.ExecuteScalar());
                            if(index > result)
                            {
                                result = index;
                            }
                        }
                    }
                    return result;
                }
            }
        }

        public long LastLogTerm 
        {
            get
            {
                lock(_lock)
                {
                    long result = 0;
                    using(var connection = new SqliteConnection($"Data Source={_path};"))
                    {
                        connection.Open();
                        var sql = @"select data from logs order by id desc limit 1";
                        using(var command = new SqliteCommand(sql, connection))
                        {
                            var data = Convert.ToString(command.ExecuteScalar());
                            var jsonSerializerSettings = new JsonSerializerSettings() { 
                                TypeNameHandling = TypeNameHandling.All
                            };
                            var log = JsonConvert.DeserializeObject<LogEntry>(data, jsonSerializerSettings);
                            if(log != null && log.Term > result)
                            {
                                result = log.Term;
                            }
                        }
                    }
                    return result;
                }
            }
        }

        public int Count 
        {
            get 
            {
                lock(_lock)
                {
                    var result = 0;
                    using(var connection = new SqliteConnection($"Data Source={_path};"))
                    {
                        connection.Open();
                        var sql = @"select count(id) from logs";
                        using(var command = new SqliteCommand(sql, connection))
                        {
                            var index = Convert.ToInt32(command.ExecuteScalar());
                            if(index > result)
                            {
                                result = index;
                            }
                        }
                    }
                    return result;
                }
            }
        }

        public int Apply(LogEntry log)
        {
            lock(_lock)
            {
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    var jsonSerializerSettings = new JsonSerializerSettings() { 
                        TypeNameHandling = TypeNameHandling.All
                    };
                    var data = JsonConvert.SerializeObject(log, jsonSerializerSettings);
                    //todo - sql injection dont copy this..
                    var sql = $"insert into logs (data) values ('{data}')";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var result = command.ExecuteNonQuery();
                    }
                    
                    sql = "select last_insert_rowid()";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }   
                }
            }
        }

        public void DeleteConflictsFromThisLog(int index, LogEntry logEntry)
        {
            lock(_lock)
            {
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    //todo - sql injection dont copy this..
                    var sql = $"select data from logs where id = {index};";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var data = Convert.ToString(command.ExecuteScalar());
                        var jsonSerializerSettings = new JsonSerializerSettings() { 
                            TypeNameHandling = TypeNameHandling.All
                        };
                        var log = JsonConvert.DeserializeObject<LogEntry>(data, jsonSerializerSettings);
                        if(logEntry != null && log != null && logEntry.Term != log.Term)
                        {
                            //todo - sql injection dont copy this..
                            var deleteSql = $"delete from logs where id >= {index};";
                            using(var deleteCommand = new SqliteCommand(deleteSql, connection))
                            {
                            var result = deleteCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        public LogEntry Get(int index)
        {
            lock(_lock)
            {
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    //todo - sql injection dont copy this..
                    var sql = $"select data from logs where id = {index}";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var data = Convert.ToString(command.ExecuteScalar());
                        var jsonSerializerSettings = new JsonSerializerSettings() { 
                            TypeNameHandling = TypeNameHandling.All
                        };
                        var log = JsonConvert.DeserializeObject<LogEntry>(data, jsonSerializerSettings);
                        return log;
                    }
                }
            }
        }

        public System.Collections.Generic.List<(int index, LogEntry logEntry)> GetFrom(int index)
        {
            lock(_lock)
            {
                var logsToReturn = new List<(int, LogEntry)>();

                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    //todo - sql injection dont copy this..
                    var sql = $"select id, data from logs where id >= {index}";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        using(var reader = command.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                var id = Convert.ToInt32(reader[0]);
                                var data = (string)reader[1];
                                var jsonSerializerSettings = new JsonSerializerSettings() { 
                                    TypeNameHandling = TypeNameHandling.All
                                };
                                var log = JsonConvert.DeserializeObject<LogEntry>(data, jsonSerializerSettings);
                                logsToReturn.Add((id, log));

                            }
                        }
                    }
                }

                return logsToReturn;
            }
          
        }

        public long GetTermAtIndex(int index)
        {
            lock(_lock)
            {
                long result = 0;
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    //todo - sql injection dont copy this..
                    var sql = $"select data from logs where id = {index}";
                    using(var command = new SqliteCommand(sql, connection))
                    {
                        var data = Convert.ToString(command.ExecuteScalar());
                        var jsonSerializerSettings = new JsonSerializerSettings() { 
                            TypeNameHandling = TypeNameHandling.All
                        };
                        var log = JsonConvert.DeserializeObject<LogEntry>(data, jsonSerializerSettings);
                        if(log != null && log.Term > result)
                        {
                            result = log.Term;
                        }
                    }
                }
                return result;
            }
        }
        public void Remove(int indexOfCommand)
        {
            lock(_lock)
            {
                using(var connection = new SqliteConnection($"Data Source={_path};"))
                {
                    connection.Open();
                    //todo - sql injection dont copy this..
                    var deleteSql = $"delete from logs where id >= {indexOfCommand};";
                    using(var deleteCommand = new SqliteCommand(deleteSql, connection))
                    {
                        var result = deleteCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}