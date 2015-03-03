﻿using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class Int64Mapper : MapperBase
    {
        public Int64Mapper() : base(typeof(Int64), new BigIntAttribute())
        {
        }
    }
}