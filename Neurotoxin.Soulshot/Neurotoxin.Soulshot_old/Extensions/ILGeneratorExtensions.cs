using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class ILGeneratorExtensions
    {
        public static void SetFieldDefault(this ILGenerator il, FieldInfo field)
        {
            il.Emit(OpCodes.Ldarg_0);
            if (field.FieldType.IsClass)
            {
                il.Emit(OpCodes.Newobj, field.FieldType.GetConstructor(Type.EmptyTypes));
            }
            else
            {
                //TODO:
                il.Emit(OpCodes.Ldc_I4_0);
            }
            il.Emit(OpCodes.Stfld, field);

        }
    }
}
