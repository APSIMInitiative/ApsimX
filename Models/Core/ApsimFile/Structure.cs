namespace Models.Core.ApsimFile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A collection of methods for manipulating the structure of an .apsimx file.
    /// </summary>
    public static class Structure
    {
        /// <summary>
        /// Adds a model as a child to a parent model. Will throw if not allowed.
        /// </summary>
        /// <param name="modelToAdd">The model to add.</param>
        /// <param name="parent">The parent model to add it to.</param>
        public static void Add(IModel modelToAdd, IModel parent)
        {
            if (parent.ReadOnly)
                throw new Exception(string.Format("Unable to modify {0} - it is read-only.", parent.Name));

            modelToAdd.Parent = parent;
            Apsim.ParentAllChildren(modelToAdd);
            parent.Children.Add(modelToAdd as Model);

            // Ensure the model name is valid.
            EnsureNameIsUnique(modelToAdd);

            // Call OnCreated
            modelToAdd.OnCreated();
            Apsim.ChildrenRecursively(modelToAdd).ForEach(m => m.OnCreated());
        }

        /// <summary>Adds a new model (as specified by the string argument) to the specified parent.</summary>
        /// <param name="parent">The parent to add the model to</param>
        /// <param name="st">The string representing the new model</param>
        /// <returns>The newly created model.</returns>
        public static IModel Add(string st, IModel parent)
        {
            List<Exception> creationExceptions;
            IModel modelToAdd = FileFormat.ReadFromString<IModel>(st, out creationExceptions);

            // Correctly parent all models.
            Add(modelToAdd, parent);

            // Ensure the model name is valid.
            EnsureNameIsUnique(modelToAdd);

            // Call OnCreated
            Apsim.ChildrenRecursively(modelToAdd).ForEach(m => m.OnCreated());

            return modelToAdd;
        }

        /// <summary>Renames a new model.</summary>
        /// <param name="model">The model to rename.</param>
        /// <param name="newName">The new name for the model.</param>
        /// <returns>The newly created model.</returns>
        public static void Rename(IModel model, string newName)
        {
            model.Name = newName;
            EnsureNameIsUnique(model);
        }

        /// <summary>Move a model from one parent to another.</summary>
        /// <param name="model">The model to move.</param>
        /// <param name="newParent">The new parente for the model.</param>
        public static void Move(IModel model, IModel newParent)
        {
            // Remove old model.
            if (model.Parent.Children.Remove(model as Model))
            {
                newParent.Children.Add(model as Model);
                model.Parent = newParent;
                EnsureNameIsUnique(model);
            }
            else
                throw new Exception("Cannot move model " + model.Name);
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        /// <param name="modelToCheck">The model to check the name of</param>
        private static void EnsureNameIsUnique(IModel modelToCheck)
        {
            string originalName = modelToCheck.Name;
            string newName = originalName;
            int counter = 0;
            List<IModel> siblings = Apsim.Siblings(modelToCheck);
            IModel child = siblings.Find(m => m.Name == newName);
            while (child != null && child != modelToCheck && counter < 10000)
            {
                counter++;
                newName = originalName + counter.ToString();
                child = siblings.Find(m => m.Name == newName);
            }

            if (counter == 1000)
            {
                throw new Exception("Cannot create a unique name for model: " + originalName);
            }

            modelToCheck.Name = newName;
        }
    }
}
