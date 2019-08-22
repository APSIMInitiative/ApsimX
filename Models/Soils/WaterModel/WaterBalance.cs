// -----------------------------------------------------------------------
// <copyright file="Soil.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Soils;
    using System;
    using System.Globalization;
    using System.Xml.Serialization;

    /// <summary>
    /// The SoilWater module is a cascading water balance model that owes much to its precursors in 
    /// CERES (Jones and Kiniry, 1986) and PERFECT(Littleboy et al, 1992). 
    /// The algorithms for redistribution of water throughout the soil profile have been inherited from 
    /// the CERES family of models.
    ///
    /// The water characteristics of the soil are specified in terms of the lower limit (ll15), 
    /// drained upper limit(dul) and saturated(sat) volumetric water contents. Water movement is 
    /// described using separate algorithms for saturated or unsaturated flow. It is notable that 
    /// redistribution of solutes, such as nitrate- and urea-N, is carried out in this module.
    ///
    /// Modifications adopted from PERFECT include:
    /// * the effects of surface residues and crop cover on modifying runoff and reducing potential soil evaporation,
    /// * small rainfall events are lost as first stage evaporation rather than by the slower process of second stage evaporation, and
    /// * specification of the second stage evaporation coefficient(cona) as an input parameter, providing more flexibility for describing differences in long term soil drying due to soil texture and environmental effects.
    ///
    /// The module is interfaced with SurfaceOrganicMatter and crop modules so that simulation of the soil water balance 
    /// responds to change in the status of surface residues and crop cover(via tillage, decomposition and crop growth).
    ///
    /// Enhancements beyond CERES and PERFECT include:
    /// * the specification of swcon for each layer, being the proportion of soil water above dul that drains in one day
    /// * isolation from the code of the coefficients determining diffusivity as a function of soil water
    ///   (used in calculating unsaturated flow).Choice of diffusivity coefficients more appropriate for soil type have been found to improve model performance.
    /// * unsaturated flow is permitted to move water between adjacent soil layers until some nominated gradient in 
    ///   soil water content is achieved, thereby accounting for the effect of gravity on the fully drained soil water profile.
    ///
    /// SoilWater is called by APSIM on a daily basis, and typical of such models, the various processes are calculated consecutively. 
    /// This contrasts with models such as SWIM that solve simultaneously a set of differential equations that describe the flow processes.
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    public class WaterBalance : Model, ISoil
    {
        // --- Links -------------------------------------------------------------------------

        /// <summary>Link to the soil properties.</summary>
        [Link]
        private APSIM.Shared.APSoil.Soil properties = null;

        /// <summary>Link to the lateral flow model.</summary>
        [Link]
        private LateralFlowModel lateralFlowModel = null;

        /// <summary>Link to the runoff model.</summary>
        [Link]
        private RunoffModel runoffModel = null;

        /// <summary>Link to the saturated flow model.</summary>
        [Link]
        private SaturatedFlowModel saturatedFlow = null;

        /// <summary>Link to the unsaturated flow model.</summary>
        [Link]
        private UnsaturatedFlowModel unsaturatedFlow = null;

        /// <summary>Link to the evaporation model.</summary>
        [Link]
        private EvaporationModel evaporationModel = null;

        /// <summary>Link to the water table model.</summary>
        [Link]
        private WaterTableModel waterTableModel = null;

        /// <summary>A link to a irrigation data.</summary>
        [Link]
        private IIrrigation irrigation = null;

        /// <summary>A link to a summary data.</summary>
        [Link]
        private ISummary summary = null;

        [Link]
        SoilNitrogen soilNitrogen = null;

        [ScopedLinkByName]
        ISolute NO3 =  null;

        [Link]
        ISolute NH4 = null;

        // --- Settable properties -------------------------------------------------------

        /// <summary>Amount of water in the soil (mm).</summary>
        [XmlIgnore]
        public double[] Water { get; set; }

        /// <summary>Runon (mm).</summary>
        [XmlIgnore]
        public double Runon { get; set; }

        /// <summary>The efficiency (0-1) that solutes move down with water.</summary>
        public double SoluteFluxEfficiency { get; set; }

        /// <summary>The efficiency (0-1) that solutes move up with water.</summary>
        public double SoluteFlowEfficiency { get; set; }

        /// <summary> This is set by Microclimate and is rainfall less that intercepted by the canopy and residue components </summary>
        [XmlIgnore]
        public double PotentialInfiltration { get; set; }

        // --- Outputs -------------------------------------------------------------------

        /// <summary>Lateral flow (mm).</summary>
        [XmlIgnore]
        public double[] LateralFlow { get; private set; }
        
        /// <summary>Runoff (mm).</summary>
        [XmlIgnore]
        public double Runoff { get; private set; }

        /// <summary>Infiltration (mm).</summary>
        [XmlIgnore]
        public double Infiltration { get; private set; }

        /// <summary>Drainage (mm).</summary>
        [XmlIgnore]
        public double Drain { get { return Flux[Flux.Length - 1]; } }

        /// <summary>Evaporation (mm).</summary>
        [XmlIgnore]
        public double Evaporation { get { return evaporationModel.Es; } }

        /// <summary>Water table depth (mm).</summary>
        [XmlIgnore]
        public double WaterTableDepth { get { return waterTableModel.Depth; } }

        /// <summary>Flux. Water moving down (mm).</summary>
        [XmlIgnore]
        public double[] Flux { get; private set; }

        /// <summary>Flow. Water moving up (mm).</summary>
        [XmlIgnore]
        public double[] Flow { get; private set; }

        /// <summary>Gets todays potential runoff (mm).</summary>
        public double PotentialRunoff
        {
            get
            {
                double waterForRunoff = PotentialInfiltration;

                if (irrigation.WillRunoff)
                    waterForRunoff = waterForRunoff + irrigation.IrrigationApplied;

                return waterForRunoff;
            }
        }

        /// <summary>Provides access to the soil properties.</summary>
        public APSIM.Shared.APSoil.Soil Properties {  get { return properties; } }

        // --- Event handlers ------------------------------------------------------------

        /// <summary>Called when a simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Set our water to the initial value.
            //Water = MathUtilities.Multiply(properties.Water.SW, properties.Water.Thickness);
        }

        /// <summary>Called by CLOCK to let this model do its water movement.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            // Calculate lateral flow.
            LateralFlow = lateralFlowModel.Values;
            MathUtilities.Subtract(Water, LateralFlow);

            // Calculate runoff.
            Runoff = runoffModel.Value();

            // Calculate infiltration.
            Infiltration = PotentialInfiltration - Runoff;
            Water[0] = Water[0] + Infiltration;

            // Allow irrigation to infiltrate.
            if (!irrigation.WillRunoff)
            {
                int irrigationLayer = APSIM.Shared.APSoil.SoilUtilities.FindLayerIndex(properties, Convert.ToInt32(irrigation.Depth, CultureInfo.InvariantCulture));
                Water[irrigationLayer] = irrigation.IrrigationApplied;
                Infiltration += irrigation.IrrigationApplied;

                // DeanH - haven't implemented solutes in irrigation water yet.
                // NO3[irrigationLayer] = irrigation.NO3;
                // NH4[irrigationLayer] = irrigation.NH4;
                // CL[irrigationLayer] = irrigation.Cl;
            }

            // Saturated flow.
            Flux = saturatedFlow.Values;

            // Add backed up water to runoff. 
            Water[0] = Water[0] - saturatedFlow.backedUpSurface;

            // Now reduce the infiltration amount by what backed up.
            Infiltration = Infiltration - saturatedFlow.backedUpSurface;

            // Turn the proportion of the infiltration that backed up into runoff.
            Runoff = Runoff + saturatedFlow.backedUpSurface;

            // Should go to pond if one exists.
            //  pond = Math.Min(Runoff, max_pond);
            MoveDown(Water, Flux);

            double[] NO3Values = soilNitrogen.CalculateNO3();
            double[] NH4Values = soilNitrogen.CalculateNH4();

            // Calcualte solute movement down with water.
            double[] NO3Down = CalculateSoluteMovementDown(NO3Values, Water, Flux, SoluteFluxEfficiency);
            double[] NH4Down = CalculateSoluteMovementDown(NH4Values, Water, Flux, SoluteFluxEfficiency);
            MoveDown(NO3Values, NO3Down);
            MoveDown(NH4Values, NH4Down);

            double es = evaporationModel.Calculate();
            Water[0] = Water[0] - es;

            Flow = unsaturatedFlow.Values;
            MoveUp(Water, Flow);

            CheckForErrors();

            double waterTableDepth = waterTableModel.Value();
            double[] NO3Up = CalculateSoluteMovementUpDown(soilNitrogen.CalculateNO3(), Water, Flow, SoluteFlowEfficiency);
            double[] NH4Up = CalculateSoluteMovementUpDown(soilNitrogen.CalculateNH4(), Water, Flow, SoluteFlowEfficiency);
            MoveUp(NO3Values, NO3Up);
            MoveUp(NH4Values, NH4Up);

            // Set deltas
            NO3.SetKgHa(SoluteSetterType.Soil, MathUtilities.Subtract(soilNitrogen.CalculateNO3(), NO3Values));
            NH4.SetKgHa(SoluteSetterType.Soil, MathUtilities.Subtract(soilNitrogen.CalculateNH4(), NH4Values));
        }

        /// <summary>Move water down the profile</summary>
        /// <param name="water">The water values</param>
        /// <param name="flux">The amount to move down</param>
        private static void MoveDown(double[] water, double[] flux)
        {
            for (int i = 0; i < water.Length; i++)
            {
                if (i == 0)
                    water[i] = water[i] - flux[i];
                else
                    water[i] = water[i] + flux[i-1] - flux[i];
            }
        }

        /// <summary>Move water up the profile.</summary>
        /// <param name="water">The water values.</param>
        /// <param name="flow">The amount to move up.</param>
        private static void MoveUp(double[] water, double[] flow)
        {
            for (int i = 0; i < water.Length; i++)
            {
                if (i < water.Length-1)
                    water[i] = water[i] + flow[i+1] - flow[i];
                else
                    water[i] = water[i] - flow[i];
            }
        }

        /// <summary>Calculate the solute movement DOWN based on flux.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flux"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateSoluteMovementDown(double[] solute, double[] water, double[] flux, double efficiency)
        {
            double[] soluteFlux = new double[solute.Length];
            for (int i = 0; i < solute.Length; i++)
            {
                double proportionMoving = flux[i] / water[i];
                if (i == 0)
                    soluteFlux[i] = solute[i] * proportionMoving * efficiency;
                else
                    soluteFlux[i] = (solute[i] + soluteFlux[i-1]) * proportionMoving * efficiency;
            }

            return soluteFlux;
        }

        /// <summary>Calculate the solute movement UP and DOWN based on flow.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flux"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateSoluteMovementUpDown(double[] solute, double[] water, double[] flux, double efficiency)
        {
            double[] soluteUp = CalculateSoluteMovementUp(solute, water, flux, efficiency);
            MoveUp(solute, soluteUp);
            double[] soluteDown = CalculateSoluteMovementDown(solute, water, flux, efficiency);
            return MathUtilities.Subtract(soluteUp, soluteDown);
        }

        /// <summary>Calculate the solute movement UP based on flow.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flow"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateSoluteMovementUp(double[] solute, double[] water, double[] flow, double efficiency)
        {
            double[] soluteFlow = new double[solute.Length];
            for (int i = solute.Length-1; i >= 0;  i--)
            {
                double proportionMoving = flow[i] / water[i];
                if (i == solute.Length - 1)
                    soluteFlow[i] = solute[i] * proportionMoving * efficiency;
                else
                    soluteFlow[i] = (solute[i] + soluteFlow[i + 1]) * proportionMoving * efficiency;
            }

            return soluteFlow;
        }

        /// <summary>Checks for soil for errors.</summary>
        private void CheckForErrors()
        {
            const double specific_bd = 2.65;

            double min_sw = 0.0;

            for (int i = 0; i < properties.Water.Thickness.Length; i++)
            {
               double max_sw = 1.0 - MathUtilities.Divide(properties.Water.BD[i], specific_bd, 0.0);  // ie. Total Porosity
                
                if (MathUtilities.IsLessThan(properties.Water.AirDry[i], min_sw))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                               " Air dry lower limit of ",
                                               properties.Water.AirDry[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below acceptable value of ",
                                               min_sw));

                if (MathUtilities.IsLessThan(properties.Water.LL15[i], properties.Water.AirDry[i]))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                               " 15 bar lower limit of ",
                                               properties.Water.LL15[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below air dry value of ",
                                               properties.Water.AirDry[i]));

                if (MathUtilities.IsLessThanOrEqual(properties.Water.DUL[i], properties.Water.LL15[i]))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                               " drained upper limit of ",
                                               properties.Water.DUL[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is at or below lower limit of ",
                                               properties.Water.LL15[i]));

                if (MathUtilities.IsLessThanOrEqual(properties.Water.SAT[i], properties.Water.DUL[i]))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                               " saturation of ",
                                               properties.Water.SAT[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is at or below drained upper limit of ",
                                               properties.Water.DUL[i]));

                if (MathUtilities.IsGreaterThan(properties.Water.SAT[i], max_sw))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G} {7} {8} {9:G} {10} {11} {12:G})",
                                               " saturation of ",
                                               properties.Water.SAT[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is above acceptable value of ",
                                               max_sw,
                                               "\n",
                                               "You must adjust bulk density (bd) to below ",
                                               (1.0 - properties.Water.SAT[i]) * specific_bd,
                                               "\n",
                                               "OR saturation (sat) to below ",
                                               max_sw));

                if (MathUtilities.IsGreaterThan(Water[i], properties.Water.SAT[i]))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                               " soil water of ",
                                               Water[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is above saturation of ",
                                               properties.Water.SAT[i]));

                if (MathUtilities.IsLessThan(Water[i], properties.Water.AirDry[i]))
                    summary.WriteWarning(this, String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                               " soil water of ",
                                               Water[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below air-dry value of ",
                                               properties.Water.AirDry[i]));
            }

        }
    }
}
