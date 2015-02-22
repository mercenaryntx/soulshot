using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Neurotoxin.Norm.Extensions
{
    public static class TypeBuilderExtensions
    {
        public static FieldBuilder CreateField<T>(this TypeBuilder typeBuilder, string name)
        {
            return CreateField(typeBuilder, typeof(T), name);
        }

        public static FieldBuilder CreateField(this TypeBuilder typeBuilder, Type fieldType, string name)
        {
            return typeBuilder.DefineField(name, fieldType, FieldAttributes.Private);
        }

        public static PropertyBuilder CreateProperty<T>(this TypeBuilder typeBuilder, string name, Func<FieldBuilder, MethodBuilder> getter, Func<FieldBuilder, MethodBuilder> setter, bool createBackingField = true)
        {
            return CreateProperty(typeBuilder, typeof(T), name, getter, setter, createBackingField);
        }

        public static PropertyBuilder CreateProperty(this TypeBuilder typeBuilder, Type propertyType, string name, Func<FieldBuilder, MethodBuilder> getter, Func<FieldBuilder, MethodBuilder> setter, bool createBackingField = true)
        {
            FieldBuilder backingField = null;
            if (createBackingField)
            {
                var fieldName = new StringBuilder("_");
                fieldName.Append(name.Substring(0, 1).ToLower());
                fieldName.Append(name.Substring(1));
                backingField = CreateField(typeBuilder, propertyType, fieldName.ToString());
            }
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, CallingConventions.Any, propertyType, Type.EmptyTypes);
            var getMethod = getter.Invoke(backingField);
            if (getMethod != null) propertyBuilder.SetGetMethod(getMethod);
            var setMethod = setter.Invoke(backingField);
            if (setMethod != null) propertyBuilder.SetSetMethod(setMethod);
            return propertyBuilder;
        }

        public static ConstructorBuilder CreateDefaultConstructor(this TypeBuilder typeBuilder, Action<ILGenerator> setConstructorBody)
        {
            var constructor = typeBuilder.BaseType.GetConstructor(Type.EmptyTypes);
            if (constructor == null) throw new Exception("Parameterless constructor is a must");
            //var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            var constructorBuilder = typeBuilder.DefineConstructor
                (
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, 
                    CallingConventions.Standard, 
                    Type.EmptyTypes
                );
            var il = constructorBuilder.GetILGenerator();
            setConstructorBody.Invoke(il);
            return constructorBuilder;
        }
    }
}