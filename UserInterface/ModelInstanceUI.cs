using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SourceGrid.Cells;
using SourceGrid.Cells.Models;
using SourceGrid;

namespace ApsimUI
{
    public class ModelVariable : IValueModel
    {
        object _Value;
        public ModelVariable(object Value)
        {
            _Value = Value;
        }
        public object GetValue(CellContext CellContext)
        {
            if (_Value == null)
                return "";

            if (_Value.GetType().IsArray)
            {
                string ReturnString = "";
                foreach (object I in _Value as Array)
                {
                    if (ReturnString != "")
                        ReturnString += "\r\n";
                    ReturnString += I.ToString();
                }
                return ReturnString;
            }
            else
                return _Value.ToString();
        }

        public void SetValue(CellContext cellContext, object p_Value)
        {

        }
    }


    public partial class ModelInstanceUI : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ModelInstanceUI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Contorl is now loaded - populate controls.
        /// </summary>
        public void Populate(object _ModelInstance)
        {
            //Grid.RowsCount = _ModelInstance.Params.Count;
            //Grid.ColumnsCount = 2;
            //int Row = 0;
            //foreach (ClassVariable Param in _ModelInstance.Params)
            //{
            //    if (Param.Description != null)
            //        Grid[Row, 0] = new SourceGrid.Cells.Cell(Param.Description);
            //    else
            //        Grid[Row, 0] = new SourceGrid.Cells.Cell(Param.Name);
            //    Grid[Row, 1] = new SourceGrid.Cells.Cell();
            //    Grid[Row, 1].Model.ValueModel = new ModelVariable(Param.Value);
            //    Row++;
            //}
            //Grid.AutoSizeCells();
        }



    }




} // namespace
