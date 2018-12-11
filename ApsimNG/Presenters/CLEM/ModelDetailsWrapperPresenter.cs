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
    using Importer;
    using Interfaces;
    using Models;
    using Models.CLEM;
    using Models.Core;
    using Models.Core.Attributes;
    using Views;

    public class ModelDetailsWrapperPresenter : IPresenter
    {
        private ExplorerPresenter ExplorerPresenter;

        private IModelDetailsWrapperView View;

        /// <summary>Gets or sets the APSIMX simulations object</summary>
        public Simulations ApsimXFile { get; set; }

        /// <summary>Presenter for the component</summary>
        private IPresenter currentLowerPresenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.ApsimXFile = model as Simulations;
            this.ExplorerPresenter = explorerPresenter;
            this.View = view as IModelDetailsWrapperView;

            if (model != null)
            {
                ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;

                View.ModelTypeText = model.GetType().ToString().Substring("Models.".Length);
                DescriptionAttribute descAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descAtt != null)
                {
                    View.ModelDescriptionText = descAtt.ToString();
                }
                else
                {
                    View.ModelDescriptionText = "";
                }
                // Set CLEM specific colours for title
                if (View.ModelTypeText.Contains(".Resources."))
                {
                    View.ModelTypeTextColour = "996633";
                }
                else if (View.ModelTypeText.Contains("Activities."))
                {
                    View.ModelTypeTextColour = "009999";
                }

                HelpUriAttribute helpAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(HelpUriAttribute), false) as HelpUriAttribute;
                View.ModelHelpURL = "";
                if (helpAtt!=null)
                {
                    View.ModelHelpURL = helpAtt.ToString();
                }

                var vs = ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false);
                if (vs.Count() > 0)
                {
                    VersionAttribute verAtt = vs.ToList<Attribute>().Last() as VersionAttribute;
                    if (verAtt != null)
                    {
                        string v = "Version ";
                        v += verAtt.ToString();
                        View.ModelVersionText = v;
                    }
                    else
                    {
                        View.ModelVersionText = "";
                    }
                }

                if (viewName != null && presenterName != null)
                {
                    // if model CLEMModel
                    if(model.GetType().IsSubclassOf(typeof(CLEMModel)) || model is ZoneCLEM)
                    {
                        ShowInLowerPanel(model, "UserInterface.Views.CLEMView", "UserInterface.Presenters.CLEMPresenter");
                    }
                    else
                    {
                        ShowInLowerPanel(model, viewName.ToString(), presenterName.ToString());
                    }
                }
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
                object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.View }, null, null); 
                this.currentLowerPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName) as IPresenter;
                if (newView != null && this.currentLowerPresenter != null)
                {
                    this.View.AddLowerView(newView);
                    this.currentLowerPresenter.Attach(model, newView, ExplorerPresenter);
                    // Resolve links in presenter.
                    // Not sure if this is needed AL
                    // ApsimXFile.Links.Resolve(currentLowerPresenter);
                }
            }
            catch (Exception err)
            {
                ExplorerPresenter.MainPresenter.ShowError(err);
            }
        }

        public void Detach()
        {
            if (currentLowerPresenter != null)
            {
                currentLowerPresenter.Detach();
            }

            return;
        }
    }
}
