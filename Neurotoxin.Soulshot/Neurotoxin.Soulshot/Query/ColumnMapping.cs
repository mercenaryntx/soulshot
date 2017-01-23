using System;
using System.Collections.Generic;
using System.Reflection;
using PetaPoco;

namespace Neurotoxin.Soulshot.Query
{
    public class ColumnMapping
    {
        public const string DiscriminatorColumnName = "Discriminator";

        public PropertyInfo Property { get; set; }
        public ColumnAttribute Column { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsDiscriminatorColumn { get; set; }
        public int Index { get; set; }

        public string ColumnName => Column?.Name ?? Property.Name;
        public Type Type
        {
            get
            {
                if (IsDiscriminatorColumn) return typeof (string);
                var type = Property.PropertyType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    type = Nullable.GetUnderlyingType(type);
                return type;
            }
        }

        public List<Type> DeclaringTypes { get; set; }

        public ColumnMapping Clone(int index)
        {
            return new ColumnMapping
            {
                Property = Property,
                Column = Column,
                IsPrimaryKey = IsPrimaryKey,
                IsDiscriminatorColumn = IsDiscriminatorColumn,
                Index = index
            };
        }

        public object GetValue(object obj)
        {
            var type = obj.GetType();
            return IsDiscriminatorColumn
                ? type.FullName
                : Property.DeclaringType.IsAssignableFrom(type)
                    ? Property.GetValue(obj) 
                    : Property.PropertyType.IsValueType 
                        ? Activator.CreateInstance(Property.PropertyType) 
                        : null;
        }
    }
}