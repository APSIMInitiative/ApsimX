using APSIM.Shared.Utilities;
using Models.CLEM;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class CLEMPresenter : IPresenter
    {
        internal ICLEMView View;
        internal ICLEMUI ClemModel;
        internal IModel Model;

        /// <summary>
        /// The explorer
        /// </summary>
        internal ExplorerPresenter ExplorerPresenter;

        internal Dictionary<string, IPresenter> PresenterList = new Dictionary<string, IPresenter> ();

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public virtual void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.Model = model as IModel;
            this.ClemModel = model as ICLEMUI;
            this.ExplorerPresenter = explorerPresenter;

            this.View = view as ICLEMView;
            
            PresenterNameAttribute presenterName = null;

            if (model != null)
            {
                //Messages
                try
                {
                    if (model is ZoneCLEM)
                    {
                        object newView = new MarkdownView(this.View as ViewBase);
                        IPresenter messagePresenter = new MessagePresenter();
                        if (newView != null && messagePresenter != null)
                        {
                            this.View.AddTabView("Messages", newView);
                            messagePresenter.Attach(model, newView, this.ExplorerPresenter);
                            PresenterList.Add("Messages", messagePresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.ExplorerPresenter.MainPresenter.ShowError(err);
                }
                //Properties
                try
                {
                    // user can set view to state it must be multiview so do not override value
                    presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;
                    string propPresenterName = presenterName.ToString();
                    ViewNameAttribute viewAttribute = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                    string viewName = viewAttribute.ToString();
                    if (propPresenterName == "UserInterface.Presenters.PropertyPresenter" | !propPresenterName.Contains("Property"))
                    {
                        propPresenterName = "UserInterface.Presenters.PropertyPresenter";
                        if(viewName != "UserInterface.Views.PropertyMultiModelView")
                            viewName = "UserInterface.Views.PropertyView";
                    }

                    var props = model.GetType().GetProperties(
                        BindingFlags.Public |
                          BindingFlags.NonPublic |
                          BindingFlags.Instance
                          );

                    // check if any category attribtes other than "*" fould and if so make this a PropertyCategoryPresenter
                    bool categoryAttributeFound = props.Where(prop => prop.IsDefined(typeof(CategoryAttribute), false) && (prop.GetCustomAttribute(typeof(CategoryAttribute)) as CategoryAttribute).Category != "*").Any();
                    if (categoryAttributeFound)
                    {
                        propPresenterName = "UserInterface.Presenters.PropertyCategorisedPresenter";
                        viewName = "UserInterface.Views.PropertyCategorisedView";
                    }

                    // check if it has properties
                    if ((viewName.Contains("PropertyMultiModelView") | propPresenterName.Contains("MultiModel")) ||
                        (props.Where(prop => prop.IsDefined(typeof(DescriptionAttribute), false)).Count() > 0))
                    {
                        object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.View }, null, null);
                        IPresenter propertyPresenter = Assembly.GetExecutingAssembly().CreateInstance(propPresenterName) as IPresenter;
                        if (newView != null && propertyPresenter != null)
                        {
                            this.View.AddTabView("Properties", newView);
                            propertyPresenter.Attach(model, newView, this.ExplorerPresenter);
                            PresenterList.Add("Properties", propertyPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.ExplorerPresenter.MainPresenter.ShowError(err);
                }

                // if presenter is ICLEMPresenter then add the extra presenters if specified
                if (presenterName != null && typeof(ICLEMPresenter).IsAssignableFrom(Assembly.GetExecutingAssembly().GetType(presenterName.ToString())))
                    (Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as ICLEMPresenter).AttachExtraPresenters(this);

                //HTML Summary
                try
                {
                    object newView = new MarkdownView(this.View as ViewBase);
                    IPresenter summaryPresenter = new CLEMSummaryPresenter();
                    if (newView != null && summaryPresenter != null)
                    {
                        this.View.AddTabView("Summary", newView);
                        summaryPresenter.Attach(model, newView, this.ExplorerPresenter);
                        PresenterList.Add("Summary", summaryPresenter);
                    }
                }
                catch (Exception err)
                {
                    this.ExplorerPresenter.MainPresenter.ShowError(err);
                }
                //Versions
                try
                {
                    var versions = ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false);
                    if (versions.Count() > 0)
                    {
                        object newView = new MarkdownView(this.View as ViewBase);
                        IPresenter versionPresenter = new VersionsPresenter();
                        if (newView != null && versionPresenter != null)
                        {
                            this.View.AddTabView("Version", newView);
                            versionPresenter.Attach(model, newView, this.ExplorerPresenter);
                            PresenterList.Add("Version", versionPresenter);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.ExplorerPresenter.MainPresenter.ShowError(err);
                }

                this.View.TabSelected += OnTabSelected;

                if (ClemModel != null)
                {
                    this.View.SelectTabView(ClemModel.SelectedTab);
                    if(ClemModel.SelectedTab == "Summary")
                    {
                        PresenterList.TryGetValue("Summary", out IPresenter selectedPresenter);
                        (selectedPresenter as CLEMSummaryPresenter).Refresh();
                    }
                    else if (ClemModel.SelectedTab == "Messages")
                    {
                        PresenterList.TryGetValue("Messages", out IPresenter selectedPresenter);
                        (selectedPresenter as MessagePresenter).Refresh();
                    }

                }

                if (ClemModel != null && ClemModel.SelectedTab is null && PresenterList.Count > 0)
                    if (PresenterList.FirstOrDefault().Value is IRefreshPresenter)
                        (PresenterList.FirstOrDefault().Value as IRefreshPresenter).Refresh(); 
            }
        }

        /// <summary>Summary tab selected</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        internal void OnTabSelected(object sender, EventArgs e)
        {
            // change tab name
            if (ClemModel != null)
                ClemModel.SelectedTab = (e as TabChangedEventArgs).TabName;

            string tabName = (e as TabChangedEventArgs).TabName;
            PresenterList.TryGetValue(tabName, out IPresenter selectedPresenter);

            if (selectedPresenter != null)
                if (selectedPresenter is IRefreshPresenter)
                    (selectedPresenter as IRefreshPresenter).Refresh();
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            this.View.TabSelected -= OnTabSelected;
            foreach (KeyValuePair<string, IPresenter> valuePair in PresenterList)
                if(valuePair.Value != null)
                    valuePair.Value.Detach();
            (this.View as ViewBase).Dispose();
        }

    }
}
