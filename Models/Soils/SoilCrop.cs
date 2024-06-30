using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>A soil crop parameterization class.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Physical))]
    public class SoilCrop : Model, IGridModel
    {
        /// <summary>Depth strings (mm/mm)</summary>
        [Summary]
        [Units("mm")]
        public string[] Depth => (Parent as Physical).Depth;

        /// <summary>Crop lower limit (mm/mm)</summary>
        [Summary]
        [Display(Format = "N3")]
        [Units("mm/mm")]
        public double[] LL { get; set; }

        /// <summary>Crop lower limit (mm)</summary>
        [Units("mm")]
        public double[] LLmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return MathUtilities.Multiply(LL, soilPhysical.Thickness);
            }
        }

        /// <summary>The KL value.</summary>
        [Summary]
        [Units("/day")]
        [Display(Format = "N3")]
        public double[] KL { get; set; }

        /// <summary>The exploration factor</summary>
        [Summary]
        [Units("0-1")]
        [Display(Format = "N1")]
        public double[] XF { get; set; }

        /// <summary>The metadata for crop lower limit</summary>
        public string[] LLMetadata { get; set; }

        /// <summary>The metadata for KL</summary>
        public string[] KLMetadata { get; set; }

        /// <summary>The meta data for the exploration factor</summary>
        public string[] XFMetadata { get; set; }

        /// <summary>Return the plant available water CAPACITY at standard thickness.</summary>
        [Units("mm/mm")]
        public double[] PAWC
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return SoilUtilities.CalcPAWC(soilPhysical.Thickness, LL, soilPhysical.DUL, XF);
            }
        }

        /// <summary>Return the plant available water CAPACITY at standard thickness.</summary>
        [Display(DisplayName = "PAWC", Format = "N1")]
        [Units("mm")]
        public double[] PAWCmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                return MathUtilities.Multiply(PAWC, soilPhysical.Thickness);
            }
        }

        /// <summary>Return the plant available water (SW-CLL).</summary>
        [Units("mm/mm")]
        public double[] PAW
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;
                var water = FindInScope<Water>();
                if (water == null)
                    return null;
                return SoilUtilities.CalcPAWC(soilPhysical.Thickness, LL, water.Volumetric, XF);
            }
        }

        /// <summary>Return the plant available water (SW-CLL) (mm).</summary>
        [Units("mm")]
        public double[] PAWmm
        {
            get
            {
                var soilPhysical = FindAncestor<IPhysical>();
                if (soilPhysical == null)
                    return null;

                return MathUtilities.Multiply(PAW, soilPhysical.Thickness);
            }
        }

        /// <summary>Tabular data. Called by GUI.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                //Do validation on the soilCrop to make that sure has the same amount of layers as the physical node
                //If not, we remove layers to make it match, or add new layers with 0 values
                //Then throw an exception back to the GUI to warn the user what has been changed.
                //This is used to allow users to copy soil from another simulation without it crashing.
                //It cannot detect if the layer count was the same, but the structure was different, as depth values
                //are not stored here.
                bool throwException = false;
                if (Depth.Length != LL.Length)
                {
                    throwException = true;

                    List<double> listLL = LL.ToList();
                    List<double> listKL = KL.ToList();
                    List<double> listXF = XF.ToList();

                    if (Depth.Length < LL.Length)
                    {
                        listLL = listLL.GetRange(0, Depth.Length);
                        listKL = listKL.GetRange(0, Depth.Length);
                        listXF = listXF.GetRange(0, Depth.Length);
                    }
                    else //Depth.Length > LL.Length
                    {
                        for (int i = LL.Length; i < Depth.Length; i++)
                        {
                            listLL.Add(0.0);
                            listKL.Add(0.0);
                            listXF.Add(0.0);
                        }
                    }

                    LL = listLL.ToArray();
                    KL = listKL.ToArray();
                    XF = listXF.ToArray();
                }

                GridTable tb = new GridTable(Name, new GridTableColumn[]
                {
                new GridTableColumn("Depth", new VariableProperty(Parent, Parent.GetType().GetProperty("Depth")), readOnly:true),
                new GridTableColumn("LL", new VariableProperty(this, GetType().GetProperty("LL"))),
                new GridTableColumn("KL", new VariableProperty(this, GetType().GetProperty("KL"))),
                new GridTableColumn("XF", new VariableProperty(this, GetType().GetProperty("XF"))),
                new GridTableColumn("PAWC", new VariableProperty(this, GetType().GetProperty("PAWCmm")), units: $"{PAWCmm.Sum():F1} mm")
                }, this);

                if (throwException)
                {
                    Exception e = new Exception("Soil Layers do not match on " + Name + ".\n" + Name + " has been modified to match Physical node.");
                    e.Data.Add("tableData", tb);
                    throw e;
                }
                else
                {
                    List<GridTable> tables = new List<GridTable>();
                    tables.Add(tb);
                    return tables;
                }
            }
        }
    }
}