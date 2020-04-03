namespace Models.Core.Run
{
    using Models.Storage;
    using System;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    public class ModelReplacement : IReplacement
    {
        /// <summary>
        /// Model path to use to find the model to replace. If null, then
        /// multiple replacements are made using the model name for matching.
        /// </summary>
        private string path;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private IModel replacement;

        /// <summary>Constructor</summary>
        /// <param name="pathOfModel">Model path to use to find the model to replace. If null, then multiple replacements are made using the model name for matching.</param>
        /// <param name="modelReplacement">The value to Model path to use to find the model to replace.</param>
        public ModelReplacement(string pathOfModel, IModel modelReplacement)
        {
            path = pathOfModel;
            replacement = modelReplacement;
        }

        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        public void Replace(IModel simulation)
        {
            if (path == null)
            {
                // Temporarily remove DataStore because we don't want to do any
                // replacements under DataStore.
                var dataStore = simulation.Children.Find(model => model is DataStore);
                if (dataStore != null)
                    simulation.Children.Remove(dataStore);

                // Do replacements.
                foreach (IModel match in Apsim.ChildrenRecursively(simulation))
                    if (match.Name.Equals(replacement.Name, StringComparison.InvariantCultureIgnoreCase))
                        ReplaceModel(match);

                // Reinstate DataStore.
                if (dataStore != null)
                    simulation.Children.Add(dataStore);
            }
            else
            {
                IModel match = Apsim.Get(simulation, path) as IModel;
                if (match == null)
                    throw new Exception("Cannot find a model on path: " + path);
                ReplaceModel(match);
            }
        }

        /// <summary>Perform the actual replacement.</summary>
        private void ReplaceModel(IModel match)
        {
            IModel newModel = Apsim.Clone(replacement);
            int index = match.Parent.Children.IndexOf(match as Model);
            match.Parent.Children.Insert(index, newModel as Model);
            newModel.Parent = match.Parent;
            newModel.Name = match.Name;
            newModel.Enabled = match.Enabled;
            match.Parent.Children.Remove(match as Model);

            // Don't call newModel.Parent.OnCreated(), because if we're replacing
            // a child of a resource model, the resource model's OnCreated event
            // will make it reread the resource string and replace this child with
            // the 'official' child from the resource.
            foreach (var model in Apsim.ChildrenRecursively(newModel.Parent))
                model.OnCreated();
        }
    }

}
