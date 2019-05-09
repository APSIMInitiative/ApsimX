namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
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
        /// <summary>
        /// Resolves links in the specified object. To resolve the links, this method will
        /// try and locate objects under the rootModel.
        /// </summary>
        /// <param name="objectToResolveLinksIn">The object to resolve links in. If the object is a IModel, child models will have their links resolved as well.</param>
        /// <param name="rootModel">The root model.</param>
        public static void Resolve(object objectToResolveLinksIn, IModel rootModel)
        {
            List<object> services = new List<object>();
            IDataStore storage = Apsim.Find(rootModel, typeof(IDataStore)) as IDataStore;
            if (storage != null)
                services.Add(storage);

            List<object> allObjectsToResolveLinksIn = Events.ExpandCompleteListOfObjects(new object[] { objectToResolveLinksIn });

            foreach (var obj in allObjectsToResolveLinksIn)
                ResolveInternal(obj, services);
        }

        /// <summary>
        /// Set to null all link fields in the specified model.
        /// </summary>
        /// <param name="model">The model to look through for links</param>
        public static void Unresolve(IModel model)
        {
            List<IModel> allModels = new List<IModel>() { model };
            allModels.AddRange(Apsim.ChildrenRecursively(model));
            foreach (IModel modelNode in allModels)
            {
                // Go looking for private [Link]s
                foreach (IVariable declaration in GetAllDeclarations(modelNode,
                                                                     modelNode.GetType(),
                                                                     BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public,
                                                                     allLinks:true))
                {
                    LinkAttribute link = declaration.GetAttribute(typeof(LinkAttribute)) as LinkAttribute;
                    if (link != null)
                        declaration.Value = null;
                }
            }
        }

        /// <summary>
        /// Resolves links in the specified object. To resolve the links, this method will
        /// try and locate objects in the simulation and it will look for objects in the 
        /// specified collection.
        /// </summary>
        /// <param name="obj">Object to resolve links in.</param>
        /// <param name="services">A collection of objects that can be used to resolve links.</param>
        private static void ResolveInternal(object obj, IEnumerable<object> services = null)
        {
            // Go looking for [Link]s
            foreach (IVariable field in GetAllDeclarations(obj,
                                                     obj.GetType(),
                                                     BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public,
                                                     allLinks: true))
            {
                LinkAttribute link = field.GetAttribute(typeof(LinkAttribute)) as LinkAttribute;

                if (link != null)
                {
                    // Get the field type or the array element if it is an array field.
                    Type fieldType = field.DataType;
                    if (fieldType.IsArray)
                        fieldType = fieldType.GetElementType();
                    else if (field.DataType.Name.StartsWith("List") && field.DataType.GenericTypeArguments.Length == 1)
                        fieldType = field.DataType.GenericTypeArguments[0];

                    // Try and get a match from our services first.
                    List<object> matches = new List<object>();
                    if (services != null)
                        matches = services.ToList().FindAll(s => fieldType.IsAssignableFrom(s.GetType()));

                    // If no match on services then try other options.
                    if (matches.Count == 0 && obj is IModel)
                    {
                        Simulation parentSimulation = Apsim.Parent(obj as IModel, typeof(Simulation)) as Simulation;
                        if (fieldType.IsAssignableFrom(typeof(ILocator)) && parentSimulation != null)
                            matches.Add(new Locator(obj as IModel));
                        else if (fieldType.IsAssignableFrom(typeof(IEvent)) && parentSimulation != null)
                            matches.Add(new Events(obj as IModel));
                    }

                    // If no match on services then try other options.
                    if (obj is IModel && matches.Count == 0)
                    {
                        // Get a list of models that could possibly match.
                        if (link is ParentLinkAttribute)
                        {
                            matches = new List<object>();
                            matches.Add(Apsim.Parent(obj as IModel, fieldType));
                        }
                        else if (link is LinkByPathAttribute)
                        {
                            object match = Apsim.Get(obj as IModel, (link as LinkByPathAttribute).Path);
                            if (match != null)
                                matches.Add(match);
                        }
                        else if (link.IsScoped(field))
                            matches = Apsim.FindAll(obj as IModel).Cast<object>().ToList();
                        else
                            matches = (obj as IModel).Children.Cast<object>().ToList();
                    }

                    // Filter possible matches to those of the correct type.
                    matches.RemoveAll(match => !fieldType.IsAssignableFrom(match.GetType()));

                    // If we should use name to match then filter matches to those with a matching name.
                    if (link.UseNameToMatch(field))
                        matches.RemoveAll(match => !StringUtilities.StringsAreEqual((match as IModel).Name, field.Name));

                    if (field.DataType.IsArray)
                    {
                        Array array = Array.CreateInstance(fieldType, matches.Count);
                        for (int i = 0; i < matches.Count; i++)
                            array.SetValue(matches[i], i);
                        field.Value = array;
                        
                    }
                    else if (field.DataType.Name.StartsWith("List") && field.DataType.GenericTypeArguments.Length == 1)
                    {
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(fieldType);
                        IList array = Activator.CreateInstance(constructedListType) as IList;
                        for (int i = 0; i < matches.Count; i++)
                            array.Add(matches[i]);
                        field.Value = array;
                    }
                    else if (matches.Count == 0)
                    {
                        if (!link.IsOptional)
                            throw new Exception("Cannot find a match for link " + field.Name + " in model " + Apsim.FullPath(obj as IModel));
                    }
                    else if (matches.Count >= 2 && !link.IsScoped(field))
                        throw new Exception(string.Format(": Found {0} matches for link {1} in model {2} !", matches.Count, field.Name, Apsim.FullPath(obj as IModel)));
                    else
                        field.Value = matches[0];
                }
            }
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
