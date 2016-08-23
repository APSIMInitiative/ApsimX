

namespace Models.Core
{
    using APSIM.Shared.Utilities;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootNode"></param>
        public static void Resolve(ModelWrapper rootNode)
        {
            List<ModelWrapper> allModels = rootNode.ChildrenRecursively;

            foreach (ModelWrapper modelNode in allModels)
            {
                List<ModelWrapper> modelsInScope = modelNode.FindModelsInScope(allModels);

                string errorMsg = string.Empty;

                // Go looking for [Link]s
                foreach (FieldInfo field in ReflectionUtilities.GetAllFields(
                                                                modelNode.Model.GetType(),
                                                                BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    var link = ReflectionUtilities.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                    if (link != null)
                    {
                        object linkedObject = null;
                        if (field.FieldType == typeof(ModelWrapper))
                            linkedObject = modelNode;
                        else if (field.FieldType == typeof(Property))
                        {
                            PropertyInfo associatedProperty = modelNode.Model.GetType().GetProperty(link.AssociatedProperty);
                            if (associatedProperty == null)
                                throw new Exception("Cannot find the associated property: " + link.AssociatedProperty);
                            linkedObject = rootNode.GetProperty(associatedProperty.GetValue(modelNode.Model, null).ToString());
                            if (linkedObject == null)
                                throw new Exception("Cannot find the value of associated property: " + link.AssociatedProperty);
                        }

                        else
                        {
                            Type type = field.FieldType;
                            if (type.IsArray)
                                type = type.GetElementType();

                            List<ModelWrapper> allMatches;
                            if (type.Name == "IFunction")
                                allMatches = modelsInScope.FindAll(m => type.IsAssignableFrom(m.Model.GetType()) && m.Name.Equals(field.Name, StringComparison.InvariantCultureIgnoreCase));
                            else
                                allMatches = modelsInScope.FindAll(m => type.IsAssignableFrom(m.Model.GetType()));

                            if (field.FieldType.IsArray)
                            {
                                Array array = Array.CreateInstance(type, allMatches.Count);
                                for (int i = 0; i < allMatches.Count; i++)
                                    array.SetValue(allMatches[i].Model, i);
                                linkedObject = array;

                            }
                            else if (allMatches.Count == 1)
                                linkedObject = allMatches[0].Model;
                            else
                            {
                                // more that one match so use name to match
                                foreach (ModelWrapper matchingModel in allMatches)
                                {
                                    if (matchingModel.Name == field.Name)
                                    {
                                        linkedObject = matchingModel.Model;
                                        break;
                                    }
                                }

                                // If the link isn't optional then choose the closest match.
                                if (linkedObject == null && !link.IsOptional && allMatches.Count > 1)
                                {
                                    // Return the first (closest) match.
                                    linkedObject = allMatches[0].Model;
                                }

                                if ((linkedObject == null) && (!link.IsOptional))
                                    errorMsg = string.Format(": Found {0} matches for {1} {2} !", allMatches.Count, field.FieldType.FullName, field.Name);
                            }
                        }

                        if (linkedObject != null)
                            field.SetValue(modelNode.Model, linkedObject);
                        else if (!link.IsOptional)
                            throw new Exception("Cannot resolve [Link] '" + field.ToString() + errorMsg);
                    }
                }
            }
        }


    }
}
