using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Logging;
using UserInterface.Commands;
using UserInterface.EventArguments;
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
            summaryView.MessagesFilter.SelectedEnumValue = MessageType.All;
            summaryView.VerbosityDropDown.SelectedEnumValue = summaryModel.Verbosity;

            // Show initial conditions table by default.
            summaryView.ShowInitialConditions.Checked = true;

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

            // Trap the message filter level change event.
            summaryView.MessagesFilter.Changed += OnFilterChanged;

            // Subscribe to the simulation name changed event.
            summaryView.SimulationDropDown.Changed += this.OnSimulationNameChanged;

            // Trap the 'show initial conditions' checkbox's changed event.
            summaryView.ShowInitialConditions.Changed += OnFilterChanged;
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void OnVerbosityChanged(object sender, EventArgs e)
        {
            MessageType newValue = summaryView.VerbosityDropDown.SelectedEnumValue;
            ICommand command = new ChangeProperty(summaryModel, nameof(summaryModel.Verbosity), newValue);
            explorerPresenter.CommandHistory.Add(command);
        }

        private void SetSimulationNamesInView()
        {
            // populate the simulation names in the view.
            IModel scopedParent = ScopingRules.FindScopedParentModel(summaryModel);

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
                List<ISimulationDescriptionGenerator> simulations = summaryModel.FindAllInScope<ISimulationDescriptionGenerator>().Cast<ISimulationDescriptionGenerator>().ToList();
                simulations.RemoveAll(s => s is Simulation && (s as IModel).Parent is Experiment);
                List<string> simulationNames = simulations.SelectMany(m => m.GenerateSimulationDescriptions()).Select(m => m.Name).ToList();
                simulationNames.AddRange(summaryModel.FindAllInScope<Models.Optimisation.CroptimizR>().Select(x => x.Name));
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
            summaryView.MessagesFilter.Changed -= OnFilterChanged;
            summaryView.ShowInitialConditions.Changed -= OnFilterChanged;
        }

        /// <summary>Populate the summary view.</summary>
        private void UpdateView()
        {
            string simulationName = summaryView.SimulationDropDown.SelectedValue;
            if (simulationName == null)
                return;

            StringBuilder markdown = new StringBuilder();

            // Show Initial Conditions.
            if (summaryView.ShowInitialConditions.Checked)
            {
                // Fetch initial conditions from the model for this simulation name.
                if (!initialConditions.ContainsKey(simulationName))
                    initialConditions[simulationName] = summaryModel.GetInitialConditions(simulationName).ToArray();

                //markdown.AppendLine(string.Join("", initialConditions[simulationName].Select(i => i.ToMarkdown())));
                IEnumerable<InitialConditionsTable> initialTables = initialConditions[simulationName].Select(i => i);
                // Initial condition tables list for solutes.
                List<InitialConditionsTable> soluteTables = new List<InitialConditionsTable>();
                List<InitialConditionsTable> tablesWithoutSolutes = new List<InitialConditionsTable>();
                // Custom data table for solutes.
                DataTable soluteTable = new()
                {
                    TableName = "Solutes"
                };
                foreach (InitialConditionsTable table in initialTables)
                {
                    // Required to get the solutes arranged into a single table.
                    if (table.Model is Models.Soils.Solute)
                    {
                        soluteTables.Add(table);
                    }
                    else
                    {
                        tablesWithoutSolutes.Add(table);
                    }
                }
                // Print out a set of initial conditions without the solutes.
                markdown.AppendLine(string.Join("", tablesWithoutSolutes.Select(i => i.ToMarkdown())));
                // Now arrange solutes into a nice markdown table.
                StringBuilder soluteMarkdownTable = new StringBuilder();
                if (soluteTables.Count > 0)
                    soluteMarkdownTable.AppendLine("### Solutes");
                soluteMarkdownTable.AppendLine();
                soluteMarkdownTable.Append("|");


                // Table headings
                bool isFirstTableNamePrinted = false;
                foreach (InitialConditionsTable table in soluteTables)
                {
                    if (!isFirstTableNamePrinted)
                    {
                        soluteMarkdownTable.AppendFormat("{0}|   |   |", table.Model.Name);
                        isFirstTableNamePrinted = true;
                    }
                    else
                        soluteMarkdownTable.AppendFormat("{0}|   |", table.Model.Name);
                }

                soluteMarkdownTable.AppendLine();
                soluteMarkdownTable.Append("|");
                // Dividers for headings.
                bool isFirstSoluteTablePrinted = false;
                foreach (InitialConditionsTable table in soluteTables)
                {
                    if (!isFirstSoluteTablePrinted)
                    {
                        soluteMarkdownTable.AppendFormat("---|---:|---:|");
                        isFirstSoluteTablePrinted = true;
                    }
                    else
                        soluteMarkdownTable.AppendFormat("---:|---:|");
                }

                soluteMarkdownTable.AppendLine();
                if (!soluteTables.IsNullOrEmpty<InitialConditionsTable>())
                {
                    soluteMarkdownTable.Append("|**Depth(mm)**|");
                }

                // Value columns
                foreach (InitialConditionsTable table in soluteTables)
                {
                    IEnumerable<string> units = table.Conditions.Select(i => i.Units);
                    List<string> unitStrings = units.ToList();
                    if (unitStrings[1] == "ppm")
                        soluteMarkdownTable.Append($"**{unitStrings[1]}**|**kg/ha**|");
                    else
                        soluteMarkdownTable.Append($"**{unitStrings[1]}**|**ppm**|");
                }

                List<List<InitialCondition>> allInitialConditionsLists = new();

                // List for storing new condition value lists.
                List<List<string>> tempValueLists = new();
                foreach (InitialConditionsTable table in soluteTables)
                {
                    // Temp storage for each condition for allInitialConditionsLists.
                    List<InitialCondition> conditions = new List<InitialCondition>();
                    foreach (InitialCondition condition in table.Conditions)
                    {
                        string stringToBeList = condition.Value;
                        List<string> newConditionValueList = stringToBeList.Split(", ").ToList();
                        tempValueLists.Add(newConditionValueList);
                        conditions.Add(condition);
                    }
                    allInitialConditionsLists.Add(conditions);
                }

                // Print the values line-by-line for each condition.
                soluteMarkdownTable.AppendLine();
                // Gets the list length of one of the InitialCondition value lists.
                int valueCount = 0;
                if (tempValueLists.Count > 0)
                {
                    valueCount = tempValueLists[0].Count;
                }
                // Create a markdown table row for each value in the list.
                for (int i = 0; i < valueCount; i++)
                {
                    soluteMarkdownTable.Append("| ");
                    // Put the actual value in the markdown table.
                    bool depthPrinted = false;
                    foreach (List<string> valueList in tempValueLists)
                    {
                        double convertedValue = 0.0;
                        bool canConvert = double.TryParse(valueList[i], out convertedValue);
                        if (canConvert)
                            soluteMarkdownTable.AppendFormat("{0:F3}|", convertedValue.Round(3));
                        else if (!depthPrinted && !canConvert)
                        {
                            soluteMarkdownTable.AppendFormat("{0}|", valueList[i]);
                            depthPrinted = true;
                        }
                    }
                    soluteMarkdownTable.AppendLine();
                }
                markdown.Append(soluteMarkdownTable.ToString());

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
                    md.AppendLine();
                    md.AppendLine("```");

                    int spacing = m.Max(msg => msg.RelativePath.Length) + 2;
                    foreach (var msg in m)
                    {
                        if (previousPath == null || msg.RelativePath != previousPath)
                        {
                            md.Append($"{msg.RelativePath}:".PadRight(spacing));
                            previousPath = msg.RelativePath;
                        }
                        else
                            md.Append(' ', spacing);
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
                result = result.Where(m => m.Severity <= summaryView.MessagesFilter.SelectedEnumValue);

                return result;
            }
            return Enumerable.Empty<Message>();
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        /// <summary>
        /// Event handler for the view's copy event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            this.explorerPresenter.SetClipboardText(e.Text, "CLIPBOARD");
        }
    }
}