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

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The HTML summary presenter
        /// </summary>
        private IPresenter summaryPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
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
                        IPresenter ip = new MessagePresenter();
                        if (newView != null && ip != null)
                        {
                            this.view.AddTabView("Messages", newView);
                            ip.Attach(model, newView, this.explorerPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    if (err is System.Reflection.TargetInvocationException)
                        err = (err as System.Reflection.TargetInvocationException).InnerException;
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
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
                        this.view.SummaryTabSelected += OnSummaryTabSelected; 
                    }
                }
                catch (Exception err)
                {
                    if (err is System.Reflection.TargetInvocationException)
                        err = (err as System.Reflection.TargetInvocationException).InnerException;
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
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
                        IPresenter ip = Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter;
                        if (newView != null && ip != null)
                        {
                            this.view.AddTabView("Properties", newView);
                            ip.Attach(model, newView, this.explorerPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    if (err is System.Reflection.TargetInvocationException)
                        err = (err as System.Reflection.TargetInvocationException).InnerException;
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }
                //Versions
                try
                {
                    var versions = ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false);
                    if (versions.Count() > 0)
                    {
                        object newView = new HTMLView(this.view as ViewBase);
                        IPresenter ip = new VersionsPresenter();
                        if (newView != null && ip != null)
                        {
                            this.view.AddTabView("Version", newView);
                            ip.Attach(model, newView, this.explorerPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    if (err is System.Reflection.TargetInvocationException)
                        err = (err as System.Reflection.TargetInvocationException).InnerException;
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }


            }
        }

        /// <summary>Summary tab selected</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        private void OnSummaryTabSelected(object sender, EventArgs e)
        {
            (summaryPresenter as CLEMSummaryPresenter).RefreshSummary();
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            this.view.SummaryTabSelected -= OnSummaryTabSelected;
        }

    }
}
