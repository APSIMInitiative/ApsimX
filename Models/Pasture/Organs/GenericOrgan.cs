using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static Models.GrazPlan.GrazType;
using APSIM.Core;
using Models.PMF.Interfaces;
using Models.PMF;
using Models.PMF.Organs;

namespace Models.GrazPlan.Organs
{

    /// <summary>This is a Organ class with Leaf, Stem and Root. It can be extended to other organs. Currently calculates DM,N and NConc.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Pasture))]
    
    public class GenericOrgan: Model,IStructureDependency,IBiomass,IHasDamageableBiomass,IOrganDamage
    {   
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        


         /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; }


       
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

        private double GetDMRoot()
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL);
            //double result = PastureModel.GetRootMass(GrazType.ptROOT, GrazType.TOTAL, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;
            return result;
             
        }

         /// <summary>
        /// Get the average digestibility of this herbage
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetDMD(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.Digestibility(comp, part);
            PastureModel.MassUnit = sUnit;

            return result;
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

                if (PastureModel != null)
                {
                    if(Name=="Leaf" && IsAboveGround is true)
                        return GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0;
                    if(Name=="Stem" && IsAboveGround is true)
                        return GetDM(GrazType.TOTAL, GrazType.ptSTEM)/10.0;

                    if (Name == "Root" && IsAboveGround is false)
                    {
                        return GetDMRoot()/10.0;
                    }
                    
                }
      
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
                if (PastureModel != null)
                {
                    if(Name=="Leaf"  && IsAboveGround is true)
                        return GetDM(GrazType.TOTAL, GrazType.ptLEAF)/10.0 * GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.N);
                    if(Name=="Stem"  && IsAboveGround is true)
                        return GetDM(GrazType.TOTAL, GrazType.ptSTEM)/10.0 * GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.N);
                    if (Name == "Root" && IsAboveGround is false)
                    {
                        return  GetDMRoot()/10.0 * PastureModel.GetRootConc(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N);
                    }
                }
                
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
                return StructuralN + StorageN;
            }
        }

        /// <summary>
        /// N concentration of Organ Live+ Dead
        /// </summary>
        public double NConc
        {
            get
            {   
                if (Wt > 0)
                {
                    return N/Wt;
                }

                return 0;
                

            }
        }

        private PMF.Biomass liveBiomass = new PMF.Biomass();
        private PMF.Biomass deadBiomass = new PMF.Biomass();
        private int part;



        /// <summary>
        /// Live Biomass
        /// </summary>
        public PMF.Biomass Live
        {
            get
            {
                CalculateLiveDead();
                return liveBiomass;
            }

        }

        /// <summary>
        /// Dead Biomass
        /// </summary>
        public PMF.Biomass Dead
        {
            get
            {
                CalculateLiveDead();
                return deadBiomass;
            }

        }


        /// <summary>
        /// Live digestibility
        /// </summary>
        public double LiveDigestibility
        {
            get
            {   
                 if (PastureModel != null)
                {
                    if (Name == "Leaf")
                    {
                        return GetDMD(sgGREEN, GrazType.ptLEAF);
                    }
                    if (Name == "Stem")
                    {
                        return GetDMD(sgGREEN, GrazType.ptSTEM);
                    }
                    
                }  

                return 0;  
            }
        }

        /// <summary>
        /// Dead digestibility
        /// </summary>
        public double DeadDigestibility
        {
            get
            {   
                 if (PastureModel != null)
                {
                    if (Name == "Leaf")
                    {
                        return GetDMD(sgDRY, GrazType.ptLEAF);
                    }
                    if (Name == "Stem")
                    {
                        return GetDMD(sgDRY, GrazType.ptSTEM);
                    }
                    
                }  

                return 0;  
            }
        }

         /// <summary>A list of material (biomass) that can be damaged.</summary>
         public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Live, true, LiveDigestibility);
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Dead, false, DeadDigestibility);
            }
        }

    /// <summary>
    /// Calculate the values for live and dead biomasses from Established and Dead cohorts.
    /// </summary>
    private void CalculateLiveDead()
        {
           if (PastureModel != null)
            {
           
                if (Name == "Leaf")
                {
 
                    liveBiomass.StructuralWt =  HerbageMassTotalDMD(stESTAB, ptLEAF, TOTAL);
                    liveBiomass.StructuralN = PastureModel.GetHerbageNutr(stESTAB,ptLEAF, TOTAL,TPlantElement.N);

                    deadBiomass.StructuralWt = HerbageMassTotalDMD(stDEAD,ptLEAF,TOTAL); 
                    deadBiomass.StructuralN = PastureModel.GetHerbageNutr(stDEAD, ptLEAF,TOTAL,TPlantElement.N);
                    
                }
                if (Name == "Stem")
                {
                    
                    liveBiomass.StructuralWt = HerbageMassTotalDMD(stESTAB, ptSTEM,TOTAL);
                    liveBiomass.StructuralN = PastureModel.GetHerbageNutr(stESTAB, ptSTEM, TOTAL,TPlantElement.N);
                                        
                    deadBiomass.StructuralWt = HerbageMassTotalDMD(stDEAD,ptSTEM,TOTAL); 
                    deadBiomass.StructuralN = PastureModel.GetHerbageNutr(stDEAD, ptSTEM,TOTAL,TPlantElement.N);
                }
            }
        }


        /// /// <summary>
        /// This method computes live and dead dry matter totals by explicitly summing across all digestibility classes (DMD classes 1..HerbClassNo.) 
        ///This is a refactor of HerbageMassGM2 in grazpastpopn.cs where the GrazPlan engine drives total from the digestibilty class (index 0).
        /// TOTAL is only refreshed during the daily update cycle inside the pasture population model.          
        /// Because GenericOrgan requires an up‑to‑date view of biomass immediately when biomass is removed from an organ and mass balance checks need to be performed
        /// relying on TOTAL (cls = 0) produces inconsistent results.  
        /// This method therefore bypasses the TOTAL pool entirely and reconstructs totals directly ensuring that live and dead
        /// biomass values reflect the current state of the pasture after removal.
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="part"></param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        private double HerbageMassTotalDMD(int comp, int part, int DMD)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= PastureModel.CohortCount() - 1; iCohort++)
            {
                if ((comp == TOTAL) || PastureModel.BelongsIn(iCohort, comp))
                {
                    if(DMD == TOTAL)
                    {
                        for(int cls =1; cls <= HerbClassNo; cls++)
                        {
                            result +=PastureModel.FCohorts[iCohort].Herbage[part,cls].DM;
                        }
                    
                    }
                    else                    
                        result += PastureModel.FCohorts[iCohort].Herbage[part,DMD].DM;
                }
            }
            return result;
        }


    /// <summary>
    /// Removes biomass from organs - leaf and stem across all DMD classes proportionally, sets the herbage in each class and returns the biomass
    /// </summary>
    /// <param name="liveToRemove"></param>
    /// <param name="deadToRemove"></param>
    /// <param name="liveToResidue"></param>
    /// <param name="deadToResidue"></param>
    /// <param name="fractionStanding"></param>
    /// <returns></returns>
    public double RemoveBiomass( double liveToRemove = 0,  double deadToRemove = 0,   double liveToResidue = 0,   double deadToResidue = 0,   double fractionStanding = 0)
    {
        double totalBiomass = 0.0;
        double totalBiomassRemoved = 0.0;
        double PostDMTotal = 0.0;
        
        double totalDM =  PastureModel.GetHerbageMass(stESTAB, TOTAL, TOTAL) +  PastureModel.GetHerbageMass(stDEAD, TOTAL, TOTAL);
        if (totalDM > 0.0)
            {
                if(Name == "Leaf")
                     part = ptLEAF;
     
                if(Name == "Stem")
                    part = ptSTEM;

                for (int cls=1; cls <= HerbClassNo; cls++)
                {
                    double totalBiomasslive = PastureModel.GetHerbageMass(stESTAB, part, cls) ;
                    double totalBiomassdead = PastureModel.GetHerbageMass(stDEAD, part, cls);
                    totalBiomass += totalBiomasslive+totalBiomassdead;
                }
                for (int cls = 1; cls <= HerbClassNo; cls++)
                {
                    double liveDM = PastureModel.GetHerbageMass(stESTAB, part, cls);
                    double liveN = PastureModel.GetHerbageNutr(stESTAB, part, cls, TPlantElement.N);

                    double deadDM = PastureModel.GetHerbageMass(stDEAD, part, cls);
                    double deadN = PastureModel.GetHerbageNutr(stDEAD, part, cls, TPlantElement.N);
                    if(liveDM > 0.0)
                    {
                        double setmass = liveDM - liveDM * liveToRemove;
                        PastureModel.SetHerbageMass(stESTAB, part, cls, setmass); 
                    }
                    else
                    PastureModel.SetHerbageMass(stESTAB, part, cls, 0.0);                                 
                    if(deadDM > 0.0)
                    {
                        double setmass = deadDM - deadDM * deadToRemove;   
                        PastureModel.SetHerbageMass(stDEAD, part, cls, setmass);                        
                    }
                    else
                    PastureModel.SetHerbageMass(stDEAD, part, cls, 0.0);                
                    if(liveN > 0.0)
                    {
                        double setN = liveN - liveN * liveToRemove;
                        PastureModel.SetHerbageNutr(stESTAB, part, cls, TPlantElement.N, setN);
                    }
                    else
                    PastureModel.SetHerbageNutr(stESTAB, part, cls, TPlantElement.N, 0.0);
                    if(deadN > 0.0)
                    {
                        double setN = deadN - deadN * deadToRemove;
                        PastureModel.SetHerbageNutr(stDEAD, part, cls, TPlantElement.N, setN);
                    }
                    else
                    PastureModel.SetHerbageNutr(stDEAD, part, cls, TPlantElement.N, 0.0);
                }
                for (int cls=1; cls <= HerbClassNo; cls++)
                {
                    double PostDMlive = PastureModel.GetHerbageMass(stESTAB, part, cls) ;
                    double PostDMdead = PastureModel.GetHerbageMass(stDEAD, part, cls);
                    PostDMTotal += PostDMlive + PostDMdead;
                }   
                               
                //update live and dead biomasses
                CalculateLiveDead();
                double RemoveDM = totalBiomass - totalBiomassRemoved;
                return RemoveDM;    
            }
            else
                throw new Exception($"There is insufficient biomass to remove.");
        }
    }
}
