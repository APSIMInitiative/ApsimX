namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.CLEM;
    using Models.CLEM.Reporting;
    using Models.Core;
    using Models.Core.Attributes;
    using Views;

    public class ModelDetailsWrapperPresenter : IPresenter
    {
        private ExplorerPresenter explorerPresenter;

        private IModelDetailsWrapperView view;

        /// <summary>Gets or sets the APSIMX simulations object</summary>
        public Simulations ApsimXFile { get; set; }

        /// <summary>Presenter for the component</summary>
        private IPresenter currentLowerPresenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.ApsimXFile = model as Simulations;
            this.explorerPresenter = explorerPresenter;
            this.view = view as IModelDetailsWrapperView;

            if (model != null)
            {
                ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;

                this.view.ModelTypeText = model.GetType().ToString()["Models.".Length..];
                DescriptionAttribute descAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descAtt != null)
                    this.view.ModelDescriptionText = descAtt.ToString();
                else
                    this.view.ModelDescriptionText = "";

                // Set CLEM specific colours for title
                if (this.view.ModelTypeText.Contains(".Resources."))
                {
                    this.view.ModelTypeTextColour = "996633";
                    this.view.ModelTypeTextStyle = "resource";
                }
                else if (this.view.ModelTypeText.Contains(".Activities.LabourRequirement"))
                {
                    this.view.ModelTypeTextColour = "cc33cc";
                    this.view.ModelTypeTextStyle = "labourrequirement";
                }
                else if (this.view.ModelTypeText.Contains(".Activities."))
                {
                    this.view.ModelTypeTextColour = "009999";
                    this.view.ModelTypeTextStyle = "activity";
                }
                else if (this.view.ModelTypeText.Contains(".Groupings."))
                {
                    this.view.ModelTypeTextColour = "cc33cc";
                    this.view.ModelTypeTextStyle = "grouping";
                }
                else if (this.view.ModelTypeText.Contains(".File"))
                {
                    this.view.ModelTypeTextColour = "008000";
                    this.view.ModelTypeTextStyle = "file";
                }
                else if (this.view.ModelTypeText.Contains(".Market"))
                {
                    this.view.ModelTypeTextColour = "1785FF";
                    this.view.ModelTypeTextStyle = "market";
                }

                HelpUriAttribute helpAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(HelpUriAttribute), false) as HelpUriAttribute;
                this.view.ModelHelpURL = "";
                if (helpAtt!=null)
                    this.view.ModelHelpURL = helpAtt.ToString();

                var vs = ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false);
                if (vs.Count() > 0)
                {
                    VersionAttribute verAtt = vs.ToList<Attribute>().Cast<VersionAttribute>().OrderBy(a => a.ToString()).Last() as VersionAttribute;
                    if (verAtt != null)
                    {
                        string v = "Version ";
                        v += verAtt.ToString();
                        this.view.ModelVersionText = v;
                    }
                    else
                        this.view.ModelVersionText = "";
                }

                if (viewName != null && presenterName != null)
                    // if model CLEMModel
                    if(model.GetType().IsSubclassOf(typeof(CLEMModel)) | model is ZoneCLEM | model is Market | model is RandomNumberGenerator | model is ReportResourceLedger)
                        // all CLEMModels will handle this presenter
                        ShowInLowerPanel(model, "UserInterface.Views.CLEMView", "UserInterface.Presenters.CLEMPresenter");
                    else if (typeof(ICLEMPresenter).IsAssignableFrom(Assembly.GetExecutingAssembly().GetType(presenterName.ToString())))
                        // apply this if the presenter has ICLEMPresenter interface and is ready to create presenters
                        ShowInLowerPanel(model, "UserInterface.Views.CLEMView", "UserInterface.Presenters.CLEMPresenter");
                    else
                        ShowInLowerPanel(model, viewName.ToString(), presenterName.ToString());
            }
        }

        /// <summary>Show a view in the right hand panel.</summary>
        /// <param name="model">The model.</param>
        /// <param name="viewName">The view name.</param>
        /// <param name="presenterName">The presenter name.</param>
        public void ShowInLowerPanel(object model, string viewName, string presenterName)
        {
            try
            {
                object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.view }, null, null); 
                this.currentLowerPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName) as IPresenter;
                if (newView != null && this.currentLowerPresenter != null)
                {
                    this.view.AddLowerView(newView);

                    // Resolve links in presenter.
                    explorerPresenter.ApsimXFile.Links.Resolve(currentLowerPresenter);
                    this.currentLowerPresenter.Attach(model, newView, explorerPresenter);
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        public void Detach()
        {
            if (currentLowerPresenter != null)
                currentLowerPresenter.Detach();
            return;
        }
    }
}
