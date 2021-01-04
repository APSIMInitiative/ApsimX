using APSIM.Shared.Utilities;
using Models;
using Models.CLEM;
using Models.Core;
using Models.Core.Attributes;
using Models.Factorial;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UserInterface.EventArguments;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class ActivityLedgerPresenter: CLEMPresenter
    {
        private Report report;
        private IDataStore dataStore;
        private DataStorePresenter dataStorePresenter;
        private ViewBase reportView;
        private ActivityLedgerGridView ledgerView;
        private ActivityLedgerGridPresenter activityGridPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.report = model as Report;
            this.clemModel = model as ICLEMUI;
            IModel modelAttached = model as IModel;
            this.explorerPresenter = explorerPresenter;
            base.view = view as CLEMView;
            base.viewobject = view as IActivityLedgerGridView; //IReportActivityLedgerView;

            if (model != null)
            {
                //Properties
                try
                {
                    PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;
                    string[] childDisplayInParentPresenters = { "PropertyTablePresenter", "PropertyTreeTablePresenter" };
                    bool isTablePresenter = childDisplayInParentPresenters.Contains(presenterName.ToString().Split('.').Last());

                    // check if it has properties
                    if (isTablePresenter ||
                        (model.GetType().GetProperties(
                        BindingFlags.Public |
                          BindingFlags.NonPublic |
                          BindingFlags.Instance
                          ).Where(prop => prop.IsDefined(typeof(DescriptionAttribute), false)).Count() > 0))
                    {
                        ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                        object newView = new GridView(view as ViewBase); 
                        IPresenter propertyPresenter = new PropertyPresenter(); 
                        if (newView != null && propertyPresenter != null)
                        {
                            (view as ICLEMView).AddTabView("Properties", newView);
                            propertyPresenter.Attach(modelAttached, newView, this.explorerPresenter);
                            presenterList.Add("Properties", propertyPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                //UI Results
                try
                {
                    ledgerView = new ActivityLedgerGridView(view as ViewBase);
                    ReportView rv = new ReportView(view as ViewBase);
                    reportView = new ViewBase(rv, "ApsimNG.Resources.Glade.DataStoreView.glade");

                    this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

                    Simulations simulations = report.FindAncestor<Simulations>();
                    if (simulations != null)
                    {
                        dataStore = simulations.FindChild<IDataStore>();
                    }

                    dataStorePresenter = new DataStorePresenter();
                    activityGridPresenter = new ActivityLedgerGridPresenter();
                    Simulation simulation = report.FindAncestor<Simulation>();
                    Zone paddock = report.FindAncestor<Zone>();

                    if (paddock != null)
                        dataStorePresenter.ZoneFilter = paddock;
                    if (simulation != null)
                    {
                        if (simulation.Parent is Experiment)
                        {
                            dataStorePresenter.ExperimentFilter = simulation.Parent as Experiment;
                        }
                        else
                        {
                            dataStorePresenter.SimulationFilter = simulation;
                        }
                    }

                    dataStorePresenter.Attach(dataStore, reportView, explorerPresenter);
                    activityGridPresenter.ModelReport = this.report;
                    activityGridPresenter.ModelName = this.report.Name;
                    activityGridPresenter.SimulationName = simulation.Name;
                    activityGridPresenter.ZoneName = paddock.Name;
                    activityGridPresenter.Attach(dataStore, ledgerView, explorerPresenter);
                    dataStorePresenter.tableDropDown.SelectedValue = this.report.Name;

                    (view as CLEMView).AddTabView("Display", ledgerView);
                    presenterList.Add("Display", activityGridPresenter);

                    (view as CLEMView).AddTabView("Data", reportView);
                    presenterList.Add("Data", dataStorePresenter);
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                //HTML Summary
                try
                {
                    object newView = new MarkdownView(this.view as ViewBase);
                    IPresenter summaryPresenter = new CLEMSummaryPresenter();
                    if (newView != null && summaryPresenter != null)
                    {
                        (view as CLEMView).AddTabView("Summary", newView);
                        summaryPresenter.Attach(modelAttached, newView, this.explorerPresenter);
                        presenterList.Add("Summary", summaryPresenter);
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                //Versions
                try
                {
                    var versions = ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false);
                    if (versions.Count() > 0)
                    {
                        object newView = new MarkdownView(this.view as ViewBase);
                        IPresenter versionPresenter = new VersionsPresenter();
                        if (newView != null && versionPresenter != null)
                        {
                            (view as CLEMView).AddTabView("Version", newView);
                            versionPresenter.Attach(modelAttached, newView, this.explorerPresenter);
                            presenterList.Add("Version", versionPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                base.view.TabSelected += base.OnTabSelected;

                if (clemModel != null)
                {
                    (view as CLEMView).SelectTabView(clemModel.SelectedTab);
                    if (clemModel.SelectedTab == "Summary")
                    {
                        presenterList.TryGetValue("Summary", out IPresenter selectedPresenter);
                        (selectedPresenter as CLEMSummaryPresenter).Refresh();
                    }
                }
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter.Detach();
            activityGridPresenter.Detach();
            base.Detach();
        }

        /// <summary>The view is asking for variable names.</summary>
        void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, true, true, false));
        }

        /// <summary>The view is asking for event names.</summary>
        void OnNeedEventNames(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, false, false, true));
        }

        /// <summary>The variable names have changed in the view.</summary>
        void OnVariableNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.ModelChanged += new ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The event names have changed in the view.</summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.ModelChanged += new ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The model has changed so update our view.</summary>
        void OnModelChanged(object changedModel)
        {
        }

    }
}
