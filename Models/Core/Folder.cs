using System;
using APSIM.Core;

namespace Models.Core
{
    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [Serializable]
    [ValidParent(DropAnywhere = true)]
    public class Folder : Model, IScopedModel
    {
        /// <summary>Show in the documentation</summary>
        /// <remarks>
        /// Whether this folder should show up in documentation or not.
        /// </remarks>
        public bool ShowInDocs { get; set; }

        /// <summary>Returns true if this folder is a child of the root Simulations model, and has the name Replacements.</summary>
        public static bool IsModelReplacementsFolder(IModel model)
        {
            if (model == null)
                return false;

            if (model.Name == "Replacements")
            {
                if (model is Folder)
                {
                    if (model.Parent is Simulations)
                        return true;
                    else
                        throw new ArgumentException($"Replacements can only be added to the top Simulations Node");
                }
                else
                {
                    throw new ArgumentException($"Replacements must be a Folder");
                }
            }
            return false;
        }

        /// <summary>Returns true if this folder is a child of the root Simulations model, and has the name Replacements.</summary>
        public static Folder FindReplacementsFolder(IModel model)
        {
            //If this model is the root model, use it
            IModel root = model;
            if (model.Parent != null)
                //Otherwise look for the simulations model
                root = model.FindAncestor<Simulations>();

            Folder replacements = root?.FindChild<Folder>("Replacements");
            if (IsModelReplacementsFolder(replacements))
                return replacements;
            else
                return null;
        }

        /// <summary>Returns true if this folder is a child of the root Simulations model, and has the name Replacements.</summary>
        public static Folder IsUnderReplacementsFolder(IModel model)
        {
            Folder replacements = model.FindAncestor<Folder>("Replacements");
            if (IsModelReplacementsFolder(replacements))
                return replacements;
            else
                return null;
        }
    }
}
