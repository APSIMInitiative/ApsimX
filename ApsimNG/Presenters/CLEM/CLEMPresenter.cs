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
        private ICLEMView view;
        private ICLEMUI clemModel;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The HTML summary presenter
        /// </summary>
        private IPresenter summaryPresenter;

        /// <summary>
        /// The message presenter
        /// </summary>
        private IPresenter messagePresenter;

        /// <summary>
        /// The property presenter
        /// </summary>
        private IPresenter propertyPresenter;

        /// <summary>
        /// The version presenter
        /// </summary>
        private IPresenter versionPresenter;


        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.clemModel = model as ICLEMUI;
            this.explorerPresenter = explorerPresenter;

            this.view = view as ICLEMView;
            if (model != null)
            {
                //Messages
                try
                {
                    if (model is ZoneCLEM)
                    {
                        object newView = new HTMLView(this.view as ViewBase);
                        messagePresenter = new MessagePresenter();
                        if (newView != null && messagePresenter != null)
                        {
                            this.view.AddTabView("Messages", newView);
                            messagePresenter.Attach(model, newView, this.explorerPresenter);
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
                    object newView = new HTMLView(this.view as ViewBase);
                    summaryPresenter = new CLEMSummaryPresenter();
                    if (newView != null && summaryPresenter != null)
                    {
                        this.view.AddTabView("Summary", newView);
                        summaryPresenter.Attach(model, newView, this.explorerPresenter);
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
                        propertyPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter;
                        if (newView != null && propertyPresenter != null)
                        {
                            this.view.AddTabView("Properties", newView);
                            propertyPresenter.Attach(model, newView, this.explorerPresenter);
                        }
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
                        object newView = new HTMLView(this.view as ViewBase);
                        versionPresenter = new VersionsPresenter();
                        if (newView != null && versionPresenter != null)
                        {
                            this.view.AddTabView("Version", newView);
                            versionPresenter.Attach(model, newView, this.explorerPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }

                if (clemModel != null)
                {
                    this.view.SelectTabView(clemModel.SelectedTab);
                }
                this.view.TabSelected += OnTabSelected;
            }
        }

        /// <summary>Summary tab selected</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        private void OnTabSelected(object sender, EventArgs e)
        {
            // change tab name
            if (clemModel != null)
            {
                clemModel.SelectedTab = (e as TabChangedEventArgs).TabName;
            }

            if((e as TabChangedEventArgs).TabName == "Summary")
            {
            (summaryPresenter as CLEMSummaryPresenter).RefreshSummary();
            }
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            this.view.TabSelected -= OnTabSelected;
            if(propertyPresenter!=null)
            {
                propertyPresenter.Detach();
            }
            if (versionPresenter != null)
            {
                versionPresenter.Detach();
            }
            if (messagePresenter != null)
            {
                messagePresenter.Detach();
            }
            if (summaryPresenter != null)
            {
                summaryPresenter.Detach();
            }

        }

    }
}
