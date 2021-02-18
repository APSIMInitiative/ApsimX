namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models.Core;
    using Models.LifeCycle;
    using Models.PMF;

    /// <summary>
    /// Helper functions for the property presenter. Most involve
    /// fetching valid values for the various DisplayType options.
    /// </summary>
    internal static class PropertyPresenterHelpers
    {
        /// <summary>Get a list of cultivars for crop.</summary>
        /// <param name="crop">The crop.</param>
        /// <returns>A list of cultivars.</returns>
        public static string[] GetCultivarNames(IPlant crop)
        {
            Simulations simulations = (crop as IModel).FindAncestor<Simulations>();
            Replacements replacements = simulations.FindChild<Replacements>();

            if (replacements == null)
                return crop.CultivarNames;

            IPlant replacementCrop = replacements.FindChild((crop as IModel).Name) as IPlant;
            if (replacementCrop != null)
                return replacementCrop.CultivarNames;

            // Check for cultivar folders under replacements.
            List<string> cultivarNames = crop.CultivarNames.ToList();
            foreach (CultivarFolder cultivarFolder in (crop as IModel).FindAllChildren<CultivarFolder>())
            {
                IModel replacementFolder = replacements.FindChild(cultivarFolder.Name);
                if (replacementFolder != null)
                {
                    // If we find a matching cultivar folder under replacements, remove
                    // all cultivar names added by this folder in the official plant
                    // model, and add the cultivar names added by the matching cultivar
                    // folder under replacements.
                    foreach (IModel cultivar in cultivarFolder.FindAllDescendants<Cultivar>())
                    {
                        cultivarNames.Remove(cultivar.Name);

                        // If the cultivar has memo children, then the memo text will
                        // be appended to the cultivar name after a vertical bar |.
                        // Technically, there could be a cultivar x and x|y, but the UI
                        // will prevent users from doing this, so the user would really
                        // just be digging their own hole at this point.
                        cultivarNames.RemoveAll(c => c.StartsWith(cultivar.Name + "|"));
                    }

                    foreach (Alias alias in cultivarFolder.FindAllDescendants<Alias>())
                        cultivarNames.RemoveAll(c => c.StartsWith(alias.Name + "|"));

                    foreach (IModel cultivar in replacementFolder.FindAllDescendants<Cultivar>())
                        cultivarNames.Add(cultivar.Name);
                }
            }
            return cultivarNames.ToArray();
        }

        /// <summary>Get a list of life cycles in the zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <returns>A list of life cycles.</returns>
        public static string[] GetLifeCycleNames(Zone zone)
        {
            List<LifeCycle> LifeCycles = zone.FindAllInScope<LifeCycle>().ToList();
            if (LifeCycles.Count > 0)
            {
                string[] Namelist = new string[LifeCycles.Count];
                int i = 0;
                foreach (IModel LC in LifeCycles)
                {
                    Namelist[i] = LC.Name;
                    i++;
                }
                return Namelist;
            }
            return new string[0];
        }
    
        /// <summary>Get a list of phases for lifecycle.</summary>
        /// <param name="lifeCycle">The lifecycle.</param>
        /// <returns>A list of phases.</returns>
        public static string[] GetPhaseNames(LifeCycle lifeCycle)
        {
            if (lifeCycle.LifeCyclePhaseNames.Length == 0)
            {
                Simulations simulations = (lifeCycle as IModel).FindAncestor<Simulations>();
                Replacements replacements = simulations.FindChild<Replacements>();
                if (replacements != null)
                {
                    LifeCycle replacementLifeCycle = replacements.FindChild((lifeCycle as IModel).Name) as LifeCycle;
                    if (replacementLifeCycle != null)
                    {
                        return replacementLifeCycle.LifeCyclePhaseNames;
                    }
                }
            }
            else
            {
                return lifeCycle.LifeCyclePhaseNames;
            }

            return new string[0];
        }
    }
}