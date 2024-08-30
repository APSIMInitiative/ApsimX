using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using System.Linq;
using APSIM.Shared.Utilities;
using System;
using Models.Functions.DemandFunctions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for Functions
    /// </summary>
    public class DocFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocFunction" /> class.
        /// </summary>
        public DocFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            newTags.Add(new Paragraph(GetFunctionText()));

            foreach (IModel child in model.FindAllChildren())
                newTags = AutoDocumentation.Document(child, newTags, headingLevel+1, indent+1).ToList();

            return newTags;
        }

        /// <summary>
        /// Get paragraph text on simple functions
        /// </summary>
        public string GetFunctionText()
        {
            if (model is AccumulateAtEvent accumulateAtEvent)
                return $"**{model.Name}** is a daily accumulation of the values of functions listed below between the {accumulateAtEvent.StartStageName} and {(model as AccumulateAtEvent).EndStageName} stages.";
            else if (model is AccumulateResetAtStage accumulateResetAtStage)
                return $"**{model.Name}** is a daily accumulation of the values of functions listed below and set to zero each time the {accumulateResetAtStage.ResetStageName} is passed.";
            else if (model is Constant constant)
                return $"**{model.Name} = {constant.FixedValue} {FindUnits(constant)}";
            else if (model is AccumulateFunction accumulateFunction)
                return $"*{model.Name}* = Accumulated {ChildFunctionList(model)} and between {accumulateFunction.StartStageName.ToLower()} and {accumulateFunction.EndStageName.ToLower()}";
            else if (model is AddFunction addFunction)
                return DocumentMathFunction('+', addFunction);
            else if (model is SubtractFunction subtractFunction)
                return DocumentMathFunction('-', subtractFunction);
            else if (model is MultiplyFunction multiplyFunction)
                return DocumentMathFunction('x', multiplyFunction);
            else if (model is DivideFunction divideFunction)
                return DocumentMathFunction('/', divideFunction);
            else if (model is DailyMeanVPD dailyMeanVPD)
                return $"*MaximumVPDWeight = {dailyMeanVPD.MaximumVPDWeight}*";
            else if (model is DeltaFunction deltaFunction)
                return $"*{model.Name}* is the daily differential of {ChildFunctionList(model)}";
            else if (model is ExpressionFunction expressionFunction)
                return $"{model.Name} = {expressionFunction.Expression.Replace(".Value()", "").Replace("*", "x")}";
            else if (model is HoldFunction holdFunction && holdFunction.FindChild<IFunction>() != null)
                return $"*{model.Name}* = *{holdFunction.FindChild<IFunction>().Name}* until {holdFunction.WhenToHold} after which the value is fixed.";
            else if (model is LessThanFunction lessThanFunction)
            {
                List<IFunction> childFunctions = lessThanFunction.FindAllChildren<IFunction>().ToList();
                if (childFunctions.Count == 4)
                    return $"IF {childFunctions[0].Name} is less than {childFunctions[1].Name} then return {childFunctions[2].Name}, else return {childFunctions[3].Name}";
                else
                    return "";
            }
            else if (model is LinearAfterThresholdFunction linearAfterThresholdFunction)
                return $"*{model.Name}* is calculated as a function of *{StringUtilities.RemoveTrailingString(linearAfterThresholdFunction.XProperty, ".Value()")}*. *Trigger value {linearAfterThresholdFunction.XTrigger} Gradient {linearAfterThresholdFunction.Slope}*";
            else if (model is MovingAverageFunction movingAverageFunction && model.FindChild<IFunction>() != null)
                return $"{model.Name} is calculated from a moving average of {model.FindChild<IFunction>().Name} over a series of {movingAverageFunction.NumberOfDays} days.";
            else if (model is MovingSumFunction movingSumFunction && model.FindChild<IFunction>() != null)
                return $"{model.Name} is calculated from a moving sum of {model.FindChild<IFunction>().Name}.Name over a series of {movingSumFunction.NumberOfDays} days.";
            else if (model is PhaseLookup phaseLookup)
                return $"{model.Name} is calculated using specific values or functions for various growth phases.  The function will use a value of zero for phases not specified below.";
            else if (model is PhaseLookupValue phaseLookupValue)
                return $"{model.Name} has a value between {phaseLookupValue.Start} and {phaseLookupValue.End} calculated as:";
            else if (model is PhotoperiodFunction photoperiodFunction)
                return $"*Twilight = {photoperiodFunction.Twilight} (degrees)*";
            else if (model is SigmoidFunction sigmoidFunction)
                return $"Values of Ymax, Xo, b and XValue are calcuated as defined below.";
            else if (model is StringComparisonFunction stringComparisonFunction)
                return $"If {stringComparisonFunction.PropertyName} = {stringComparisonFunction.StringValue} Then:";
            else if (model is VariableReference variableReference)
                return $"*{model.Name} = {StringUtilities.RemoveTrailingString(variableReference.VariableName, ".Value()")}*";
            else if (model is WangEngelTempFunction wangEngelTempFunction)
                return $"{model.Name} is calculated using a Wang and Engel beta function which has a value of zero below {wangEngelTempFunction.MinTemp} {wangEngelTempFunction.Units} increasing to a maximum value at {wangEngelTempFunction.OptTemp} {wangEngelTempFunction.Units} and decreasing to zero again at {wangEngelTempFunction.MaxTemp} {wangEngelTempFunction.Units} ([WangEngel1998]).";
            else if (model is WeightedTemperatureFunction weightedTemperatureFunction)
                return $"*MaximumTemperatureWeighting = {weightedTemperatureFunction.MaximumTemperatureWeighting}*";
            else if (model is AllometricDemandFunction allometricDemandFunction)
                return $"YValue = {allometricDemandFunction.Const} * XValue ^ {allometricDemandFunction.Power}";
            else if (model is PartitionFractionDemandFunction partitionFractionDemandFunction)
                return $"*{model.Name} = PartitionFraction x [Arbitrator].DM.TotalFixationSupply*";
            else
                return $"";
        }

        /// <summary> 
        /// Creates a list of child function names 
        /// </summary>
        private static string ChildFunctionList(IModel model)
        {
            List<IFunction> childFunctions = model.FindAllChildren<IFunction>().ToList();

            string output = "";
            int total = childFunctions.Count;
            for(int i = 0; i < childFunctions.Count; i++)
            {
                output += "*" + childFunctions[i].Name + "*";                    
                if (i < total - 1)
                    output += ", ";
            }

            return output;
        }

        /// <summary>
        /// Get the units for a constant
        /// </summary>
        private string FindUnits(Constant model)
        {
            if (!string.IsNullOrEmpty(model.Units))
                return $"({model.Units})";

            var parentType = model.Parent.GetType();
            var property = parentType.GetProperty(model.Name);
            if (property != null)
            {
                var unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return $"({unitsAttribute.ToString()})";
            }
            return null;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        private static string DocumentMathFunction(char op, IFunction model)
        {
            string writer = "";
            writer += $"*{model.Name}* = ";

            bool addOperator = false;
            foreach (IModel child in model.Children)
            {
                if (child is IFunction)
                {
                    if (addOperator)
                        writer += $" {op} ";

                    if (child is VariableReference varRef)
                        writer += varRef.VariableName;
                    else if (child is Constant c && NameEqualsValue(c.Name, c.FixedValue))
                        writer += c.FixedValue;
                    else
                    {
                        writer += $"*" + child.Name + "*";
                    }
                    addOperator = true;
                }
            }
            return writer;
        }

        private static bool NameEqualsValue(string name, double value)
        {
            return name.Equals("zero", StringComparison.InvariantCultureIgnoreCase) && value == 0 ||
                   name.Equals("one", StringComparison.InvariantCultureIgnoreCase) && value == 1 ||
                   name.Equals("two", StringComparison.InvariantCultureIgnoreCase) && value == 2 ||
                   name.Equals("three", StringComparison.InvariantCultureIgnoreCase) && value == 3 ||
                   name.Equals("four", StringComparison.InvariantCultureIgnoreCase) && value == 4 ||
                   name.Equals("five", StringComparison.InvariantCultureIgnoreCase) && value == 5 ||
                   name.Equals("six", StringComparison.InvariantCultureIgnoreCase) && value == 6 ||
                   name.Equals("seven", StringComparison.InvariantCultureIgnoreCase) && value == 7 ||
                   name.Equals("eight", StringComparison.InvariantCultureIgnoreCase) && value == 8 ||
                   name.Equals("nine", StringComparison.InvariantCultureIgnoreCase) && value == 9 ||
                   name.Equals("ten", StringComparison.InvariantCultureIgnoreCase) && value == 10 ||
                   name.Equals("constant", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
