using System;
using System.Collections.Generic;
using Models.Core;
using Models.Soils.Nutrients;

namespace Models.Soils.NutrientPatching
{
    /// <summary>A nutrient pool (patch verison).</summary>
    public class OrganicPoolPatch : Model, IOrganicPool
    {
        private NutrientPatchManager patchManager;

        /// <summary>Amount of carbon (kg/ha)</summary>
        IReadOnlyList<double> IOrganicPool.C => GetPoolFromPatchManager().C; 

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        IReadOnlyList<double> IOrganicPool.N => GetPoolFromPatchManager().N; 

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        IReadOnlyList<double> IOrganicPool.P => GetPoolFromPatchManager().P; 

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="patchManager">The nutrient patch manager</param>
        public OrganicPoolPatch(NutrientPatchManager patchManager)
        {
            this.patchManager = patchManager;
        }

        /// <summary>
        /// Add an amount of c, n, p (kg/ha) into a layer.
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="c">Amount of carbon (kg/ha)</param>
        /// <param name="n">Amount of nitrogen (kg/ha)</param>
        /// <param name="p">Amount of phosphorus (kg/ha)</param>
        void IOrganicPool.Add(int index, double c, double n, double p)
        {
            if (Name == "Microbial")
                patchManager.AddToMicrobial(index, c, n, p);
            else if (Name == "Humic")
                patchManager.AddToHumic(index, c, n, p);
            else
                throw new Exception($"Unknown pool name: {Name}");
        }

        /// <summary>
        /// Get an organic pool from patch manager that matches our name.
        /// </summary>
        private IOrganicPool GetPoolFromPatchManager()
        {
            if (Name == "Microbial")
                return patchManager.Microbial;
            else if (Name == "Humic")
                return patchManager.Humic;
            else
                throw new Exception($"Unknown pool name: {Name}");
        }        
    }
}