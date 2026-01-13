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

    /// <summary>This is a Organ class with Leaf and Stem. It can be extended to other organs. Currently calculates DM,N and NConc.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    
    public class GenericOrgan: Model,IStructureDependency,IBiomass
    {   
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

         /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true; 
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

        /// <summary>
        /// StructuralWt of the Organ Live+ Dead
        /// </summary>
        [JsonIgnore]
        [Units("g/m^2")]

        public double StructuralWt
        {
            get
            {   
                if(Name=="Leaf")
                    return GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0;
                if(Name=="Stem")
                    return GetDM(GrazType.TOTAL, GrazType.ptSTEM)/10.0;
              
                return 0;
            }
        }


         /// <summary>
        /// StorageWt in Organ Live+ Dead
        /// </summary>
        public double StorageWt
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// StorageN of Organ Live+ Dead
        /// </summary>
        public double StorageN
        {
            get
            {
                return 0;
            }
        }

         /// <summary>
        /// Nitrogen content of Organ Live+ Dead
        /// </summary>
        public double StructuralN
        {
            get
            {   
                if(Name=="Leaf")
                    return (GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0)* (GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.N));
                if(Name=="Stem")
                    return (GetDM(GrazType.TOTAL, GrazType.ptSTEM)/10.0)* (GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.N));
                return 0;
            }
        }

        /// <summary>
        /// DM of Organ Live+ Dead
        /// </summary>
        public double Wt
        {
            get
            {
                return StructuralWt+StorageWt;
            }
        }

        

        /// <summary>
        /// N amount of Organ Live+ Dead
        /// </summary>
        public double N
        {
            get
            {
                return StructuralN;
            }
        }

        /// <summary>
        /// N concentration of Organ Live+ Dead
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
