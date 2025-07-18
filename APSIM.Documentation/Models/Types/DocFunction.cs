using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using System.Linq;
using APSIM.Shared.Utilities;
using System;
using Models.PMF.Struct;
using Models.Functions.DemandFunctions;
using Models;
using APSIM.Core;

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
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            string text = GetFunctionText(this.model as IFunction);
            if (text.Length > 0)
                section.Add(new Paragraph(text));

            List<ITag> subTags = new();
            foreach (IModel child in model.FindAllChildren())
                if (!(child is Memo))
                    subTags.AddRange(AutoDocumentation.DocumentModel(child));

            section.Add(subTags);

            return new List<ITag>() {section};
        }

        /// <summary>
        /// Get paragraph text on simple functions
        /// </summary>
        public static string GetFunctionText(IFunction function)
        {
            var functionAsModel = function as IModel;
            if (function is AccumulateAtEvent accumulateAtEvent)
                return $"{functionAsModel.Name} is a daily accumulation of the values of functions listed below between the {accumulateAtEvent.StartStageName} and {(function as AccumulateAtEvent).EndStageName} stages.";
            else if (function is AccumulateResetAtStage accumulateResetAtStage)
                return $"{functionAsModel.Name} is a daily accumulation of the values of functions listed below and set to zero each time the {accumulateResetAtStage.ResetStageName} is passed.";
            else if (function is Constant constant)
                return $"{functionAsModel.Name} = {constant.FixedValue} {FindUnits(constant)}";
            else if (function is AccumulateFunction accumulateFunction)
                return $"*{functionAsModel.Name}* = Accumulated {ChildFunctionList(functionAsModel)} and between {accumulateFunction.StartStageName.ToLower()} and {accumulateFunction.EndStageName.ToLower()}";
            else if (function is AddFunction addFunction)
                return DocumentMathFunction('+', addFunction);
            else if (function is SubtractFunction subtractFunction)
                return DocumentMathFunction('-', subtractFunction);
            else if (function is MultiplyFunction multiplyFunction)
                return DocumentMathFunction('x', multiplyFunction);
            else if (function is DivideFunction divideFunction)
                return DocumentMathFunction('/', divideFunction);
            else if (function is DailyMeanVPD dailyMeanVPD)
                return $"*MaximumVPDWeight = {dailyMeanVPD.MaximumVPDWeight}*";
            else if (function is DeltaFunction deltaFunction)
                return $"*{functionAsModel.Name}* is the daily differential of {ChildFunctionList(functionAsModel)}";
            else if (function is ExpressionFunction expressionFunction)
                return $"{functionAsModel.Name} = {expressionFunction.Expression.Replace(".Value()", "").Replace("*", "x")}";
            else if (function is HoldFunction holdFunction && holdFunction.FindChild<IFunction>() != null)
                return $"*{functionAsModel.Name}* = *{holdFunction.FindChild<IModel>().Name}* until {holdFunction.WhenToHold} after which the value is fixed.";
            else if (function is LessThanFunction lessThanFunction)
            {
                List<IModel> childFunctions = lessThanFunction.FindAllChildren<IModel>().ToList();
                if (childFunctions.Count == 4)
                    return $"IF {childFunctions[0].Name} is less than {childFunctions[1].Name} then return {childFunctions[2].Name}, else return {childFunctions[3].Name}";
                else
                    return "";
            }
            else if (function is LinearAfterThresholdFunction linearAfterThresholdFunction)
                return $"*{functionAsModel.Name}* is calculated as a function of *{StringUtilities.RemoveTrailingString(linearAfterThresholdFunction.XProperty, ".Value()")}*. *Trigger value {linearAfterThresholdFunction.XTrigger} Gradient {linearAfterThresholdFunction.Slope}*";
            else if (function is MovingAverageFunction movingAverageFunction && functionAsModel.FindChild<IFunction>() != null)
                return $"{functionAsModel.Name} is calculated from a moving average of {functionAsModel.FindChild<IModel>().Name} over a series of {movingAverageFunction.NumberOfDays} days.";
            else if (function is MovingSumFunction movingSumFunction && functionAsModel.FindChild<IFunction>() != null)
                return $"{functionAsModel.Name} is calculated from a moving sum of {functionAsModel.FindChild<IModel>().Name}.Name over a series of {movingSumFunction.NumberOfDays} days.";
            else if (function is BudNumberFunction budNumberFunction && functionAsModel.FindChild<IFunction>() != null)
                return $"Each time {budNumberFunction.SetStage} occurs, bud number on each main-stem is set to:" +
                $"*{budNumberFunction.FindChild<IModel>("FractionOfBudBurst").Name}* * *SowingData.BudNumber* (from manager at establishment)";
            else if (function is PhaseLookup phaseLookup)
                return $"{functionAsModel.Name} is calculated using specific values or functions for various growth phases.  The function will use a value of zero for phases not specified below.";
            else if (function is PhaseLookupValue phaseLookupValue)
                return $"{functionAsModel.Name} has a value between {phaseLookupValue.Start} and {phaseLookupValue.End} calculated as:";
            else if (function is PhotoperiodFunction photoperiodFunction)
                return $"*Twilight = {photoperiodFunction.Twilight} (degrees)*";
            else if (function is SigmoidFunction sigmoidFunction)
                return $"Values of Ymax, Xo, b and XValue are calcuated as defined below.";
            else if (function is StringComparisonFunction stringComparisonFunction)
                return $"If {stringComparisonFunction.PropertyName} = {stringComparisonFunction.StringValue} Then:";
            else if (function is VariableReference variableReference)
                return $"*{functionAsModel.Name} = {StringUtilities.RemoveTrailingString(variableReference.VariableName, ".Value()")}*";
            else if (function is WangEngelTempFunction wangEngelTempFunction)
                return $"{functionAsModel.Name} is calculated using a Wang and Engel beta function which has a value of zero below {wangEngelTempFunction.MinTemp} {wangEngelTempFunction.Units} increasing to a maximum value at {wangEngelTempFunction.OptTemp} {wangEngelTempFunction.Units} and decreasing to zero again at {wangEngelTempFunction.MaxTemp} {wangEngelTempFunction.Units} ([WangEngel1998]).";
            else if (function is WeightedTemperatureFunction weightedTemperatureFunction)
                return $"*MaximumTemperatureWeighting = {weightedTemperatureFunction.MaximumTemperatureWeighting}*";
            else if (function is AllometricDemandFunction allometricDemandFunction)
                return $"YValue = {allometricDemandFunction.Const} * XValue ^ {allometricDemandFunction.Power}";
            else if (function is PartitionFractionDemandFunction partitionFractionDemandFunction)
                return $"*{functionAsModel.Name} = PartitionFraction x [Arbitrator].DM.TotalFixationSupply*";
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
                var childAsModel = childFunctions[i] as IModel;
                output += "*" + childAsModel.Name + "*";
                if (i < total - 1)
                    output += ", ";
            }

            return output;
        }

        /// <summary>
        /// Get the units for a constant
        /// </summary>
        private static string FindUnits(Constant model)
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
        private static string DocumentMathFunction(char op, IFunction function)
        {
            IModel model = function as IModel;
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
