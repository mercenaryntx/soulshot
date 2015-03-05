using System;
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

        public T MapToType<T>(object value)
        {
            return (T)MapToType(value, typeof (T));
        }

        public virtual object MapToType(object value, Type type)
        {
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