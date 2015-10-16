using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    [Table("__MigrationHistory")]
    public class ColumnInfo
    {
        public virtual string TableName { get; set; }
        public virtual string TableSchema { get; set; }
        public virtual string ColumnName { get; set; }
        public virtual string ColumnType { get; set; }
        public virtual string PropertyName { get; set; }
        public virtual bool IsNullable { get; set; }
        public virtual bool IsIdentity { get; set; }
        public virtual IndexType? IndexType { get; set; }

        [Ignore] public object DefaultValue { get; set; }
        [Ignore] public bool IsDiscriminatorColumn { get; set; }
        [Ignore] public List<Type> DeclaringTypes { get; set; }
        [Ignore] public IColumnInfoCollection ReferenceTable { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!obj.GetType().IsInstanceOfType(this) && !GetType().IsInstanceOfType(obj)) return false;
            return Equals((ColumnInfo) obj);
        }

        protected bool Equals(ColumnInfo other)
        {
            return string.Equals(TableName, other.TableName) && 
                string.Equals(TableSchema, other.TableSchema) && 
                string.Equals(ColumnName, other.ColumnName) && 
                string.Equals(ColumnType, other.ColumnType) && 
                string.Equals(PropertyName, other.PropertyName) && 
                IsNullable.Equals(other.IsNullable) && 
                IsIdentity.Equals(other.IsIdentity) &&
                IndexType.Equals(other.IndexType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (TableName != null ? TableName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (TableSchema != null ? TableSchema.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ColumnName != null ? ColumnName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ColumnType != null ? ColumnType.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (PropertyName != null ? PropertyName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ IsNullable.GetHashCode();
                hashCode = (hashCode*397) ^ IsIdentity.GetHashCode();
                hashCode = (hashCode * 397) ^ IndexType.GetHashCode();
                return hashCode;
            }
        }

        public ColumnExpression ToColumnExpression(TableExpression table = null, Type type = null)
        {
            if (type == null)
                type = PropertyName == null ? typeof (Type) : DeclaringTypes[0].GetProperty(PropertyName).PropertyType;
            return new ColumnExpression(ColumnName, table, type);
        }

        public BinaryExpression ToEqualExpression(object obj, TableExpression table = null)
        {
            var value = GetValue(obj);
            var column = ToColumnExpression(table);
            return Expression.MakeBinary(ExpressionType.Equal, column, Expression.Constant(value, column.Type));
        }

        public object GetValue(object obj)
        {
            return GetProperty(obj).GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            var property = GetProperty(obj);
            if (property == null)
            {
                Debugger.Break();
            }
            property.SetValue(obj, value);
        }

        private PropertyInfo GetProperty(object obj)
        {
            return obj.GetType().GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public bool DescribesProperty(MemberInfo property)
        {
            if (DeclaringTypes == null || PropertyName == null) return false;
            if (PropertyName != property.Name) return false;

            var declaringType = property.DeclaringType;
            do
            {
                if (DeclaringTypes.Contains(declaringType)) return true;
                declaringType = declaringType.BaseType;
            } 
            while (declaringType != null);

            return false;
        }
    }
}