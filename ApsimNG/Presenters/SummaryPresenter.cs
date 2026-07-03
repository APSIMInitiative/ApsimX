using Models;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Logging;
using Models.Soils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Commands;
using UserInterface.Views;

namespace UserInterface.Presenters
{

    /// <summary>Presenter class for working with a summary component</summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>The summary model to work with.</summary>
        private Summary summaryModel;

        /// <summary>The view model to work with.</summary>
        private ISummaryView summaryView;

        /// <summary>The explorer presenter which manages this presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// This dictionary maps simulation names to lists of messages.
        /// </summary>
        private Dictionary<string, IEnumerable<Message>> messages = new Dictionary<string, IEnumerable<Message>>();

        /// <summary>
        /// This dictionary maps simulation names to lists of initial conditions tables.
        /// </summary>
        private Dictionary<string, IEnumerable<InitialConditionsTable>> initialConditions = new Dictionary<string, IEnumerable<InitialConditionsTable>>();

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="parentPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            summaryModel = model as Summary;
            this.explorerPresenter = parentPresenter;
            summaryView = view as ISummaryView;

            // Populate the messages filter dropdown.
            summaryView.VerbosityDropDown.SelectedEnumValue = summaryModel.Verbosity;

            SetSimulationNamesInView();

            string simulationName = summaryView.SimulationDropDown.SelectedValue;

            if (simulationName != null)
            {
                try
                {
                    messages[simulationName] = summaryModel.GetMessages(simulationName)?.ToArray();
                    initialConditions[simulationName] = summaryModel.GetInitialConditions(simulationName).ToArray();
                }
                catch (Exception error)
                {
                    explorerPresenter.MainPresenter.ShowError(error);
                }

                UpdateView();
            }

            // Trap the verbosity level change event.
            summaryView.VerbosityDropDown.Changed += OnVerbosityChanged;

