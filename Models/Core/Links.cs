namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// 
    /// </summary>
    public class Links
    {
        /// <summary>A collection of services that can be linked to</summary>
        private List<object> services;

        /// <summary>Constructor</summary>
        /// <param name="linkableServices">A collection of services that can be linked to</param>
        public Links(IEnumerable<object> linkableServices = null)
        {
            if (linkableServices != null && linkableServices.Count() > 0)
                services = linkableServices.ToList();
            else
                services = new List<object>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="recurse">Recurse through all child models?</param>
        /// <param name="allLinks">Unresolve all links or just the non child links?</param>
        /// <param name="throwOnFail">Should all links be considered optional?</param>
        public void Resolve(IModel rootNode, bool allLinks, bool recurse = true, bool throwOnFail = false)
        {
            var scope = new ScopingRules();

            if (recurse)
            {
                List<IModel> allModels = new List<IModel>() { rootNode };
                allModels.AddRange(Apsim.ChildrenRecursively(rootNode));
                foreach (IModel modelNode in allModels)
                {
                    if (modelNode.Enabled)
                        ResolveInternal(modelNode, scope, throwOnFail);
                }
            }
            else
                ResolveInternal(rootNode, scope, throwOnFail);
        }

        /// <summary>
        /// Resolve links in an unknown object e.g. user interface presenter
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="throwOnFail">Should an exception be thrown if a link fails to be resolved?</param>
        public void Resolve(object obj, bool throwOnFail = true)
        {
            // Go looking for [Link]s
            foreach (IVariable field in GetAllDeclarations(obj, GetModel(obj).GetType(),
                                                           BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public,
                                                           allLinks:true))
            {
                LinkAttribute link = field.GetAttribute(typeof(LinkAttribute)) as LinkAttribute;

                if (link != null)
                {
                    // For now only try matching on a service
                    object match = services.Find(s => field.DataType.IsAssignableFrom(s.GetType()));
                    if (match != null)
                        field.Value = GetModel(match);
                    else if (!link.IsOptional && throwOnFail)
                        throw new Exception("Cannot find a match for link " + field.Name + " in model " + GetFullName(obj));
                }
            }
        }

        /// <summary>
        /// Set to null all link fields in the specified model.
        /// </summary>
        /// <param name="model">The model to look through for links</param>
        /// <param name="allLinks">Unresolve all links or just the non child links?</param>
        public void Unresolve(IModel model, bool allLinks)
        {
            List<IModel> allModels = new List<IModel>() { model };
            allModels.AddRange(Apsim.ChildrenRecursively(model));
            foreach (IModel modelNode in allModels)
            {
                // Go looking for private [Link]s
                foreach (IVariable declaration in GetAllDeclarations(modelNode,
                                                                     modelNode.GetType(),
                                                                     BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public,
                                                                     allLinks))
                {
                    LinkAttribute link = declaration.GetAttribute(typeof(LinkAttribute)) as LinkAttribute;
                    if (link != null)
                        declaration.Value = null;
                }
            }
        }

        /// <summary>
        /// Internal [link] resolution algorithm.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="scope">The scoping rules to use to resolve links.</param>
        /// <param name="throwOnFail">Should all links be considered optional?</param>
        private void ResolveInternal(object obj, ScopingRules scope, bool throwOnFail)
        {
            foreach (IVariable field in GetAllDeclarations(GetModel(obj),
                                                     GetModel(obj).GetType(),
                                                     BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public,
                                                     allLinks: true))
            {
                LinkAttribute link = field.GetAttribute(typeof(LinkAttribute)) as LinkAttribute;
                if (link != null)
                {
                    Type fieldType = field.DataType;
                    if (fieldType.IsArray)
                        fieldType = fieldType.GetElementType();
                    else if (field.DataType.Name.StartsWith("List") && field.DataType.GenericTypeArguments.Length == 1)
                        fieldType = field.DataType.GenericTypeArguments[0];
                    List<object> matches;
                    matches = services.FindAll(s => fieldType.IsAssignableFrom(s.GetType()));
                    if (matches.Count == 0 && obj is IModel)
                    {
                        Simulation parentSimulation = Apsim.Parent(obj as IModel, typeof(Simulation)) as Simulation;
                        if (fieldType.IsAssignableFrom(typeof(ILocator)) && parentSimulation != null)
                            matches.Add(new Locator(obj as IModel));
                        else if (fieldType.IsAssignableFrom(typeof(IEvent)) && parentSimulation != null)
                            matches.Add(new Events(obj as IModel));
                    }
                    if (matches.Count == 0)
                    {
                        if (link.Type == LinkType.Ancestor)
                        {
                            matches = new List<object>();
                            matches.Add(GetParent(obj, fieldType));
                        }
                        else if (link.Type == LinkType.Path)
                        {
                            var locater = new Locater();
                            object match = locater.Get(link.Path, obj as Model);
                            if (match != null)
                                matches.Add(match);
                        }
                        else if (link.Type == LinkType.Scoped)
                            matches = scope.FindAll(obj as IModel).Cast<object>().ToList();
                        else
                            matches = GetChildren(obj);
                    }
                    matches.RemoveAll(match => !fieldType.IsAssignableFrom(GetModel(match).GetType()));
                    if (link.ByName)
                        matches.RemoveAll(match => !StringUtilities.StringsAreEqual(GetName(match), field.Name));
                    if (field.DataType.IsArray)
                    {
                        Array array = Array.CreateInstance(fieldType, matches.Count);
                        for (int i = 0; i < matches.Count; i++)
                            array.SetValue(GetModel(matches[i]), i);
                        field.Value = array;
                    }
                    else if (field.DataType.Name.StartsWith("List") && field.DataType.GenericTypeArguments.Length == 1)
                    {
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(fieldType);
                        IList array = Activator.CreateInstance(constructedListType) as IList;
                        for (int i = 0; i < matches.Count; i++)
                            array.Add(GetModel(matches[i]));
                        field.Value = array;
                    }
                    else if (matches.Count == 0 && !throwOnFail)
                    {
                        if (!link.IsOptional)
                            throw new Exception("Cannot find a match for link " + field.Name + " in model " + GetFullName(obj));
                    }
                    else if (matches.Count >= 2 && link.Type != LinkType.Scoped)
                        throw new Exception(string.Format(": Found {0} matches for link {1} in model {2} !", matches.Count, field.Name, GetFullName(obj)));
                    else
                        field.Value = GetModel(matches[0]);
                }
            }
        }

        /// <summary>
        /// Determine the type of an object and return its model.
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <returns>The model</returns>
        private object GetModel(object obj)
        {
            if (obj is IModel)
                return obj;
            else
                return obj;
        }

        /// <summary>
        /// Determine the type of an object and return its name.
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <returns>The name</returns>
        private string GetName(object obj)
        {
            if (obj is IModel)
                return (obj as IModel).Name;
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Determine the type of an object and return its parent of the specified type.
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <param name="type">The type of parent to find.</param>
        /// <returns>The matching parent</returns>
        private object GetParent(object obj, Type type)
        {
            if (obj is IModel)
                return Apsim.Parent(obj as IModel, type);
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Determine the type of an object and return its name.
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <returns>The name</returns>
        private string GetFullName(object obj)
        {
            if (obj is IModel)
                return Apsim.FullPath(obj as IModel);
            else
                return obj.GetType().FullName;
        }

        /// <summary>
        /// Determine the type of an object and return all direct child models
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <returns>The child models.</returns>
        private List<object> GetChildren(object obj)
        {
            if (obj is IModel)
                return (obj as IModel).Children.Cast<object>().ToList();
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Return all fields. The normal .NET reflection doesn't return private fields in base classes.
        /// This function does.
        /// </summary>
        public static List<IVariable> GetAllDeclarations(object obj, Type type, BindingFlags flags, bool allLinks)
        {
            if (type == typeof(Object) || type == typeof(Model)) return new List<IVariable>();

            var list = GetAllDeclarations(obj, type.BaseType, flags, allLinks);
            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            foreach (FieldInfo field in type.GetFields(flags | BindingFlags.DeclaredOnly))
                foreach (Attribute a in field.GetCustomAttributes())
                {
                    LinkAttribute link = a as LinkAttribute;
                    if (link != null)
                    {
                        if (allLinks || !link.GetType().Name.StartsWith("Child"))
                            list.Add(new VariableField(obj, field));
                    }
                }
            foreach (PropertyInfo property in type.GetProperties(flags | BindingFlags.DeclaredOnly))
                foreach (Attribute a in property.GetCustomAttributes())
                {
                    LinkAttribute link = a as LinkAttribute;
                    if (link != null)
                    {
                        if (allLinks || !link.GetType().Name.StartsWith("Child"))
                            list.Add(new VariableProperty(obj, property));
                    }
                }

            return list;
        }



    }
}
