using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    [Table("__MigrationHistory")]
    public class ColumnInfo
    {
        public string TableName { get; set; }
        public string TableSchema { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public string PropertyName { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public IndexType? IndexType { get; set; }

        [Ignore] public object DefaultValue { get; set; }
        [Ignore] public bool IsDiscriminatorColumn { get; set; }
        [Ignore] public List<Type> DeclaringTypes { get; set; }

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

        public ColumnExpression ToColumnExpression(string alias = null)
        {
            var type = PropertyName == null ? typeof(Type) : DeclaringTypes[0].GetProperty(PropertyName).PropertyType;
            return new ColumnExpression(ColumnName, alias, type);
        }

        public BinaryExpression ToEqualExpression(object obj, string alias = null)
        {
            var value = GetValue(obj);
            return Expression.MakeBinary(ExpressionType.Equal, ToColumnExpression(alias), Expression.Constant(value));
        }

        public object GetValue(object obj)
        {
            return obj.GetType().GetProperty(PropertyName).GetValue(obj);
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