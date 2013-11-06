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

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Model = model as Model;
            View = view as HtmlView;

            if (Model is ISummary)
            {
                ISummary summary = Model as ISummary;
                Utility.Configuration configuration = new Utility.Configuration();
                View.HTML = summary.GetHtml(configuration.SummaryPngFileName);
            }
            else
                View.HTML = Utility.Reflection.GetValueOfFieldOrProperty("HTML", model) as string;
        }

        public void Detach()
        {
            
        }
    }
}
