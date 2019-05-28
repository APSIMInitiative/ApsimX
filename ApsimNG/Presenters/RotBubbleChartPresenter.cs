// -----------------------------------------------------------------------
// <copyright file="ManagerPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Views;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ICSharpCode.NRefactory.CSharp;

    /// <summary>
    /// Presenter for the rotation bubble chart component
    /// </summary>
    public class RotBubbleChartPresenter : IPresenter
    {
        /// <summary>
        /// The view for the manager
        /// </summary>
        private Interfaces.IRotBubbleChartView chartView;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="presenter">The explorer presenter being used</param>
        public void Attach(object model, object view, ExplorerPresenter presenter)
        {
            chartView = view as Interfaces.IRotBubbleChartView;
            //explorerPresenter = presenter;
            //intellisense = new IntellisensePresenter(managerView as ViewBase);
            //intellisense.ItemSelected += OnIntellisenseItemSelected;
        }
        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            
            //explorerPresenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
            //intellisense.ItemSelected -= OnIntellisenseItemSelected;
            //intellisense.Cleanup();
        }



    }
}

