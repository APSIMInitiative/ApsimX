using APSIM.Shared.Utilities;
using Models.CLEM;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class CLEMPresenter : IPresenter
    {
        internal ICLEMView view;
        internal object viewobject;
        internal ICLEMUI clemModel;

        /// <summary>
        /// The explorer
        /// </summary>
        internal ExplorerPresenter explorerPresenter;

        internal Dictionary<string, IPresenter> presenterList = new Dictionary<string, IPresenter> ();

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public virtual void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.clemModel = model as ICLEMUI;
            this.explorerPresenter = explorerPresenter;

            this.view = view as ICLEMView;
            this.viewobject = view;

            if (model != null)
            {
                //Messages
                try
                {
                    if (model is ZoneCLEM)
                    {
                        object newView = new MarkdownView(this.view as ViewBase);
                        IPresenter messagePresenter = new MessagePresenter();
                        if (newView != null && messagePresenter != null)
                        {
                            this.view.AddTabView("Messages", newView);
                            messagePresenter.Attach(model, newView, this.explorerPresenter);
                            presenterList.Add("Messages", messagePresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }
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
                        object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName.ToString(), false, BindingFlags.Default, null, new object[] { this.view }, null, null);
                        IPresenter propertyPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter;
                        if (newView != null && propertyPresenter != null)
                        {
                            this.view.AddTabView("Properties", newView);
                            propertyPresenter.Attach(model, newView, this.explorerPresenter);
                            presenterList.Add("Properties", propertyPresenter);
                        }
                    }
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
                        this.view.AddTabView("Summary", newView);
                        summaryPresenter.Attach(model, newView, this.explorerPresenter);
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
                            this.view.AddTabView("Version", newView);
                            versionPresenter.Attach(model, newView, this.explorerPresenter);
                            presenterList.Add("Version", versionPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                this.view.TabSelected += OnTabSelected;

                if (clemModel != null)
                {
                    this.view.SelectTabView(clemModel.SelectedTab);
                    if(clemModel.SelectedTab == "Summary")
                    {
                        presenterList.TryGetValue("Summary", out IPresenter selectedPresenter);
                        (selectedPresenter as CLEMSummaryPresenter).Refresh();
                    }
                }
            }
        }

        /// <summary>Summary tab selected</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        internal void OnTabSelected(object sender, EventArgs e)
        {
            // change tab name
            if (clemModel != null)
            {
                clemModel.SelectedTab = (e as TabChangedEventArgs).TabName;
            }

            string tabName = (e as TabChangedEventArgs).TabName;
            presenterList.TryGetValue(tabName, out IPresenter selectedPresenter);

            if (selectedPresenter != null)
            {
                switch ((e as TabChangedEventArgs).TabName)
                {
                    case "Messages":
                        (selectedPresenter as MessagePresenter).Refresh();
                        break;
                    case "Summary":
                        (selectedPresenter as CLEMSummaryPresenter).Refresh();
                        break;
                    case "Version":
                        (selectedPresenter as VersionsPresenter).Refresh();
                        break;
                    case "Display":
                        (selectedPresenter as ActivityLedgerGridPresenter).Refresh();
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            this.view.TabSelected -= OnTabSelected;

            foreach (KeyValuePair<string, IPresenter> valuePair in presenterList)
            {
                if(valuePair.Value != null)
                {
                    valuePair.Value.Detach();
                }
            }
        }

    }
}
