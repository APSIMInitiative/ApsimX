using System;
using System.Linq;
using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;
using Models.PMF.Organs;
using Models.Climate;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using APSIM.Core;

namespace Models.Functions
{
    /// <summary> Damage functions of frost and heat stress </summary>
    /// <remarks> <strong>Model Description</strong>:
    /// <para>The damage function is developed in APSIM to account for the effects of frost and heat stresses on yield predictions,
    /// which runs at the daily step.The damage function is the product of the potential yield reduction induced by extreme(i.e.frost and heat) events and the sensitivity of the
    /// yield reduction to the growth stage when the events occur.The potential ratio of yield reduction induced by a frost or heat event is a piece-wise linear function of daily
    /// minimum or maximum air temperature, respectively.The potential ratio of yield reduction ranges from 0 to 1 indicating mild to severe yield reduction induced by an extreme
    /// event. The function is described with three parameters including the lower and upper temperature thresholds and the maximum yield reduction. The sensitivity of yield reduction
    /// to the growth stage is a piece-wise linear function of the growth stage simulated by APSIM. The sensitivity ranges from 0 to 1 indicating the least to most sensitivity of yield
    /// reduction.The function has four parameters: the lower and upper growth stage thresholds of the sensitive period to frost or heat stress, and the lower and upper growth stage
    /// thresholds of the most sensitive period around flowering when sensitivity equals 1. The same function of sensitivity applies to both frost and heat stress but with different
    /// parameterizations.</para><br/>
    /// <para>The values of the parameters of the damage function are estimated by linking the frost- and heat-limited yield (i.e., obtained by applying the damage function to
    /// APSIM-simulated yields) and the corresponding field yields.Currently, the damage function was parameterized for canola and wheat, and it will be available for barley soon.</para><br/>
    /// <strong>Model usage</strong>: <para>Add the <em>FrostHeatDamgeFunctions</em> model under the specific Plant model (i.e., Canola or Wheat model) via the interface</para><br/>
    /// <strong>Model output</strong>: <para>The output variables include
    /// <list type="bullet">
    /// <item><description><em>FrostReductionRatio</em>: Daily yield reduction ratio by the frost event</description></item>
    /// <item><description><em>HeatReductionRatio</em>: Daily yield reduction ratio by the heat event</description></item>
    /// <item><description><em>FrostHeatReductionRatio</em>: Daily yield reduction ratio by the frost and heat events</description></item>
    /// <item><description><em>CumulativeFrostReductionRatio</em>: Cumulative yield reduction ratio induced by the occurred frost events</description></item>
    /// <item><description><em>CumulativeHeatReductionRatio</em>: Cumulative yield reduction ratio induced by the occurred heat events</description></item>
    /// <item><description><em>CumulativeFrostHeatReductionRatio</em>: Cumulative yield reduction ratio induced by the occurred frost and heat events</description></item>
    /// <item><description><em>FrostEventNnumber</em>: Number of frost events during sensitive period</description></item>
    /// <item><description><em>HeatEventNumber</em>: Number of heat events during sensitive period</description></item>
    /// <item><description><em>FrostHeatYield</em>: Frost- and heat-limited yield</description></item>
    /// <item><description><em>FrostSensitivePeriodStartDAS</em>: Start of frost sensitive period in days after sowing</description></item>
    /// <item><description><em>FrostSensitivePeriodEndDAS</em>: End of frost sensitive period in days after sowing</description></item>
    /// <item><description><em>HeatSensitivePeriodStartDAS</em>: Start of heat sensitive period in days after sowing</description></item>
    /// <item><description><em>HeatSensitivePeriodEndDAS</em>: End of heat sensitive period in days after sowing</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [Serializable]
    [Description("The development of frost and heat damage functions for canola and wheat was supported by the Frost and Heat Management Analytics (FAHMA) project " +
        "via funding from the Grains Research and Development Corporation (GRDC; Grant No. CSP2204-009RTX).\n" +
        "When using the damage functions, please use the following reference for more information: Hu, P., He, D., Zheng, B., Whish, J., Kirkegaard, J., Bell, L., Leske, B., Chen, S., Uppal, R., Biddulph, B., Trethowan, R., Beletse, Y., Lilley, J., 2026. " +
        "Event-based frost and heat damage functions improve yield predictions of APSIM canola and wheat: formulation, calibration, and evaluation. " +
        "Agricultural and Forest Meteorology 386, 111239. https://doi.org/10.1016/j.agrformet.2026.111239")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class FrostHeatDamageFunctions : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        private const string HowToUseThisModelText =
@"## About the Frost and Heat Damage Functions model

This model reduces simulated crop yield in response to daily frost and heat events. Each day it
combines two things: (1) the *potential* yield-reduction ratio caused by an extreme minimum
(frost) or maximum (heat) temperature on that day - a piece-wise linear function of temperature
between a lower and upper threshold - and (2) the *sensitivity* of yield to the crop's growth
stage on the day the event occurs - a piece-wise linear function of growth stage, ramping up to
full sensitivity around flowering and back down again. The product of these two gives the actual
daily yield-reduction ratio, which is then compounded over the season to estimate a frost- and
heat-limited yield. Parameter values have been statistically calibrated for wheat and canola (see
this model's `Description` citation for how the model was formulated, calibrated, and evaluated); 
barley support is planned. 

### Outputs produced

- **FrostReductionRatio** - daily actual yield reduction ratio caused by a frost event
- **HeatReductionRatio** - daily actual yield reduction ratio caused by a heat event
- **FrostHeatReductionRatio** - daily actual yield reduction ratio from combined frost and heat events
- **CumulativeFrostReductionRatio** - season-to-date cumulative yield reduction ratio from frost events
- **CumulativeHeatReductionRatio** - season-to-date cumulative yield reduction ratio from heat events
- **CumulativeFrostHeatReductionRatio** - season-to-date cumulative yield reduction ratio from combined frost and heat events
- **FrostEventNumber** - number of frost events counted during the frost-sensitive period
- **HeatEventNumber** - number of heat events counted during the heat-sensitive period
- **FrostHeatYield** (g/m2) - final frost- and heat-limited grain yield
- **FrostSensitivePeriodStartDAS** / **FrostSensitivePeriodEndDAS** (days) - start/end of the frost-sensitive period, in days after sowing
- **HeatSensitivePeriodStartDAS** / **HeatSensitivePeriodEndDAS** (days) - start/end of the heat-sensitive period, in days after sowing

### How to use it

1. Add this model as a child of the **Plant** model you want it to affect - it is only valid under
   a Plant, and its `CropType` must match that Plant's crop type (Wheat or Canola).
2. Open this model and set **Crop to be simulated (CropType)** to `Wheat` or `Canola`. Selecting a
   crop automatically fills in the published, calibrated threshold and sensitivity-period
   parameters below for that crop.
3. Only change the auto-filled parameters if you have your own calibration:
   - **Frost damage**: `FrostLowTT`, `FrostUpTT` (lower/upper minimum-temperature thresholds, degC)
     and `FrostMaxReductionRatio`, `FrostMinReductionRatio` (yield-reduction ratio at each threshold).
   - **Frost sensitive period**: `FrostStartSensitiveGS`, `FrostStartMostSensitiveGS`,
     `FrostEndMostSensitiveGS`, `FrostEndSensitiveGS` (growth stages bounding when frost sensitivity
     ramps up to 1, stays at 1, and ramps back down).
   - **Heat damage**: `HeatLowTT`, `HeatUpTT` (lower/upper maximum-temperature thresholds, degC) and
     `HeatMinReductionRatio`, `HeatMaxReductionRatio` (yield-reduction ratio at each threshold).
   - **Heat sensitive period**: `HeatStartSensitiveGS`, `HeatStartMostSensitiveGS`,
     `HeatEndMostSensitiveGS`, `HeatEndSensitiveGS` (same idea as frost, for heat).
4. Run the simulation - the outputs listed above become available for Report/graphing once a crop
   has been sown (they reset at each sowing event).

### Using this with more than one crop (e.g. a rotation)

If this model is added under more than one crop in the same simulation (for example, Wheat and
Canola both present and sown in rotation), give each instance a **unique name** - e.g.
`WheatFrostHeatDamageFunctions` and `CanolaFrostHeatDamageFunctions` - rather than leaving both at
the default `FrostHeatDamageFunctions`. Then reference outputs in a Report or Graph using the
**fully qualified path** for the crop you mean, e.g. `[Wheat].WheatFrostHeatDamageFunctions.FrostHeatYield`,
rather than the bare `[FrostHeatDamageFunctions]`. This matters because a bare-name lookup from
outside both crops resolves to whichever instance is found first and stays pinned to it for the
whole run - if both instances share a name, any Report using the bare name will silently report
the wrong crop's values, even while the other crop is actively growing. If two instances do end up
with the same name, this model will raise a clear error at the start of the run rather than
silently mis-reporting.

### Funding and citation

The development of frost and heat damage functions for canola and wheat was supported by the
Frost and Heat Management Analytics (FAHMA) project via funding from the Grains Research and
Development Corporation (GRDC; Grant No. CSP2204-009RTX).

When using the damage functions, please use the following reference for more information:
Hu, P., He, D., Zheng, B., Whish, J., Kirkegaard, J., Bell, L., Leske, B., Chen, S., Uppal, R.,
Biddulph, B., Trethowan, R., Beletse, Y., Lilley, J., 2026. Event-based frost and heat damage
functions improve yield predictions of APSIM canola and wheat: formulation, calibration, and
evaluation. Agricultural and Forest Meteorology 386, 111239.
[https://doi.org/10.1016/j.agrformet.2026.111239](https://doi.org/10.1016/j.agrformet.2026.111239)

*(You can safely edit or delete this note; it will not reappear once removed.)*";

        //[Link]
        //Clock Clock;
        [Link]
        Weather Weather = null;
        [Link]
        Plant Plant = null;
        [Link]
        private ISummary Summary = null;

        /// <summary>
        /// Called when the model has been newly created in memory (fresh Add-Model/drag-drop/paste,
        /// or file load/clone). Ensures every FrostHeatDamageFunctions instance carries a short
        /// how-to Memo.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            if (Node != null && Node.FindChild<Memo>("How To Use This Model") == null)
            {
                Memo howTo = new Memo
                {
                    Name = "How To Use This Model",
                    Text = HowToUseThisModelText
                };
                Node.AddChild(howTo);
            }
        }

        // Define parameters

        /// <summary>Define the enum for crop types</summary>
        // update the enum to include a default option
        public enum CropTypes
        {
            /// <summary> Default option </summary>
            [Description("Please choose crop type")]
            SelectCrop,
            
            /// <summary>Wheat crop type.</summary>
            [Description("Wheat")]
            Wheat,
            /// <summary>Canola crop type.</summary>
            [Description("Canola")]
            Canola
        }

        /// <summary>Crop to be simulated</summary>
        [Separator("Crop to be simulated, wheat or canola?")]
        // <summary>Crop to be simulated</summary>
        [Description("Crop to be simulated")]
        //public string CropType { get; set; }
        public CropTypes CropType
        {
            get => cropType;
            set
            {
                cropType = value;
                SetDefaultValues();
            }
        }

        [JsonIgnore]
        private CropTypes cropType = CropTypes.SelectCrop;        


        /// <summary>Frost damage</summary>
        [Separator("Frost damage")]
        // <summary>Lower thereshold</summary>
        [Description("Lower threshold of air temperature for frost damage")]
        public double FrostLowTT { get; set; }

        /// <summary>Yield reduction at lower threshold</summary>
        [Description("Yield reduction ratio of frost damage induced by lower threshold")]
        public double FrostMaxReductionRatio { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of air temperature for frost damage")]
        public double FrostUpTT { get; set; }

        /// <summary>Yield reduction at upper threshold</summary>
        [Description("Yield reduction ratio frost damage induced by upper threshold")]
        public double FrostMinReductionRatio { get; set; }


        /// <summary>Sensitive period of frost damage</summary>
        [Separator("Growth stages to define the sensitive period of frost damage")]
        // <summary>The start of sensitive period of frost damage</summary>
        [Description("Start of sensitive period")]
        public double FrostStartSensitiveGS { get; set; }

        /// <summary>The start of the most sensitive period of frost damage</summary>
        [Description("Start of the most sensitive period (i.e., when sensitivity = 1)")]
        public double FrostStartMostSensitiveGS { get; set; }

        /// <summary>The end of the most sensitive period of frost damagee</summary>
        [Description("End of the most sensitive period (i.e., when sensitivity = 1)")]
        public double FrostEndMostSensitiveGS { get; set; }

        /// <summary>The end of sensitive period of frost damage</summary>
        [Description("End of sensitive period")]
        public double FrostEndSensitiveGS { get; set; }


        /// <summary>Heat damage</summary>
        [Separator("Heat damage")]
        // <summary>Lower threshold</summary>
        [Description("Lower threshold of air temperature for heat damage")]
        public double HeatLowTT { get; set; }

        /// <summary>Yield reduction at lower threshold</summary>
        [Description("Yield reduction ratio of heat damage induced by lower threshold")]
        public double HeatMinReductionRatio { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of air temperature for heat damage")]
        public double HeatUpTT { get; set; }

        /// <summary>Yield reduction at upper threshold</summary>
        [Description("Yield reduction ratio of heat damage induced by upper threshold")]
        public double HeatMaxReductionRatio { get; set; }


        /// <summary>Sensitivity period of heat damage</summary>
        [Separator("Growth stages to define the sensitivity period of heat damage")]
        // <summary>The start of sensitive period of heat damage</summary>
        [Description("Start of sensitive period")]
        public double HeatStartSensitiveGS { get; set; }

        /// <summary>The start of the most sensitive period of heat damage</summary>
        [Description("Start of the most sensitive period (i.e., when sensitivity = 1)")]
        public double HeatStartMostSensitiveGS { get; set; }

        /// <summary>The end of the most sensitive period</summary>
        [Description("End of the most sensitive period (i.e., when sensitivity = 1)")]
        public double HeatEndMostSensitiveGS { get; set; }

        /// <summary>The end of sensitive period of heat damage</summary>
        [Description("End of sensitive period")]
        public double HeatEndSensitiveGS { get; set; }


        // Internal variables
        /// <summary>Overall remainng ratio after frost events.</summary>
        double FrostOverallRemaining;

        /// <summary>Overall remainng ratio after heat events.</summary>
        double HeatOverallRemaining;


        // Output variables
        /// <summary>Daily potential yield reduction ratio by a frost event.</summary>
        [JsonIgnore]
        public double FrostPotentialReductionRatio { get; set; }

        /// <summary>Daily sensitivity of yield reduction to growth stage when the frost event occurs.</summary>
        [JsonIgnore]
        public double FrostSensitivity { get; set; }

        /// <summary>Daily actual yield reduction ratio by frost stress.</summary>
        [JsonIgnore]
        public double FrostReductionRatio { get; set; }

        /// <summary>Daily potential yiled reduction ratio by a heat event.</summary>
        [JsonIgnore]
        public double HeatPotentialReductionRatio { get; set; }

        /// <summary>Daily sensitivity of yield reduction to growth stage when the heat event occurs.</summary>
        [JsonIgnore]
        public double HeatSensitivity { get; set; }

        /// <summary>Daily actual yield reduction ratio by heat stress.</summary>
        [JsonIgnore]
        public double HeatReductionRatio { get; set; }

        /// <summary>Daily actual yield reduction ratio by frost and heat stress.</summary>
        [JsonIgnore]
        public double FrostHeatReductionRatio { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by frost stress.</summary>
        [JsonIgnore]
        public double CumulativeFrostReductionRatio { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by heat stress.</summary>
        [JsonIgnore]
        public double CumulativeHeatReductionRatio { get; set; }

        /// <summary>Number of frost events during sensitive period.</summary>
        [JsonIgnore]
        public double FrostEventNumber { get; set; }

        /// <summary>Number of heat events during sensitive period.</summary>
        [JsonIgnore]
        public double HeatEventNumber { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by frost and heat stress.</summary>
        [JsonIgnore]
        public double CumulativeFrostHeatReductionRatio { get; set; }

        /// <summary>Frost- and heat-limiated yield.</summary>
        [Units("g/m2")]
        [JsonIgnore]
        public double FrostHeatYield { get; set; }

        /// <summary>Start of frost sensitive period in days after sowing.</summary>
        [Units("days")]
        [JsonIgnore]
        public double FrostSensitivePeriodStartDAS { get; set; }

        /// <summary>End of frost sensitive period in days after sowing.</summary>
        [Units("days")]
        [JsonIgnore]
        public double FrostSensitivePeriodEndDAS { get; set; }

        /// <summary>Start of heat sensitive period in days after sowing.</summary>
        [Units("days")]
        [JsonIgnore]
        public double HeatSensitivePeriodStartDAS { get; set; }

        /// <summary>End of heat sensitive period in days after sowing.</summary>
        [Units("days")]
        [JsonIgnore]
        public double HeatSensitivePeriodEndDAS { get; set; }

        // Dictionary to hold default values for each crop type
        private readonly Dictionary<CropTypes, Dictionary<string, double>> cropDefaults = new Dictionary<CropTypes, Dictionary<string, double>>()
        {
            {
                CropTypes.Wheat, new Dictionary<string, double>()
                {
                    { nameof(FrostLowTT), -6.1 },
                    { nameof(FrostMaxReductionRatio), 0.5 },
                    { nameof(FrostUpTT), 0.2 },
                    { nameof(FrostMinReductionRatio), 0 },
                    { nameof(FrostStartSensitiveGS), 5.56 },
                    { nameof(FrostStartMostSensitiveGS), 6.68 },
                    { nameof(FrostEndMostSensitiveGS), 8.43 },
                    { nameof(FrostEndSensitiveGS), 9.00 },
                    { nameof(HeatLowTT), 30.1 },
                    { nameof(HeatMinReductionRatio), 0 },
                    { nameof(HeatUpTT), 38.7 },
                    { nameof(HeatMaxReductionRatio), 0.56 },
                    { nameof(HeatStartSensitiveGS), 6.53 },
                    { nameof(HeatStartMostSensitiveGS), 7.84 },
                    { nameof(HeatEndMostSensitiveGS), 8.87 },
                    { nameof(HeatEndSensitiveGS), 9.20 }
                }
            },
            {
                CropTypes.Canola, new Dictionary<string, double>()
                {
                    { nameof(FrostLowTT), -4.9 },
                    { nameof(FrostMaxReductionRatio), 0.13 },
                    { nameof(FrostUpTT), 0.1 },
                    { nameof(FrostMinReductionRatio), 0 },
                    { nameof(FrostStartSensitiveGS), 5.71 },
                    { nameof(FrostStartMostSensitiveGS), 7.56 },
                    { nameof(FrostEndMostSensitiveGS), 8.38 },
                    { nameof(FrostEndSensitiveGS), 9.01 },
                    { nameof(HeatLowTT), 29.1 },
                    { nameof(HeatMinReductionRatio), 0 },
                    { nameof(HeatUpTT), 39.8 },
                    { nameof(HeatMaxReductionRatio), 0.42 },
                    { nameof(HeatStartSensitiveGS), 5.75 },
                    { nameof(HeatStartMostSensitiveGS), 7.01 },
                    { nameof(HeatEndMostSensitiveGS), 8.31 },
                    { nameof(HeatEndSensitiveGS), 10.18 }
                }
            }
        };

        // Function to set default values using reflection
        private void SetDefaultValues()
        {
            if (CropType == CropTypes.SelectCrop)
            {
                // Clear all parameters when the default option is selected
                Type thisType = this.GetType();
                foreach (var cropDefProperty in cropDefaults[CropTypes.Wheat].Keys) // Using Wheat just to get property names
                {
                    PropertyInfo prop = thisType.GetProperty(cropDefProperty);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(this, 0.0); // Set to default value (0.0)
                    }
                }
            }
            else if (cropDefaults.TryGetValue(CropType, out var defaults))
            {
                Type thisType = this.GetType();
                foreach (var kvp in defaults)
                {
                    PropertyInfo prop = thisType.GetProperty(kvp.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(this, kvp.Value);
                    }
                }
            }
            else
            {
                Summary?.WriteMessage(this, $"Unknown crop type: {CropType}", MessageType.Error);
            }
        }

        /// <summary>Caculates daily potential yield reduction ratio induced by a frost event.</summary>
        private double FrostPotentialReductionRatioFun(double t)
        {
            double ratio = 0.0;
            if (t >= FrostUpTT)
            {
                //ratio = 0.0d;
                ratio = FrostMinReductionRatio;
            }
            else if (t > FrostLowTT && t < FrostUpTT)
            {
                ratio =
                t * ((FrostMinReductionRatio - FrostMaxReductionRatio) / (FrostUpTT - FrostLowTT))
                + (FrostMaxReductionRatio * FrostUpTT - FrostLowTT * FrostMinReductionRatio)
                / (FrostUpTT - FrostLowTT);
            }
            else if (t <= FrostLowTT)
            {
                ratio = FrostMaxReductionRatio;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of yield reduction to growth stage when the frost event occurs.</summary>
        private double FrostSensitivityFun(double GrowthStage)
        {
            double sens = 0.0;
            if (GrowthStage <= FrostStartSensitiveGS)
            {
                sens = 0;
            }
            else if (GrowthStage > FrostStartSensitiveGS && GrowthStage < FrostStartMostSensitiveGS)
            {
                sens =
                GrowthStage * ((1 - 0) / (FrostStartMostSensitiveGS - FrostStartSensitiveGS))
                + (0 * FrostStartMostSensitiveGS - FrostStartSensitiveGS * 1) / (FrostStartMostSensitiveGS - FrostStartSensitiveGS);
            }
            else if (GrowthStage >= FrostStartMostSensitiveGS && GrowthStage <= FrostEndMostSensitiveGS)
            {
                sens = 1.0;
            }
            else if (GrowthStage > FrostEndMostSensitiveGS && GrowthStage < FrostEndSensitiveGS)
            {
                sens =
                GrowthStage * ((0 - 1) / (FrostEndSensitiveGS - FrostEndMostSensitiveGS))
                + (1 * FrostEndSensitiveGS - FrostEndMostSensitiveGS * 0) / (FrostEndSensitiveGS - FrostEndMostSensitiveGS);
            }
            else if (GrowthStage >= FrostEndSensitiveGS)
            {
                sens = 0;
            }
            return sens;
        }

        /// <summary>Caculates daily potential yield reduction ratio incuded by a heat event.</summary>
        private double HeatPotentialReductionRatioFun(double t)
        {
            double ratio = 0.0;
            if (t <= HeatLowTT)
            {
                //ratio = 0.0;
                ratio = HeatMinReductionRatio;
            }
            else if (t > HeatLowTT && t < HeatUpTT)
            {
                ratio =
                t * ((HeatMaxReductionRatio - HeatMinReductionRatio) / (HeatUpTT - HeatLowTT))
                + (HeatMinReductionRatio * HeatUpTT - HeatLowTT * HeatMaxReductionRatio)
                / (HeatUpTT - HeatLowTT);
            }
            else if (t >= HeatUpTT)
            {
                ratio = HeatMaxReductionRatio;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of yield reduction when the heat event occurs.</summary>
        private double HeatSensitivityFun(double GrowthStage)
        {
            double sens = 0;
            if (GrowthStage <= HeatStartSensitiveGS)
            {
                sens = 0;
            }
            else if (GrowthStage > HeatStartSensitiveGS && GrowthStage < HeatStartMostSensitiveGS)
            {
                sens =
                GrowthStage * ((1 - 0) / (HeatStartMostSensitiveGS - HeatStartSensitiveGS))
                + (0 * HeatStartMostSensitiveGS - HeatStartSensitiveGS * 1) / (HeatStartMostSensitiveGS - HeatStartSensitiveGS);
            }
            else if (GrowthStage >= HeatStartMostSensitiveGS && GrowthStage <= HeatEndMostSensitiveGS)
            {
                sens = 1.0;
            }
            else if (GrowthStage > HeatEndMostSensitiveGS && GrowthStage < HeatEndSensitiveGS)
            {
                sens =
                GrowthStage * ((0 - 1) / (HeatEndSensitiveGS - HeatEndMostSensitiveGS))
                + (1 * HeatEndSensitiveGS - HeatEndMostSensitiveGS * 0) / (HeatEndSensitiveGS - HeatEndMostSensitiveGS);
            }
            else if (GrowthStage >= HeatEndSensitiveGS)
            {
                sens = 0;
            }
            return sens;
        }

        /// <summary>
        /// Find the node whose subtree should be searched for other `FrostHeatDamageFunctions` instances
        /// when checking for a name collision. Prefers the nearest enclosing Simulation - matching how a
        /// Report's bare-name variable lookup is actually scoped, so unrelated Simulations elsewhere in
        /// the same file are never treated as colliding - and falls back to the whole file's root only
        /// when there is no enclosing Simulation, which is the case for a model living in a Replacements
        /// folder (that sits outside every Simulation but can be merged into any of them). Returns null if
        /// this model is not yet attached to a tree.
        /// </summary>
        private Node FindAmbiguityScopeRoot()
        {
            if (Node == null)
                return null;
            return Node.WalkParents().FirstOrDefault(n => n.Model is Simulation) ?? Node.WalkParents().LastOrDefault() ?? Node;
        }

        //// <summary>Validate inputs</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnDoCommencing(object sender, EventArgs e)
        {
            // Don't run if no valid crop type is selected
            if (CropType == CropTypes.SelectCrop)
            {
                throw new Exception($"Please select a crop type in the `FrostHeatDamageFunctions` before running.");
            }

            // Check if the selected crop type matches the plant type in the simulation
            string actualPlantType = Plant.PlantType;
            string selectedCropType = CropType.ToString();

            List<string> plantTypes = new List<string>();
            foreach(CropTypes type in Enum.GetValues(typeof(CropTypes)))
                plantTypes.Add(type.ToString());

            //check if the crop this is on exists in the enum, and then check if the correct crop was selected
            //if the crop is not in the list, don't throw on this to allow prototying for new crops.
            if (plantTypes.Contains(Plant.PlantType))
            {
                // Compare the selected crop type with the plant type in simulation
                if (!actualPlantType.Equals(selectedCropType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"The selected crop type '{selectedCropType}' in the `FrostHeatDamageFunctions` does not match the plant type '{actualPlantType}' in the simulation. " +
                        $"Please select the correct crop type in the `FrostHeatDamageFunctions`.");
                }
            }

            // In a crop rotation, more than one crop (e.g. Wheat and Canola) can each have their own
            // `FrostHeatDamageFunctions` child. If those instances share the same name (the default when
            // added via a resource Replacements folder), a variable reference that uses the bare name
            // (e.g. "[FrostHeatDamageFunctions].FrostHeatYield" in a Report) will always resolve to
            // whichever instance is found first in scope - silently ignoring the other crop's values for
            // the entire simulation, even while that crop is the one actually growing. 
            Node root = FindAmbiguityScopeRoot() ?? Node;
            bool ambiguousName = root.Walk().Any(n => n.Model is FrostHeatDamageFunctions other && other != this
                && n.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
            if (ambiguousName)
            {
                throw new Exception($"More than one `FrostHeatDamageFunctions` model named '{Name}' was found in scope, " +
                    "most likely one under each crop in a rotation. Any variable reference using the ambiguous name " +
                    $"'[{Name}]' would always resolve to the same instance, silently ignoring the other crop's values. " +
                    $"Give each `FrostHeatDamageFunctions` instance a unique name (e.g. '{selectedCropType}{Name}') and " +
                    $"reference it with a fully qualified path, e.g. '[{Plant.Name}].{Name}.FrostHeatYield'. " +
                    "See the 'How To Use This Model' note under this model for more information.");
            }
        }

        /// <summary>Initializing the variables</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>    
        [EventSubscribe("Sowing")]
        private void OnDoSowing(object sender, EventArgs e)
        {
            // initialize
            Summary.WriteMessage(this, "FrostHeatDamageFunctions will be performed.", Core.MessageType.Information);

            FrostPotentialReductionRatio = 0;
            FrostSensitivity = 0;
            FrostReductionRatio = 0;
            HeatPotentialReductionRatio = 0;
            HeatSensitivity = 0;
            HeatReductionRatio = 0;
            FrostHeatReductionRatio = 0;
            FrostOverallRemaining = 1;
            HeatOverallRemaining = 1;
            CumulativeFrostReductionRatio = 0;
            CumulativeHeatReductionRatio = 0;
            CumulativeFrostHeatReductionRatio = 0;
            FrostHeatYield = 0;
            FrostEventNumber = 0;
            HeatEventNumber = 0;
            FrostSensitivePeriodStartDAS = -1;
            FrostSensitivePeriodEndDAS = -1;
            HeatSensitivePeriodStartDAS = -1;
            HeatSensitivePeriodEndDAS = -1;
        }

        /// <summary>Does the calculations of multiplers and sensitivities of frost and heat stresses.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoManagementCalculations")]
        private void OnDoManagementCalculations(object sender, EventArgs e)
        {
            if (Plant != this.Parent)
                throw new Exception("Error: `FrostHeatDamageFunctions` has linked with a Plant that is not its parent");

            if (!Plant.IsAlive)
                return;
    
            Phenology phen = Plant.Phenology;
            ReproductiveOrgan organs = Plant.Node.FindChild<ReproductiveOrgan>("Grain");

            double GrowthStageToday = phen.Stage;
            //GrowthStageToday = phen.Zadok;
            double DaysAfterSowingToday = Plant.DaysAfterSowing;

            // Track frost sensitive period start and end
            if (GrowthStageToday >= FrostStartSensitiveGS && FrostSensitivePeriodStartDAS < 0)
            {
                FrostSensitivePeriodStartDAS = DaysAfterSowingToday;
            }
            if (GrowthStageToday >= FrostEndSensitiveGS && FrostSensitivePeriodEndDAS < 0)
            {
                FrostSensitivePeriodEndDAS = DaysAfterSowingToday;
            }

            // Track heat sensitive period start and end
            if (GrowthStageToday >= HeatStartSensitiveGS && HeatSensitivePeriodStartDAS < 0)
            {
                HeatSensitivePeriodStartDAS = DaysAfterSowingToday;
            }
            if (GrowthStageToday >= HeatEndSensitiveGS && HeatSensitivePeriodEndDAS < 0)
            {
                HeatSensitivePeriodEndDAS = DaysAfterSowingToday;
            }

            // Daily potential yield reduction ratio by a frost event
            FrostPotentialReductionRatio = FrostPotentialReductionRatioFun(Weather.MinT);

            // Daily sensitivity of yield reduction to the growth stage when a frost event occurs
            FrostSensitivity = FrostSensitivityFun(GrowthStageToday);

            // Daily actual yield reduction by a frost event
            FrostReductionRatio = FrostPotentialReductionRatio * FrostSensitivity;
            // Count frost events
            if (FrostReductionRatio > 0)
            {
                FrostEventNumber++;
            }
            // Daily potential yield reduction by a heat event
            HeatPotentialReductionRatio = HeatPotentialReductionRatioFun(Weather.MaxT);

            // Daily sensitivity of yield reduction to the growth stage when a heat frost event occurs
            HeatSensitivity = HeatSensitivityFun(GrowthStageToday);

            // Daily actual yield reduction by a heat event
            HeatReductionRatio = HeatPotentialReductionRatio * HeatSensitivity;
            // Count heat events
            if (HeatReductionRatio > 0)
            {
                HeatEventNumber++;
            }

            // Daily actual yield reduction by the frost and heat events
            FrostHeatReductionRatio = 1 - (1 - FrostReductionRatio) * (1 - HeatReductionRatio);

            // Cumulative yield reduction by frost events
            FrostOverallRemaining = FrostOverallRemaining * (1 - FrostReductionRatio);
            CumulativeFrostReductionRatio = 1 - FrostOverallRemaining;

            // Cumulative yield reduction by heat events
            HeatOverallRemaining = HeatOverallRemaining * (1 - HeatReductionRatio);
            CumulativeHeatReductionRatio = 1 - HeatOverallRemaining;

            // Cumulative yield reduction by frost and heat events
            CumulativeFrostHeatReductionRatio = 1 - FrostOverallRemaining * HeatOverallRemaining;

            // Frost- and heat-limited yield
            FrostHeatYield = organs.Wt * FrostOverallRemaining * HeatOverallRemaining;
        }
    }
}
