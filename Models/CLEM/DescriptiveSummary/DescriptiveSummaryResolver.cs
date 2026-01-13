using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Models.CLEM.Interfaces;
using Models.Core;
#nullable enable

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider resolver and cache.
    /// </summary>
    public static class DescriptiveSummaryResolver
    {
        private static readonly ConcurrentDictionary<Type, Type?> providerTypeCache = new();
        private static readonly object scanLock = new();

        /// <summary>
        /// Get the provider instance for the supplied model object.
        /// </summary>
        /// <param name="modelObj">model for which summary is needed</param>
        /// <param name="generator"></param>
        /// <returns>Returns an instance of the matching closed generic provider implementing IDescriptiveSummaryProvider{TModel} for the provided model object. Returns null if no provider is found</returns>
        public static IDescriptiveSummaryProvider GetProviderInstance(object modelObj, DescriptiveSummaryGenerator generator)
        {
            ArgumentNullException.ThrowIfNull(modelObj);
            ArgumentNullException.ThrowIfNull(generator);

            var modelType = modelObj.GetType();

            // get provider Type (or null) from cache (FindProviderTypeForModelType remains unchanged)
            var providerType = providerTypeCache.GetOrAdd(modelType, FindProviderTypeForModelType);

            IDescriptiveSummaryProvider provider;
            if (providerType == null)
            {
                // fallback to default provider implementation
                provider = new DefaultDescriptiveSummaryProvider();
            }
            else
            {
                object? created;
                created = null;
                var ctors = providerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                var matchedCtor = ctors.FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 1 && p[0].ParameterType.IsAssignableFrom(modelType);
                });

                if (matchedCtor != null)
                {
                    try
                    {
                        created = matchedCtor.Invoke(new object[] { modelObj });
                    }
                    catch (TargetInvocationException tie)
                    {
                        throw new InvalidOperationException($"Provider ctor threw for {providerType.FullName}", tie.InnerException ?? tie);
                    }
                }
                else
                {
                    // fallback to parameterless ctor
                    created = Activator.CreateInstance(providerType);
                }
                if (created is not IDescriptiveSummaryProvider instance)
                    throw new InvalidOperationException($"Provider {providerType.FullName} does not implement IDescriptiveSummaryProvider.");
                provider = instance;
            }

            // Ensure provider has the model available (useful if ctor didn't accept the model).
            // DescriptiveSummaryProvider exposes SetModel(IModel), so cast and call if available.
            if (provider is DescriptiveSummaryProvider dsp && modelObj is IModel im)
                dsp.SetModel(im);

            // ensure provider has the generator and any initialization is done in SetGenerator
            provider.SetGenerator(generator);
            return provider;
        }

        // Search loaded assemblies for a provider suitable for the supplied model type.
        // Returns the provider Type (class) or null if none found.
        private static Type? FindProviderTypeForModelType(Type modelType)
        {
            // scan for closed-generic implementations of IDescriptiveSummaryProvider<T>
            // Prefer exact T == modelType, otherwise try base types and interfaces (closest match)
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // build list of candidate provider types once per AppDomain scan
            List<Type> candidateProviders = new();
            lock (scanLock)
            {
                foreach (var asm in assemblies)
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rex) { types = rex.Types.Where(t => t != null).ToArray()!; }
                    foreach (var t in types)
                    {
                        if (t == null || t.IsAbstract || !t.IsClass)
                            continue;

                        var providerInterfaces = t.GetInterfaces()
                            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDescriptiveSummaryProvider<>));
                        if (providerInterfaces.Any())
                            candidateProviders.Add(t);
                    }
                }
            }

            // For each candidate provider, get the TModel type and choose the best match
            Type? bestProvider = null;
            int bestDistance = int.MaxValue;
            foreach (var provider in candidateProviders)
            {
                foreach (var iface in provider.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDescriptiveSummaryProvider<>)))
                {
                    var tModel = iface.GetGenericArguments()[0];
                    if (tModel.IsAssignableFrom(modelType))
                    {
                        // compute inheritance distance (lower is better)
                        int distance = InheritanceDistance(modelType, tModel);
                        if (distance >= 0 && distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestProvider = provider;
                        }
                    }
                }
            }

            return bestProvider;
        }

        // simple distance: number of steps up inheritance/interfaces from concreteType to targetType
        private static int InheritanceDistance(Type concreteType, Type targetType)
        {
            if (concreteType == targetType)
                return 0;
            int distance = 0;
            var t = concreteType;
            while (t != null)
            {
                if (t == targetType)
                    return distance;
                if (targetType.IsInterface)
                {
                    if (t.GetInterfaces().Contains(targetType))
                        return distance + 1;
                }
                t = t.BaseType;
                distance++;
            }
            return -1;
        }
    }
}
