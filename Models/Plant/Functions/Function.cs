using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    [Serializable]
    [XmlInclude(typeof(AccumulateFunction))]
    [XmlInclude(typeof(AddFunction))]
    [XmlInclude(typeof(AgeCalculatorFunction))]
    [XmlInclude(typeof(AirTemperatureFunction))]
    [XmlInclude(typeof(BellCurveFunction))]
    [XmlInclude(typeof(Constant))]
    [XmlInclude(typeof(DivideFunction))]
    [XmlInclude(typeof(ExponentialFunction))]
    [XmlInclude(typeof(ExpressionFunction))]
    [XmlInclude(typeof(ExternalVariable))]
    [XmlInclude(typeof(InPhaseTtFunction))]
    [XmlInclude(typeof(LessThanFunction))]
    [XmlInclude(typeof(LinearInterpolationFunction))]
    [XmlInclude(typeof(MaximumFunction))]
    [XmlInclude(typeof(MinimumFunction))]
    [XmlInclude(typeof(MultiplyFunction))]
    [XmlInclude(typeof(OnEventFunction))]
    [XmlInclude(typeof(PhaseBasedSwitch))]
    [XmlInclude(typeof(PhaseLookup))]
    [XmlInclude(typeof(PhaseLookupValue))]
    [XmlInclude(typeof(PhotoperiodDeltaFunction))]
    [XmlInclude(typeof(PhotoperiodFunction))]
    [XmlInclude(typeof(PowerFunction))]
    [XmlInclude(typeof(SigmoidFunction))]
    [XmlInclude(typeof(SigmoidFunction2))]
    [XmlInclude(typeof(SoilTemperatureDepthFunction))]
    [XmlInclude(typeof(SoilTemperatureFunction))]
    [XmlInclude(typeof(SoilTemperatureWeightedFunction))]
    [XmlInclude(typeof(SplineInterpolationFunction))]
    [XmlInclude(typeof(StageBasedInterpolation))]
    [XmlInclude(typeof(SubtractFunction))]
    [XmlInclude(typeof(VariableReference))]
    [XmlInclude(typeof(WeightedTemperatureFunction))]
    [XmlInclude(typeof(Zadok))]
    [XmlInclude(typeof(DemandFunctions.AllometricDemandFunction))]
    [XmlInclude(typeof(DemandFunctions.InternodeDemandFunction))]
    [XmlInclude(typeof(DemandFunctions.PartitionFractionDemandFunction))]
    [XmlInclude(typeof(DemandFunctions.PopulationBasedDemandFunction))]
    [XmlInclude(typeof(DemandFunctions.PotentialSizeDemandFunction))]
    [XmlInclude(typeof(DemandFunctions.RelativeGrowthRateDemandFunction))]
    [XmlInclude(typeof(StructureFunctions.HeightFunction))]
    [XmlInclude(typeof(StructureFunctions.InPhaseTemperatureFunction))]
    [XmlInclude(typeof(StructureFunctions.MainStemFinalNodeNumberFunction))]
    [XmlInclude(typeof(SupplyFunctions.RUECO2Function))]
    [XmlInclude(typeof(SupplyFunctions.RUEModel))]
    [Description("Base class from which other functions inherit")]
    abstract public class Function: Model
    {
        abstract public double Value { get; }
        virtual public double[] Values { get { return new double[1] { Value }; } }

        virtual public void UpdateVariables(string initial) { }

        [Link]
        protected WeatherFile MetData = null;

        [Link]
        protected Clock Clock = null;

    }
}