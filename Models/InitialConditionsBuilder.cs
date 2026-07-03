using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Storage;
using Models.Utilities;

namespace Models
{
    /// <summary>
    /// Builds the '_InitialConditions' DataTable for a simulation by reflecting
    /// over all models and collecting properties marked with <see cref="Models.Core.SummaryAttribute"/>.
    /// </summary>
    public static class InitialConditionsBuilder
    {
        /// <summary>
        /// Build and return the initial conditions DataTable for the given simulation.
        /// The caller is responsible for writing the returned table to a data store.
        /// </summary>
        /// <param name="simulation">The simulation whose initial conditions should be captured.</param>
        /// <returns>A DataTable named '_InitialConditions'.</returns>
        public static DataTable Build(Simulation simulation)
        {
            var initConditions = new DataTable("_InitialConditions");
            initConditions.Columns.Add("SimulationName", typeof(string));
            initConditions.Columns.Add("ModelPath", typeof(string));
            initConditions.Columns.Add("Name", typeof(string));
            initConditions.Columns.Add("Description", typeof(string));
            initConditions.Columns.Add("DataType", typeof(string));
            initConditions.Columns.Add("Units", typeof(string));
            initConditions.Columns.Add("DisplayFormat", typeof(string));
            initConditions.Columns.Add("Total", typeof(int));
            initConditions.Columns.Add("Value", typeof(string));

            string simulationPath = simulation.FullPath;

            DataRow row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "Simulation name", "Simulation name", "String", string.Empty, string.Empty, 0, simulation.Name };
            initConditions.Rows.Add(row);

            row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "APSIM version", "APSIM version", "String", string.Empty, string.Empty, 0, Simulations.GetApsimVersion() };
            initConditions.Rows.Add(row);

            row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "Run on", "Run on", "String", string.Empty, string.Empty, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            initConditions.Rows.Add(row);

            foreach (Model model in simulation.Node.FindAll<IModel>())
            {
                string modelPath = model.FullPath.Replace(simulationPath + ".", string.Empty);

                foreach (var property in FindSummaryProperties(model))
                {
                    object propertyValue = (string)ApsimConvert.ToType(property.info.GetValue(model), typeof(string));
                    if (propertyValue?.ToString() != string.Empty)
                    {
                        if (propertyValue is DateTime dateValue)
                            propertyValue = dateValue.ToString("yyyy-MM-dd HH:mm:ss");

                        int total = 0;
                        var description = ReflectionUtilities.GetAttribute(property.info, typeof(DescriptionAttribute), false)?.ToString();
                        var units = ReflectionUtilities.GetAttribute(property.info, typeof(UnitsAttribute), false)?.ToString();
                        string format = property.info.GetFormat();

                        row = initConditions.NewRow();
                        row.ItemArray = new object[] { simulation.Name, modelPath, property.name, description, property.info.PropertyType.Name, units, format, total, propertyValue };
                        initConditions.Rows.Add(row);
                    }
                }
            }

            return initConditions;
        }

        /// <summary>
        /// Find all public instance properties on the given model that are decorated
        /// with the <see cref="Models.Core.SummaryAttribute"/>.
        /// </summary>
        /// <param name="model">The model to inspect.</param>
        /// <returns>Name and PropertyInfo tuples for each matching property.</returns>
        private static IEnumerable<(string name, PropertyInfo info)> FindSummaryProperties(Model model)
        {
            if (model == null)
                yield break;

            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                if (property.IsDefined(typeof(SummaryAttribute), false))
                    yield return (property.Name, property);
            }
        }
    }
}
