﻿using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// This model calculates the CO~2~ impact on stomatal conductance using the approach of [Elli2020].
    /// 
    ///     _StomatalConductanceCO2Modifier = PhotosynthesisCO2Modifier x (350 - CP)/(CO~2~ - CP)_
    ///     
    /// where CP, is the compensation point calculated from daily average temperature (T) as
    /// 
    ///     _CP = (163.0 - T) / (5.0 - 0.1 * T)_
    ///     
    /// </summary>
    [Serializable]
    [Description("This model calculates CO2 Impact on stomatal conductance RUE using the approach of <br>Elli et al (2020) <br>Global sensitivity-based modelling approach to identify suitable Eucalyptus traits for adaptation to climate variability and change. <br> in silico Plants Vol. 2, No. 1, pp. 1–17")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class StomatalConductanceCO2Modifier : Model, IFunction
    {
        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Photosynthesis CO2 Modifier</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PhotosynthesisCO2Modifier = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (MetData.CO2 < 350)
                throw new Exception("CO2 concentration too low for Stomatal Conductance CO2 Function");
            else if (MetData.CO2 == 350)
                return 1.0;
            else
            {
                double temp = (MetData.MaxT + MetData.MinT) / 2.0; // Average temperature
                double CP = (163.0 - temp) / (5.0 - 0.1 * temp);  //co2 compensation point (ppm)

                double first = (MetData.CO2 - CP);
                double second = (350.0 - CP);
                return PhotosynthesisCO2Modifier.Value() / (first / second);
            }
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class from summary and remarks XML documentation.
            foreach (var tag in GetModelDescription())
                yield return tag;

            foreach (var tag in DocumentChildren<IModel>())
                yield return tag;
        }
    }
}