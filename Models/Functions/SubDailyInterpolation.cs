using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// This class uses aggregates, using a child aggregation function, sub-daily values from a child response function..
    /// Each of the interpolated values are passed into the response function and then given to the aggregation function.
    /// </summary>

    [Serializable]
    [Description("Uses the specified InterpolationMethod to determine sub daily values then calcualtes a value for the Response at each of these time steps and returns either the sum or average depending on the AgrevationMethod selected")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SubDailyInterpolation : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary> Method for interpolating Max and Min temperature to sub daily values </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IInterpolationMethod InterpolationMethod = null;

        /// <summary>The temperature response function applied to each sub daily temperature and averaged to give daily mean</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction Response = null;

        /// <summary>Method used to agreagate sub daily values</summary>
        [Description("Method used to agregate sub daily temperature function")]
        public AgregationMethod agregationMethod { get; set; }

        /// <summary>Method used to agreagate sub daily values</summary>
        public enum AgregationMethod
        {
            /// <summary>Return average of sub daily values</summary>
            Average,
            /// <summary>Return sum of sub daily values</summary>
            Sum
        }


        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyInput = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyResponse = null;

        /// <summary>Daily average temperature calculated from sub daily temperature interpolations</summary>
        public double Value(int arrayIndex = -1)
        {
            if (SubDailyResponse != null)
            {
                if (agregationMethod == AgregationMethod.Average)
                    return SubDailyResponse.Average();
                if (agregationMethod == AgregationMethod.Sum)
                    return SubDailyResponse.Sum();
                else
                    throw new Exception("invalid agregation method selected in " + this.Name + "temperature interpolation");
            }
            else
                return 0.0;
        }

        /// <summary> Set the sub dialy temperature values for the day then call temperature response function and set value for each sub daily period</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            SubDailyInput = InterpolationMethod.SubDailyValues();
            SubDailyResponse = new List<double>();
            foreach (double sdt in SubDailyInput)
                SubDailyResponse.Add(Response.ValueIndexed(sdt));
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"{Name} is the {agregationMethod.ToString().ToLower()} of sub-daily values from a {Response.GetType().Name}.");

            // Write memos.
            foreach (var tag in DocumentChildren<Memo>())
                yield return tag;

            foreach (var tag in (InterpolationMethod as IModel).Document())
                yield return tag;

            yield return new Paragraph($"Each of the interpolated {InterpolationMethod.OutputValueType}s are then passed into the following Response and the {agregationMethod} taken to give daily {Name}");

            var xyPairs = Response as XYPairs;
            xyPairs.XVariableName ??= "Air temperature (oC)";
            foreach (var tag in (Response as IModel).Document())
                yield return tag;
        }
    }

}
