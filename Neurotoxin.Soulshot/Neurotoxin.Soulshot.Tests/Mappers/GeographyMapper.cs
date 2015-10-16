using System;
using System.Data.Spatial;
using System.Diagnostics;
using System.Windows.Markup;
using Neurotoxin.Soulshot.Mappers;

namespace Neurotoxin.Soulshot.Tests.Mappers
{
    public class GeographyMapper : MapperBase
    {
        public GeographyMapper() : base(typeof(DbGeography), new GeographyAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            var dbg = value as DbGeography;
            return dbg == null ? "null" : string.Format("geography::STGeomFromText('{0}', 4326)", dbg.AsText());
        }

        public override object MapToType(object value, Type type)
        {
            //TODO: null, other types
            return DbGeography.FromText(value.ToString());
            //TODO: else
            return base.MapToType(value, type);
        }
    }
}