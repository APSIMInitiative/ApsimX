

namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using PMF;
    using PMF.Functions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// 
    /// </summary>
    public class Links
    {
        private List<ModelWrapper> allModels;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootNode"></param>
        public void Resolve(IModel rootNode)
        {
            List<IModel> allModels = Apsim.ChildrenRecursively(rootNode);
            foreach (IModel modelNode in allModels)
                ResolveInternal(modelNode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootNode"></param>
        public void Resolve(ModelWrapper rootNode)
        {
            allModels = rootNode.ChildrenRecursively;
            foreach (ModelWrapper modelNode in allModels)
                ResolveInternal(modelNode);
        }

        /// <summary>
        /// Internal [link] resolution algorithm.
        /// </summary>
        /// <param name="obj"></param>
        private void ResolveInternal(object obj)
        {
            string errorMsg = string.Empty;
            List<object> modelsInScope = GetModelsInScope(obj);

            // Go looking for [Link]s
            foreach (FieldInfo field in ReflectionUtilities.GetAllFields(
                                                            GetModel(obj).GetType(),
                                                            BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var link = ReflectionUtilities.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;

                //link = ReflectionUtilities.GetAttribute(field, typeof(ScopedLinkByNameAttribute), false) as ScopedLinkByNameAttribute;

                if (link != null)
                {
                    object linkedObject = null;

                    Type type = field.FieldType;
                    if (type.IsArray)
                        type = type.GetElementType();

                    // Get a list of models that could possibly match.
                    List<object> possibleMatches;
                    bool useNameToMatch;
                    bool selectClosestMatch = true;
                    if (typeof(IFunction).IsAssignableFrom(field.FieldType) ||
                        typeof(IFunctionArray).IsAssignableFrom(field.FieldType) ||
                        typeof(Biomass).IsAssignableFrom(field.FieldType) ||
                        field.FieldType.Name == "Object")
                    {
                        possibleMatches = GetChildren(obj);
                        useNameToMatch = true;
                        selectClosestMatch = false;
                    }
                    else if (link is ScopedLinkByNameAttribute)
                    {
                        possibleMatches = modelsInScope;
                        useNameToMatch = false;
                    }
                    else
                    {
                        possibleMatches = modelsInScope;
                        useNameToMatch = false;
                    }

                    // Get a list of models that actually match.
                    List<object> allMatches = new List<object>();
                    foreach (object match in possibleMatches)
                    {
                        if (type.IsAssignableFrom(GetModel(match).GetType()))
                        {
                            if (useNameToMatch)
                            {
                                if (GetName(match).Equals(field.Name, StringComparison.InvariantCultureIgnoreCase))
                                    allMatches.Add(GetModel(match));
                            }
                            else
                                allMatches.Add(GetModel(match));
                        }
                    }

                    if (field.FieldType.IsArray)
                    {
                        Array array = Array.CreateInstance(type, allMatches.Count);
                        for (int i = 0; i < allMatches.Count; i++)
                            array.SetValue(allMatches[i], i);
                        linkedObject = array;

                    }
                    else if (allMatches.Count == 1)
                        linkedObject = allMatches[0];
                    else if (allMatches.Count == 0)
                    {
                        if (!link.IsOptional)
                            throw new Exception("Cannot find a match for link " + field.Name + " in model " + GetName(obj));
                    }
                    else if (allMatches.Count > 1)
                    {
                        if (selectClosestMatch)
                            linkedObject = allMatches[0];
                        else
                            throw new Exception(string.Format(": Found {0} matches for link {1} in model {2} !", allMatches.Count, field.Name, GetName(obj)));
                    }
                    field.SetValue(GetModel(obj), linkedObject);
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
                return (obj as ModelWrapper).Model;
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
                return (obj as ModelWrapper).Name;
        }

        /// <summary>
        /// Determine the type of an object and return all models that are in scope.
        /// </summary>
        /// <param name="obj">obj can be either a ModelWrapper or an IModel.</param>
        /// <returns>The models that are in scope of obj.</returns>
        private List<object> GetModelsInScope(object obj)
        {
            if (obj is IModel)
                return Apsim.FindAll(obj as IModel).Cast<object>().ToList();
            else
                return (obj as ModelWrapper).FindModelsInScope(allModels).Cast<object>().ToList();
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
                return (obj as ModelWrapper).Children.Cast<object>().ToList();
        }



    }
}