            // Subscribe to the simulation name changed event.
            summaryView.SimulationDropDown.Changed += this.OnSimulationNameChanged;

        }

        private void OnVerbosityChanged(object sender, EventArgs e)
        {
            MessageType newValue = summaryView.VerbosityDropDown.SelectedEnumValue;
            ICommand command = new ChangeProperty(summaryModel, nameof(summaryModel.Verbosity), newValue);
            explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void SetSimulationNamesInView()
        {
            // populate the simulation names in the view.
            IModel scopedParent = summaryModel.Node.ScopedParent().Model as IModel;

            if (scopedParent is Simulation parentSimulation)
            {
                if (scopedParent.Parent is Experiment)
                    scopedParent = scopedParent.Parent;
                else
                {
                    summaryView.SimulationDropDown.Values = new string[] { parentSimulation.Name };
                    summaryView.SimulationDropDown.SelectedValue = parentSimulation.Name;
                    return;
                }
            }

            if (scopedParent is Experiment experiment)
            {
                string[] simulationNames = experiment.GenerateSimulationDescriptions().Select(s => s.Name).ToArray();
                summaryView.SimulationDropDown.Values = simulationNames;
                if (simulationNames != null && simulationNames.Count() > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames.First();
            }
            else
            {
                List<ISimulationDescriptionGenerator> simulations = summaryModel.Node.FindAll<ISimulationDescriptionGenerator>().Cast<ISimulationDescriptionGenerator>().ToList();
                simulations.RemoveAll(s => s is Simulation && (s as IModel).Parent is Experiment);
                List<string> simulationNames = simulations.SelectMany(m => m.GenerateSimulationDescriptions()).Select(m => m.Name).ToList();
                simulationNames.AddRange(summaryModel.Node.FindAll<Models.Optimisation.CroptimizR>().Select(x => x.Name));
                summaryView.SimulationDropDown.Values = simulationNames.ToArray();
                if (simulationNames != null && simulationNames.Count > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames[0];
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            summaryView.SimulationDropDown.Changed -= this.OnSimulationNameChanged;
            //summaryView.SummaryDisplay.Copy -= OnCopy;
            summaryView.VerbosityDropDown.Changed -= OnVerbosityChanged;
        }

        /// <summary>Populate the summary view.</summary>
        private void UpdateView()
        {
            string simulationName = summaryView.SimulationDropDown.SelectedValue;
            if (simulationName == null)
                return;

            StringBuilder markdown = new StringBuilder();

            // Show Initial Conditions.
            if (summaryView.VerbosityDropDown.SelectedEnumValue >= MessageType.Information)
            {
                // Fetch initial conditions from the model for this simulation name.
                if (!initialConditions.ContainsKey(simulationName))
                    initialConditions[simulationName] = summaryModel.GetInitialConditions(simulationName).ToArray();

                IEnumerable<InitialConditionsTable> initialTables = initialConditions[simulationName];

                // Initial condition tables list for solutes.
                List<InitialConditionsTable> soluteTables = new List<InitialConditionsTable>();
                List<InitialConditionsTable> tablesWithoutSolutes = new List<InitialConditionsTable>();

                OrganiseInitialConditionTables(initialTables, soluteTables, tablesWithoutSolutes);
                AppendInitialConditionsToMarkdown(markdown, tablesWithoutSolutes);
                markdown.Append(BuildSoluteGridMarkdown(soluteTables));
            }

            // Fetch messages from the model for this simulation name.
            if (!messages.ContainsKey(simulationName))
                messages[simulationName] = summaryModel.GetMessages(simulationName).ToArray();

            IEnumerable<Message> filteredMessages = GetFilteredMessages(simulationName);
            var groupedMessages = filteredMessages.GroupBy(m => new { m.Date });
            if (filteredMessages.Any())
            {
                markdown.AppendLine($"## Simulation log");
                markdown.AppendLine();
                markdown.AppendLine(string.Join("", groupedMessages.Select(m =>
                {
                    string previousPath = null;
                    StringBuilder md = new StringBuilder();
                    md.AppendLine($"### {m.Key.Date:yyyy-MM-dd}");
                    md.AppendLine("```");

                    foreach (var msg in m)
                    {
                        if (!string.IsNullOrEmpty(msg.RelativePath))
                        {
                            int spacing = msg.RelativePath.Length + 2;
                            if (previousPath == null || msg.RelativePath != previousPath)
                            {
                                if (msg != m.First())
                                    md.AppendLine();
                                md.Append($"{msg.RelativePath}:".PadRight(spacing));
                                previousPath = msg.RelativePath;
                            }
                            else
                                md.Append(' ', spacing);
                        }
                        md.AppendLine($"{msg.Text}");
                    }
                    md.AppendLine("```");
                    md.AppendLine();
                    return md.ToString();
                })));
            }

            summaryView.SummaryDisplay.Text = markdown.ToString();
        }

        private IEnumerable<Message> GetFilteredMessages(string simulationName)
        {
            if (messages.ContainsKey(simulationName))
            {
                IEnumerable<Message> result = messages[simulationName];
                //result = result.Where(m => m.Severity <= summaryView.MessagesFilter.SelectedEnumValue);

                return result;
            }
            return Enumerable.Empty<Message>();
        }

        private void OrganiseInitialConditionTables(IEnumerable<InitialConditionsTable> initialTables, List<InitialConditionsTable> soluteTables, List<InitialConditionsTable> tablesWithoutSolutes)
        {
            foreach (InitialConditionsTable table in initialTables)
            {
                // Required to get the solutes arranged into a single table.
                if (table.Model is Models.Soils.Solute)
                    soluteTables.Add(table);
                else tablesWithoutSolutes.Add(table);
            }
        }

        private void AppendInitialConditionsToMarkdown(StringBuilder markdown, List<InitialConditionsTable> tablesWithoutSolutes)
        {
            markdown.AppendLine(string.Join("", tablesWithoutSolutes.Select(i => i.ToMarkdown())));
        }

        private string BuildSoluteGridMarkdown(IReadOnlyList<InitialConditionsTable> soluteTables)
        {
            if (soluteTables == null || soluteTables.Count == 0)
                return string.Empty;

            var soluteColumns = soluteTables.Select(BuildSoluteColumns).ToList();
            StringBuilder markdown = new StringBuilder();
            markdown.AppendLine("### Solutes");
            markdown.AppendLine();

            markdown.Append("|Depth (mm)|");
            foreach (var solute in soluteColumns)
                markdown.Append($"{solute.Name} ({solute.PrimaryLabel})|{solute.Name} ({solute.SecondaryLabel})|");
            markdown.AppendLine();

            markdown.Append("|---|");
            foreach (var _ in soluteColumns)
                markdown.Append("---:|---:|");
            markdown.AppendLine();

            int rowCount = soluteColumns.Max(s => s.Depth.Count);
            for (int row = 0; row < rowCount; row++)
            {
                markdown.Append("|");
                markdown.Append(GetAt(soluteColumns[0].Depth, row));
                markdown.Append("|");

                foreach (var solute in soluteColumns)
                {
                    markdown.Append(FormatNumericOrText(GetAt(solute.Primary, row)));
                    markdown.Append("|");
                    markdown.Append(FormatNumericOrText(GetAt(solute.Secondary, row)));
                    markdown.Append("|");
                }
                markdown.AppendLine();
            }

            markdown.AppendLine();
            return markdown.ToString();
        }

        private (string Name, List<string> Depth, List<string> Primary, List<string> Secondary, string PrimaryLabel, string SecondaryLabel) BuildSoluteColumns(InitialConditionsTable table)
        {
            List<InitialCondition> conditions = table.Conditions?.ToList() ?? new List<InitialCondition>();

            InitialCondition depthCondition = conditions.FirstOrDefault(c => string.Equals(c.Name, "Depth", StringComparison.OrdinalIgnoreCase));
            List<InitialCondition> valueConditions = conditions.Where(c => !string.Equals(c.Name, "Depth", StringComparison.OrdinalIgnoreCase)).ToList();

            List<string> depth = SplitValues(depthCondition.Value);
            List<string> primary = SplitValues(valueConditions.ElementAtOrDefault(0).Value);
            List<string> secondary = SplitValues(valueConditions.ElementAtOrDefault(1).Value);

            return (
                table.Model?.Name ?? "Solute",
                depth,
                primary,
                secondary,
                GetLabel(table, valueConditions.ElementAtOrDefault(0), "value"),
                GetLabel(table, valueConditions.ElementAtOrDefault(1), "alt")
            );
        }

        private string GetLabel(InitialConditionsTable table, InitialCondition condition, string fallback)
        {
            if (string.IsNullOrWhiteSpace(condition.Name))
                return fallback;

            if (!string.IsNullOrWhiteSpace(condition.Units))
                return condition.Units;

            if (table.Model is Solute solute)
            {
                bool initialIsPpm = solute.InitialValuesUnits == Solute.UnitsEnum.ppm;
                if (string.Equals(condition.Name, "InitialValues", StringComparison.OrdinalIgnoreCase))
                    return initialIsPpm ? "ppm" : "kg/ha";
                if (string.Equals(condition.Name, "InitialValuesConverted", StringComparison.OrdinalIgnoreCase))
                    return initialIsPpm ? "kg/ha" : "ppm";
            }

            if (string.Equals(condition.Name, "InitialValues", StringComparison.OrdinalIgnoreCase))
                return "initial";
            if (string.Equals(condition.Name, "InitialValuesConverted", StringComparison.OrdinalIgnoreCase))
                return "converted";

            return condition.Name;
        }

        private List<string> SplitValues(string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return new List<string>();

            return values
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private string GetAt(List<string> values, int index)
        {
            if (values == null || index < 0 || index >= values.Count)
                return string.Empty;

            return values[index];
        }

        private string FormatNumericOrText(string value)
        {
            if (double.TryParse(value, out double numericValue))
                return numericValue.ToString("F3");

            return value ?? string.Empty;
        }
    }
}