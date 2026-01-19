using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Models.GrazPlan;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using static Models.GrazPlan.GrazType;

namespace Models.Grazplan.Biomass
{
    /// <summary>This is a composite biomass class, representing the sum of 1 or more biomass objects from one or more organs.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    public class CompositeBiomass : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        private List<IBiomass> organs = null;

        /// <summary>List of organs to aggregate.</summary>
        [Description("List of organs to aggregate.")]
        public string[] OrganNames { get; set; }

        // /// <summary>Include live material?</summary>
        // [Description("Include live material?")]
        // public bool IncludeLive { get; set; }

        // /// <summary>Include dead material?</summary>
        // [Description("Include dead material?")]
        // public bool IncludeDead { get; set; }

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
        /// Get average nutrient content of a plant (g/g)
        /// </summary>
        /// <param name="comp">Herbage</param>
        /// <param name="part">Plant part</param>
        /// <param name="elem">Nutrient element</param>
        /// <returns></returns>
        private double GetPlantNutr(int comp, int part, TPlantElement elem)
        {
            return PastureModel.GetHerbageConc(comp, part, GrazType.TOTAL, elem);
        }
        
        // /// <summary>
        // /// Get average nutrient content of a plant (g/g) (CONCENTRATION NOT AMT)
        // /// </summary>
        // /// <param name="comp">Herbage</param>
        // /// <param name="part">Plant part</param>
        // /// <param name="elem">Nutrient element</param>
        // /// <returns></returns>
        // private double GetPlantNutr(int comp, int part, TPlantElement elem)
        // {
        //     return PastureModel.GetHerbageConc(comp, part, GrazType.TOTAL, elem);
        // }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            organs = new List<IBiomass>();
            var parentPlant = Structure.FindParent<Pasture>(recurse: true);
            if (parentPlant == null)
                throw new Exception("CompositeBiomass can only be dropped on a plant.");
            foreach (var organName in OrganNames)
            {
                var organ = parentPlant.Children.FirstOrDefault(o => o.Name == organName);
                if (organ == null && !(organ is IBiomass))
                    throw new Exception($"In {Name}, cannot find a plant organ called {organName}");
                organs.Add(organ as IBiomass);
                
            }
        }

        /// <summary>Composite Biomass mass of the organs.</summary>
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                
                double wt = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        wt+=organ.Wt;
                    }
                if (PastureModel != null)
                {
                    if (Name == "AboveGroundLive")
                    {   
                    
                        wt=GetDM(GrazType.sgGREEN, GrazType.TOTAL)/10.0;
                    
                    }
                   
                    if(Name=="AboveGroundDead")
                        wt= GetDM(GrazType.stDEAD, GrazType.TOTAL)/10.0;
                }
                
         

                return wt;
            }
        }
 
        /// <summary>
        /// Composite N content of the organs
        /// </summary>
        [Units("g/m^2")]
        public double N
        {
            get
            {
                
                double n = 0;
                
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        n+=organ.N;
                    }
                 if (PastureModel != null)
                {
                    if (Name == "AboveGroundLive")
                    {   
                         n= GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.N);
                    
                    }
                   
                    if(Name=="AboveGroundDead")
                        n= GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.N);
                       
                }
                return n;
            }
        }

        /// <summary>
        /// Gets the Nitrogen Concentration of the composite organ
        /// </summary>
        [Units ("g/g")]
        public double NConc
        {
            get
            {
                if (Wt>0)
                    return N/Wt;
                else
                    return 0.0;
            }
        }




    }
}