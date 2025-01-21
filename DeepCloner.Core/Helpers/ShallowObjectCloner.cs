﻿using System.Linq.Expressions;

namespace DeepCloner.Core.Helpers;

/// <summary>
/// Internal class but due implementation restriction should be public
/// </summary>
public abstract class ShallowObjectCloner
{
    /// <summary>
    /// Abstract method for real object cloning
    /// </summary>
    protected abstract object DoCloneObject(object obj);

    private static readonly ShallowObjectCloner _instance;

    /// <summary>
    /// Performs real shallow object clone
    /// </summary>
    public static object CloneObject(object obj)
    {
        return _instance.DoCloneObject(obj);
    }

    static ShallowObjectCloner()
    {
        _instance = new ShallowSafeObjectCloner();
    }

    private class ShallowSafeObjectCloner : ShallowObjectCloner
    {
        private static readonly Func<object, object> _cloneFunc;

        static ShallowSafeObjectCloner()
        {
            var methodInfo = typeof(object).GetPrivateMethod(nameof(MemberwiseClone));
            var p = Expression.Parameter(typeof(object));
            var mce = Expression.Call(p, methodInfo);
            _cloneFunc = Expression.Lambda<Func<object, object>>(mce, p).Compile();
        }

        protected override object DoCloneObject(object obj)
        {
            return _cloneFunc(obj);
        }
    }
}