using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class WFMasterPresenter : IPresenter
    {
        private ExplorerPresenter ExplorerPresenter;

        private IWFMasterView View;

        /// <summary>Gets or sets the APSIMX simulations object</summary>
        public Simulations ApsimXFile { get; set; }

        /// <summary>Presenter for the component</summary>
        private IPresenter currentLowerPresenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.ExplorerPresenter = explorerPresenter;
            this.View = view as IWFMasterView;

            if (model != null)
            {
                ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;

                View.ModelTypeText = model.GetType().ToString().Substring("Models.WholeFarm.".Length);
                DescriptionAttribute descAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descAtt != null)
                {
                    View.ModelDescriptionText = descAtt.ToString();
                }
                else
                {
                    View.ModelDescriptionText = "";
                }
                View.ModelHelpURL = "http://CLEMHelp.csiro.au/" + View.ModelTypeText + ".html";

                if(View.ModelTypeText.Contains("Resources."))
                {
                    View.ModelTypeTextColour = Color.FromArgb(153, 102, 51);
                }
                else if (View.ModelTypeText.Contains("Activities."))
                {
                    View.ModelTypeTextColour = Color.FromArgb(0, 153, 153);
                }
                else
                {
                    View.ModelTypeTextColour = Color.Black;
                }

                if (viewName != null && presenterName != null)
                {
                    ShowInLowerPanel(model, viewName.ToString(), presenterName.ToString());
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
                object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName);
                this.currentLowerPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName) as IPresenter;
                if (newView != null && this.currentLowerPresenter != null)
                {
                    // Resolve links in presenter.
//                    ApsimXFile.Links.Resolve(currentLowerPresenter);
                    this.View.AddLowerView(newView);
                    this.currentLowerPresenter.Attach(model, newView, ExplorerPresenter);
                }
            }
            catch (Exception err)
            {
                if (err is System.Reflection.TargetInvocationException)
                    err = (err as System.Reflection.TargetInvocationException).InnerException;
                string message = err.Message;
                message += "\r\n" + err.StackTrace;
//                MainPresenter.ShowMessage(message, Simulation.ErrorLevel.Error);
            }
        }


        public void Detach()
        {
            return;
        }
    }
}
