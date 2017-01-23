using System;
using System.Collections.Generic;

namespace Neurotoxin.Soulshot
{
    internal static class TypeSystem
    {
        internal static Type GetElementType(Type seqType)
        {
            var ienum = FindIEnumerable(seqType);
            return ienum == null ? seqType : ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            while (true)
            {
                if (seqType == null || seqType == typeof (string)) return null;
                if (seqType.IsArray) return typeof (IEnumerable<>).MakeGenericType(seqType.GetElementType());

                if (seqType.IsGenericType)
                {
                    foreach (var arg in seqType.GetGenericArguments())
                    {
                        var ienum = typeof (IEnumerable<>).MakeGenericType(arg);
                        if (ienum.IsAssignableFrom(seqType)) return ienum;
                    }
                }

                var ifaces = seqType.GetInterfaces();
                if (ifaces.Length > 0)
                {
                    foreach (var iface in ifaces)
                    {
                        var ienum = FindIEnumerable(iface);
                        if (ienum != null) return ienum;
                    }
                }

                if (seqType.BaseType != null && seqType.BaseType != typeof (object))
                {
                    seqType = seqType.BaseType;
                    continue;
                }

                return null;
            }
        }
    }
}