using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm
{
    [Table("__MigrationHistory")]
    public class ColumnInfo
    {
        public string TableName { get; set; }
        public string TableSchema { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public Type BaseType { get; set; }
        public string PropertyName { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }

        public string DefinitionString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("[");
                sb.Append(ColumnName);
                sb.Append("] ");
                sb.Append(ColumnType);
                if (IsIdentity) sb.Append(" IDENTITY(1,1)");
                if (!IsNullable) sb.Append(" NOT");
                sb.Append(" NULL");
                return sb.ToString();
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ColumnInfo) obj);
        }

        protected bool Equals(ColumnInfo other)
        {
            return string.Equals(TableName, other.TableName) && string.Equals(TableSchema, other.TableSchema) && string.Equals(ColumnName, other.ColumnName) && string.Equals(ColumnType, other.ColumnType) && string.Equals(PropertyName, other.PropertyName) && IsNullable.Equals(other.IsNullable) && IsIdentity.Equals(other.IsIdentity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (TableName != null ? TableName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TableSchema != null ? TableSchema.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ColumnName != null ? ColumnName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ColumnType != null ? ColumnType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PropertyName != null ? PropertyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsNullable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsIdentity.GetHashCode();
                return hashCode;
            }
        }
    
    }
}
