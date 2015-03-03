using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Neurotoxin.Norm.Extensions
{
    public static class SqlConnectionExtensions
    {
        public static T ExecuteScalar<T>(this SqlConnection connection, string command)
        {
            using (var cmd = new SqlCommand(command, connection))
            {
                return (T)cmd.ExecuteScalar();
            }
        }

        public static void ExecuteNonQuery(this SqlConnection connection, string command, SqlTransaction transaction = null)
        {
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }

        public static List<T> ExecuteQuery<T>(this SqlConnection connection, string command, SqlTransaction transaction = null)
        {
            return (List<T>)ExecuteQuery(connection, typeof (T), command, transaction);
        }

        public static IEnumerable ExecuteQuery(this SqlConnection connection, Type type, string command, SqlTransaction transaction = null)
        {
            var listType = typeof(List<>).MakeGenericType(type);
            var addMethod = listType.GetMethod("Add");
            var list = Activator.CreateInstance(listType);

            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Transaction = transaction;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var entityType = type;
                    var start = 0;
                    if (reader.GetName(0) == ColumnMapper.DiscriminatorColumnName)
                    {
                        entityType = ColumnMapper.MapType(reader.GetString(0));
                        start = 1;
                    }
                    var proxyType = DynamicProxy.Instance.GetProxyType(entityType);
                    var instance = Activator.CreateInstance(proxyType);

                    for(var i = start; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = reader.GetValue(i);
                        var pi = entityType.GetProperty(name);
                        if (!pi.CanWrite) continue;
                        pi.SetValue(instance, ColumnMapper.MapToPropertyValue(value, pi));
                    }
                    addMethod.Invoke(list, new [] {instance});
                }
                reader.Close();
            }
            return (IEnumerable)list;
        }
    }
}