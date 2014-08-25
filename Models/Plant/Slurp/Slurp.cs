using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.Soils;
using System.Xml.Serialization;
using Models.PMF;


namespace Models.PMF.Slurp
{
    /// <summary>
    /// Slurp is a 'dummy' static crop model.  The user sets very basic input information such as ....  These states will
    /// not change during the simulation (no growth or death) unless the states are reset by the user.  
    /// 
    /// Need to check canopy height and depth units.  Micromet documentation says m but looks like is in mm in the module
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Slurp : Model, ICrop2
    {
        /// <summary>
        /// Link to the soil module
        /// </summary>
        [Link] Soils.Soil Soil = null;

        /// <summary>
        /// Link to the Summary file for reporting all sorts of useful information
        /// </summary>
        [Link]
        Summary Summary = null;

        // The variables that are in CanopyProperties
        /// <summary>
        /// Holds the set of crop canopy properties that is used by Arbitrator for light and engergy calculations
        /// </summary>
        public CanopyProperties CanopyProperties { get { return LocalCanopyData; } }
        CanopyProperties LocalCanopyData = new CanopyProperties();

        [Description("Green LAI (m2/m2)")] public double localLAI { get; set; }
        [Description("Total LAI (m2/m2)")] public double localLAItot { get; set; }
        [Description("Light extinction coefficient (-)")]        public double localLightExtinction { get; set; }
        [Description("Height of the canopy (mm)")] public double localCanopyHeight { get; set; }
        [Description("Depth of the canopy (mm)")] public double localCanopyDepth { get; set; }
        [Description("Maximum stomatal conductance (m/s)")] public double localMaximumStomatalConductance { get; set; }
        [Description("Frgr - effect on stomatal conductance (-)")] public double localFrgr { get; set; }
        [Description("Water demand (mm /day)")] public double localDemandWater { get; set; }
        [Description("Nitrogen demand (kgN /ha /day)")] public double localPotentialNitrogenDemand { get; set; }

        public double localCoverGreen { get; set; }
        public double localCoverTot { get; set; }

        // The variables that are in RootProperties
        /// <summary>
        /// Holds the set of crop root properties that is used by Arbitrator for water and nutrient calculations
        /// </summary>
        public RootProperties RootProperties { get { return LocalRootData; } }
        RootProperties LocalRootData = new RootProperties();

        [Description("Rooting Depth (mm)")] public double localRootDepth { get; set; }
        [Description("Root length density at the soil surface (mm/mm3)")] public double localSurfaceRootLengthDensity { get; set; }

        public double[] localRootExplorationByLayer { get; set; }
        public double[] localRootLengthDensityByVolume { get; set; }

        double tempDepthUpper;
        double tempDepthMiddle;
        double tempDepthLower;

        /// <summary>
        /// Arbitrator supplies PotentialEP
        /// </summary>
        [XmlIgnore]
        public double demandWater { get; set; }

        /// <summary>
        /// Arbitrator supplies ActualEP
        /// </summary>
        [XmlIgnore]
        public double[] supplyWater { get; set; }

        /// <summary>
        /// Crop calculates potentialNitrogenDemand after getting its water allocation
        /// </summary>
        [XmlIgnore]
        public double potentialNitrogenDemand { get; set; }

        /// <summary>
        /// Arbitrator supplies actualNitrogenSupply based on soil supply and other crop demand
        /// </summary>
        [XmlIgnore]
        public double actualNitrogenSupply { get; set; }

        /// <summary>
        /// MicroClimate supplies LightProfile
        /// </summary>
        //[XmlIgnore]
        //public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }


        // The following event handler will be called once at the beginning of the simulation
        public override void  OnSimulationCommencing()
        {
            // set the canopy and root properties here - no need to capture the sets from any Managers as they directly set the properties
            CanopyProperties.Name = "Slurp";
            CanopyProperties.CoverGreen = 1.0 - Math.Exp(-1*localLightExtinction*localLAI);
            CanopyProperties.CoverTot = 1.0 - Math.Exp(-1 * localLightExtinction * localLAItot);
            CanopyProperties.CanopyDepth = localCanopyDepth;
            CanopyProperties.CanopyHeight = localCanopyHeight;
            CanopyProperties.LAI = localLAI;
            CanopyProperties.LAItot = localLAItot;
            CanopyProperties.MaximumStomatalConductance = localMaximumStomatalConductance;
            CanopyProperties.HalfSatStomatalConductance = 200.0;  // should this be on the UI?
            CanopyProperties.CanopyEmissivity = 0.96;
            CanopyProperties.Frgr = localFrgr;

            RootProperties.KL = Soil.KL(Name);
            RootProperties.LowerLimitDep = Soil.LL(Name);
            RootProperties.RootDepth = localRootDepth;

            localRootExplorationByLayer = new double[Soil.SoilWater.dlayer.Length];
            localRootLengthDensityByVolume = new double[Soil.SoilWater.dlayer.Length];

            supplyWater = new double[Soil.SoilWater.dlayer.Length];

            tempDepthUpper = 0.0;
            tempDepthMiddle = 0.0;
            tempDepthLower = 0.0;

            demandWater = localDemandWater;

            // calculate root exploration (proprotion of the layer occupied by the roots) for each layer
            for (int i = 0; i < Soil.SoilWater.dlayer.Length; i++)
            {

                tempDepthLower += Soil.SoilWater.dlayer[i];  // increment soil depth thorugh the layers
                tempDepthMiddle = tempDepthLower - Soil.SoilWater.dlayer[i]*0.5;
                tempDepthUpper = tempDepthLower - Soil.SoilWater.dlayer[i];
                if (tempDepthUpper < localRootDepth)        // set the root exploration
                {
                    localRootExplorationByLayer[i] = 1.0;
                }
                else if (tempDepthLower <= localRootDepth)
                {
                    localRootExplorationByLayer[i] = Utility.Math.Divide(localRootDepth - tempDepthUpper, Soil.SoilWater.dlayer[i], 0.0);
                }
                else
                {
                    localRootExplorationByLayer[i] = 0.0;
                }
                // set a triangular root length density by scaling layer depth against maximum rooting depth, constrain the multiplier between 0 and 1
                localRootLengthDensityByVolume[i] = localSurfaceRootLengthDensity * localRootExplorationByLayer[i] * (1.0 - Utility.Math.Constrain(Utility.Math.Divide(tempDepthMiddle, localRootDepth, 0.0), 0.0, 1.0));
            }
            RootProperties.RootExplorationByLayer = localRootExplorationByLayer;
            RootProperties.RootLengthDensityByVolume = localRootLengthDensityByVolume;
        }


        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPlantPotentialGrowth(object sender, EventArgs e)
        {
            // nothing for Slurp to do in here but a full/proper crop model would use the LightProfile, PotenialEP and ActualEP to calculate a 
            // PotentialNDemand - the N that the plant wants in order to satisfy growth after accounting for the water supply

            potentialNitrogenDemand = localPotentialNitrogenDemand;

        }

        [EventSubscribe("DoPlantActualGrowth")]
        private void OnDoPlantActualGrowth(object sender, EventArgs e)
        {
            // At this stage a full/proper crop model would be supplied the N uptake from Arbitrator and it would then complete its calculations for the day
        }

    }   
}