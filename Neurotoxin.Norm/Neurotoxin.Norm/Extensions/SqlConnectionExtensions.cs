using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

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
            var properties = type.GetProperties().ToList();
            var listType = typeof(List<>).MakeGenericType(type);
            var addMethod = listType.GetMethod("Add");
            var list = Activator.CreateInstance(listType);
            var proxyType = DynamicProxy.Instance.GetProxyType(type);

            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Transaction = transaction;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var instance = Activator.CreateInstance(proxyType);
                    var columns = new HashSet<string>();
                    for(var i=0; i < reader.FieldCount; i++)
                    {
                       columns.Add(reader.GetName(i));
                    }

                    foreach (var pi in properties)
                    {
                        if (columns.Contains(pi.Name) && pi.CanWrite)
                        {
                            var stringValue = reader[pi.Name].ToString();
                            object value = null;
                            //TODO: proper mapping
                            if (pi.PropertyType == typeof(Type))
                            {
                                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    value = assembly.GetTypes().FirstOrDefault(t => t.FullName == stringValue);
                                    if (value != null) break;
                                }
                                if (value == null) throw new Exception("Invalid type: " + stringValue);
                            }
                            else if (pi.PropertyType == typeof (Boolean))
                            {
                                bool b;
                                if (!Boolean.TryParse(stringValue, out b)) throw new Exception("Cannot parse Boolean value: " + stringValue);
                                value = b;
                            }
                            else if (pi.PropertyType.IsEnum)
                            {
                                var intValue = Int32.Parse(stringValue);
                                value = Enum.ToObject(pi.PropertyType, intValue);
                            }
                            else if (pi.PropertyType == typeof (Guid))
                            {
                                value = Guid.Parse(stringValue);
                            }
                            else if (pi.PropertyType.IsValueType)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                value = stringValue;
                            }
                            pi.SetValue(instance, value);
                        }
                    }
                    addMethod.Invoke(list, new [] {instance});
                }
                reader.Close();
            }
            return (IEnumerable)list;
        }
    }
}