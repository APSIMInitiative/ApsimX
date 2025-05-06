﻿using System.Collections.Concurrent;
using System.Reflection;

namespace DeepCloner.Core.Helpers;

/// <summary>
/// Safe types are types, which can be copied without real cloning. e.g. simple structs or strings (it is immutable)
/// </summary>
internal static class DeepClonerSafeTypes
{
    internal static readonly ConcurrentDictionary<Type, bool> KnownTypes = new()
    {
        // Primitives
        [typeof(byte)] = true,
        [typeof(short)] = true,
        [typeof(ushort)] = true,
        [typeof(int)] = true,
        [typeof(uint)] = true,
        [typeof(long)] = true,
        [typeof(ulong)] = true,
        [typeof(float)] = true,
        [typeof(double)] = true,
        [typeof(decimal)] = true,
        [typeof(string)] = true,
        [typeof(DateTime)] = true,
        [typeof(DateTimeOffset)] = true,
#if !OLDFRAMEWORK
        [typeof(DateOnly)] = true,
        [typeof(TimeOnly)] = true,
#endif
        [typeof(IntPtr)] = true,
        [typeof(UIntPtr)] = true,
        [typeof(Guid)] = true,
        
        // Others
        [typeof(DBNull)] = true,
        [StringComparer.Ordinal.GetType()] = true,
        [StringComparer.OrdinalIgnoreCase.GetType()] = true,
    };

    static DeepClonerSafeTypes()
    {
        foreach (
            var x in
            new[]
            {
                Type.GetType("System.RuntimeType"),
                Type.GetType("System.RuntimeTypeHandle"),
                StringComparer.InvariantCulture.GetType(),
                StringComparer.InvariantCultureIgnoreCase.GetType(),
            }) KnownTypes.TryAdd(x, true);
    }

    private static bool CanReturnSameType(Type type, HashSet<Type>? processingTypes)
    {
        if (KnownTypes.TryGetValue(type, out var isSafe))
            return isSafe;

        // enums are safe
        // pointers (e.g. int*) are unsafe, but we cannot do anything with it except blind copy
        if (type.IsEnum() || type.IsPointer)
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

#if OLDFRAMEWORK
        // do not do anything with remoting. it is very dangerous to clone, bcs it relate to deep core of framework
        if (type.FullName.StartsWith("System.Runtime.Remoting.")
            && type.Assembly == typeof(System.Runtime.Remoting.CustomErrorsModes).Assembly)
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }
#endif

        if (type.FullName.StartsWith("System.Reflection.") && type.Assembly == typeof(PropertyInfo).Assembly)
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        // this types are serious native resources, it is better not to clone it
        if (type.IsSubclassOf(typeof(System.Runtime.ConstrainedExecution.CriticalFinalizerObject)))
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        // Better not to do anything with COM
        if (type.IsCOMObject)
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        if (type.FullName.StartsWith("System.RuntimeType"))
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        if (type.FullName.StartsWith("System.Reflection.") && Equals(type.GetTypeInfo().Assembly, typeof(PropertyInfo).GetTypeInfo().Assembly))
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        if (type.IsSubclassOfTypeByName("CriticalFinalizerObject"))
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        // better not to touch ms dependency injection
        if (type.FullName.StartsWith("Microsoft.Extensions.DependencyInjection."))
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        if (type.FullName == "Microsoft.EntityFrameworkCore.Internal.ConcurrencyDetector")
        {
            KnownTypes.TryAdd(type, true);
            return true;
        }

        // default comparers should not be cloned due possible comparison EqualityComparer<T>.Default == comparer
        if (type.FullName.Contains("EqualityComparer"))
        {
            if (type.FullName.StartsWith("System.Collections.Generic.GenericEqualityComparer`")
                || type.FullName.StartsWith("System.Collections.Generic.ObjectEqualityComparer`")
                || type.FullName.StartsWith("System.Collections.Generic.EnumEqualityComparer`")
                || type.FullName.StartsWith("System.Collections.Generic.NullableEqualityComparer`")
                || type.FullName == "System.Collections.Generic.ByteEqualityComparer")
            {
                KnownTypes.TryAdd(type, true);
                return true;
            }
        }

        // classes are always unsafe (we should copy it fully to count references)
        if (!type.IsValueType())
        {
            KnownTypes.TryAdd(type, false);
            return false;
        }

        processingTypes ??= new();

        // structs cannot have a loops, but check it anyway
        processingTypes.Add(type);

        List<FieldInfo> fi = new List<FieldInfo>();
        var tp = type;
        do
        {
            fi.AddRange(tp.GetAllFields());
            tp = tp.BaseType();
        }
        while (tp != null);

        foreach (var fieldInfo in fi)
        {
            // type loop
            var fieldType = fieldInfo.FieldType;
            if (processingTypes.Contains(fieldType))
                continue;

            // not safe and not not safe. we need to go deeper
            if (!CanReturnSameType(fieldType, processingTypes))
            {
                KnownTypes.TryAdd(type, false);
                return false;
            }
        }

        KnownTypes.TryAdd(type, true);
        return true;
    }

    public static bool CanReturnSameObject(Type type)
    {
        return CanReturnSameType(type, null);
    }
}