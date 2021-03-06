﻿using System;
using System.Collections.Generic;
using System.Linq;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class TypeMapper : MapperBase
    {
        private readonly Dictionary<string, Type> _cache = new Dictionary<string, Type>();

        public TypeMapper() : base(typeof(Type), new NVarcharAttribute(255))
        {
        }

        public override object MapToType(object value, Type type)
        {
            var stringValue = (string)value;
            if (_cache.ContainsKey(stringValue)) return _cache[stringValue];

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var typeValue = assembly.GetTypes().FirstOrDefault(t => t.FullName == stringValue);
                if (typeValue == null) continue;

                _cache.Add(stringValue, typeValue);
                return typeValue;
            }
            throw new Exception("Invalid type: " + stringValue);
        }

        public override string MapToSql(object value)
        {
            return string.Format("'{0}'", ((Type)value).FullName);
        }
    }
}