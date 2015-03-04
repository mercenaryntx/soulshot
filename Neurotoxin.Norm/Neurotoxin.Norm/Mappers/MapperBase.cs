﻿using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public abstract class MapperBase
    {
        public Type PropertyType { get; private set; }
        public ColumnTypeAttribute ColumnType { get; private set; }

        public MapperBase(Type propertyType, ColumnTypeAttribute columnType)
        {
            PropertyType = propertyType;
            ColumnType = columnType;
        }

        public virtual object MapToType(object value)
        {
            if (value is DBNull) return null;
            if (value.GetType() == PropertyType) return value;
            throw new NotSupportedException(string.Format("Not supported mapping: {0} -> {1}", value.GetType(), PropertyType));
        }

        public virtual string MapToSql(object value)
        {
            //TODO
            return value.ToString();
        }
    }
}