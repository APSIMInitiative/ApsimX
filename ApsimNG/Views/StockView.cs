// -----------------------------------------------------------------------
// <copyright file="StockView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Gtk;
    using Interfaces;
    using Models.GrazPlan;   // For access to the TSuppAttribute enumeration

    public class StockView : ViewBase, IStockView
    {

        public StockView(ViewBase owner) : base(owner)
        {

        }
    }
}
