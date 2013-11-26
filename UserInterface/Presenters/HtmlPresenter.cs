using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using UserInterface.Views;
using System.IO;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter class for working with HtmlView
    /// </summary>
    public class HtmlPresenter : IPresenter
    {
        private Model Model;
        private HtmlView View;
        private bool html;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Model = model as Model;
            View = view as HtmlView;

            html = Environment.OSVersion.Platform == PlatformID.Win32NT ||
                   Environment.OSVersion.Platform == PlatformID.Win32Windows;

            if (Model is ISummary)
            {
                ISummary summary = Model as ISummary;
                Utility.Configuration configuration = new Utility.Configuration();
                string contents = summary.GetSummary(configuration.SummaryPngFileName, html);
                View.SetSummary(contents, html);
            }
            else
                View.SetSummary(Utility.Reflection.GetValueOfFieldOrProperty("HTML", model) as string, true);
        }

        public void Detach()
        {
            
        }
    }
}
