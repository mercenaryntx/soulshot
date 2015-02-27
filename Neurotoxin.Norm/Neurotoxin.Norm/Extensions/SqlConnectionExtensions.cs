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

        public static void ExecuteNonQuery(this SqlConnection connection, string command)
        {
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static List<T> ExecuteQuery<T>(this SqlConnection connection, string command)
        {
            return (List<T>)ExecuteQuery(connection, typeof (T), command);
        }

        public static IEnumerable ExecuteQuery(this SqlConnection connection, Type type, string command)
        {
            var properties = type.GetProperties().ToList();
            var listType = typeof(List<>).MakeGenericType(type);
            var addMethod = listType.GetMethod("Add");
            var list = Activator.CreateInstance(listType);
            var proxyType = DynamicProxy.Instance.GetProxyType(type);

            using (var cmd = new SqlCommand(command, connection))
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var instance = Activator.CreateInstance(proxyType);
                    var columns = new HashSet<string>();
                    for(int i=0;i<reader.FieldCount;i++)
                    {
                       columns.Add(reader.GetName(i));
                    }

                    foreach (var pi in properties)
                    {
                        if (columns.Contains(pi.Name) && pi.CanWrite)
                        {
                            var stringValue = reader[pi.Name].ToString();
                            object value = null;
                            //TODO:
                            if (pi.PropertyType == typeof(Type))
                            {
                                value = Type.GetType(stringValue);
                            }
                            else if (pi.PropertyType == typeof (Boolean))
                            {
                                value = stringValue == "1";
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