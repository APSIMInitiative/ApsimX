using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Grazplan;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Soils.Nutrients;
using Models.Surface;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using static Models.GrazPlan.GrazType;
using static Models.GrazPlan.PastureUtil;
using APSIM.Core;
using Models.GrazPlan;
using Models.PMF.Interfaces;
using Models.PMF;

namespace Models.GrazPlan.Organs
{

    /// <summary>This is a composite biomass classteting.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    
    public class Leaf: Model,IStructureDependency,IBiomass
    {   
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }
         /// <summary>
        /// TPasturePopulation
        /// </summary>
        public TPasturePopulation PastureModel;
        private double GetDM(int comp, int part)
        {   
    
            string sUnit = PastureModel.MassUnit;
             PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GetHerbageMass(comp, part, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;
            return result;
        }
        
        /// <summary>
        /// Get average nutrient content of a plant (g/g) (CONCENTRATION NOT AMT)
        /// </summary>
        /// <param name="comp">Herbage</param>
        /// <param name="part">Plant part</param>
        /// <param name="elem">Nutrient element</param>
        /// <returns></returns>
        private double GetPlantNutr(int comp, int part, TPlantElement elem)
        {
            return PastureModel.GetHerbageConc(comp, part, GrazType.TOTAL, elem);
        }



        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true; 
        /// <summary>
        /// Leaf DM Live+Dead
        /// </summary>
        [JsonIgnore]
        [Units("g/m^2")]

        public double   StructuralWt
        {
            get
            {
                return GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0;
            }
        }
        
        /// <summary>
        /// Nitrogen content in Leaf live+dead
        /// </summary>
        public double StructuralN
        {
            get
            {
                return (GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0)* (GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.N));
            }
        }

        /// <summary>
        /// StorageWt in leaf dead+live
        /// </summary>
        public double StorageWt
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// StorageN in leaf dead+live
        /// </summary>
        public double StorageN
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Leaf Weight
        /// </summary>
        public double Wt
        {
            get
            {
                return StructuralWt+StorageWt;
            }
        }

        /// <summary>
        /// N amt in leaf
        /// </summary>
        public double N
        {
            get
            {
                return StructuralN;
            }
        }

        /// <summary>
        /// NConc in leaves
        /// </summary>
        public double NConc
        {
            get
            {   
                
                return N/Wt;

            }
        }



    }
}