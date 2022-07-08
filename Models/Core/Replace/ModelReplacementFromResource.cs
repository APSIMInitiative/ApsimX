namespace Models.Core.Replace
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using System;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    public class ModelReplacementFromResource
    {
        /// <summary>Replace all models with a ResourceName with a new model loaded from a resource.</summary>
        /// <param name="modelToReplace">The parent model to search under for models to replace.</param>
        public static void Replace(IModel modelToReplace)
        {
            //foreach (var match in modelToReplace.FindAllDescendants()
            //                                    .Where(m => !string.IsNullOrEmpty(m.ResourceName))
            //                                    .ToArray()) // ToArray is necessary to stop 'Collection was modified' exception
            //{
            //    string fullResourceName = $"Models.Resources.{match.ResourceName}.json";
            //    var contents = ReflectionUtilities.GetResourceAsString(fullResourceName);
            //    if (contents == null)
            //        throw new Exception($"Cannot find a resource named {match.ResourceName}");

            //    IModel modelFromResource = FileFormat.ReadFromString<IModel>(contents, e => throw e, false);
            //    modelFromResource =  modelFromResource.Children.First();
            //    modelFromResource.Enabled = modelToReplace.Enabled;

            //    // Replace existing children that match (name and type) the resource model children.
            //    modelToReplace.Children.RemoveAll(c => modelFromResource.Children.Contains(c, new ModelComparer()));
            //    modelToReplace.Children.InsertRange(0, modelFromResource.Children);

            //    CopyPropertiesFrom(modelFromResource);

            //    // Make the model and all children readonly if it's not under replacements.
            //    bool isHidden = modelToReplace.FindAncestor<Replacements>() == null;
            //    foreach (Model child in modelToReplace.FindAllDescendants()
            //                                          .Append(modelToReplace))
            //    {
            //        child.IsHidden = isHidden;
            //        child.ReadOnly = isHidden;
            //    }

            //    modelToReplace.ParentAllDescendants();
            //}
        }

    }
}
