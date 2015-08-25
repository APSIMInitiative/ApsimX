using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing water parameters
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(typeof(Soil))]
    public class Water : Model
    {
        /// <summary>Gets the soil.</summary>
        /// <value>The soil.</value>
        private Soil soil
        {
            get
            {
                return Apsim.Parent(this, typeof(Soil)) as Soil;
            }
        }

        /// <summary>The _ thickness</summary>
        private double[] _Thickness;

        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness
        {
            get
            {
                return _Thickness;
            }
            set
            {
                _Thickness = value;
            }
        }

        /// <summary>Gets or sets the depth.</summary>
        /// <value>The depth.</value>
        [Summary]
        [XmlIgnore]
        [Units("cm")]
        [Description("Depth")]
        public string[] Depth
        {
            get
            {
                return Soil.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = Soil.ToThickness(value);
            }
        }

        /// <summary>Gets or sets the bd.</summary>
        /// <value>The bd.</value>
        [Summary]
        [Description("BD")]
        [Units("g/cc")]
        [Display(Format = "N2")]
        public double[] BD { get; set; }

        /// <summary>Gets or sets the air dry.</summary>
        /// <value>The air dry.</value>
        [Summary]
        [Description("Air dry")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] AirDry { get; set; }

        /// <summary>Gets or sets the l L15.</summary>
        /// <value>The l L15.</value>
        [Summary]
        [Description("LL15")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] LL15 { get; set; }

        /// <summary>Gets or sets the dul.</summary>
        /// <value>The dul.</value>
        [Summary]
        [Description("DUL")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] DUL { get; set; }

        /// <summary>Gets or sets the sat.</summary>
        /// <value>The sat.</value>
        [Summary]
        [Description("SAT")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SAT { get; set; }

        /// <summary>Gets or sets the ks.</summary>
        /// <value>The ks.</value>
        [Summary]
        [Description("KS")]
        [Units("mm/day")]
        [Display(Format = "N1")]
        public double[] KS { get; set; }

        /// <summary>Gets or sets the bd metadata.</summary>
        /// <value>The bd metadata.</value>
        public string[] BDMetadata { get; set; }
        /// <summary>Gets or sets the air dry metadata.</summary>
        /// <value>The air dry metadata.</value>
        public string[] AirDryMetadata { get; set; }
        /// <summary>Gets or sets the l L15 metadata.</summary>
        /// <value>The l L15 metadata.</value>
        public string[] LL15Metadata { get; set; }
        /// <summary>Gets or sets the dul metadata.</summary>
        /// <value>The dul metadata.</value>
        public string[] DULMetadata { get; set; }
        /// <summary>Gets or sets the sat metadata.</summary>
        /// <value>The sat metadata.</value>
        public string[] SATMetadata { get; set; }
        /// <summary>Gets or sets the ks metadata.</summary>
        /// <value>The ks metadata.</value>
        public string[] KSMetadata { get; set; }

        /// <summary>Gets the crops.</summary>
        /// <value>The crops.</value>
        [Description("Soil crop parameterisations")]
        [XmlIgnore]
        public List<ISoilCrop> Crops
        {
            get
            {
                List<ISoilCrop> crops = new List<ISoilCrop>();
                foreach (ISoilCrop crop in Apsim.Children(this, typeof(ISoilCrop)))
                    crops.Add(crop);
                return crops;
            }
        }

        /// <summary>Return the specified crop to caller. Will return null if not found.</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
        public ISoilCrop Crop(string CropName)
        {
            return this.Children.Find(m => m.Name.Equals(CropName, StringComparison.CurrentCultureIgnoreCase)) as ISoilCrop;
        }

        /// <summary>
        /// Get or set the names of crops. Note: When setting, the crops will be reorded to match
        /// the setting list of names. Also new crops will be added / deleted as required.
        /// </summary>
        /// <value>The crop names.</value>
        [XmlIgnore]
        public string[] CropNames
        {
            get
            {
                string[] CropNames = new string[Crops.Count];
                for (int i = 0; i < Crops.Count; i++)
                    CropNames[i] = Crops[i].Name;
                return CropNames;
            }
            //set
            //{
            //    // Add in extra crops if necessary.
            //    for (int i = 0; i < value.Length; i++)
            //    {
            //        ISoilCrop Crop = FindCrop(value[i]);
            //        if (Crop == null)
            //        {
            //            Crop = new SoilCrop { Name = value[i] };
            //            Crops.Add(Crop);
            //        }

            //    }

            //    // Remove unwanted crops if necessary.
            //    for (int i = Crops.Count - 1; i >= 0; i--)
            //    {
            //        int Pos = StringUtilities.IndexOfCaseInsensitive(value, Crops[i].Name);
            //        if (Pos != -1 && value[Pos] != "")
            //            Crops[i].Name = value[Pos];  // ensure case transfer.
            //        else
            //            Crops.Remove(Crops[i]);      // remove unwanted crop.
            //    }

            //    // Now reorder.
            //    for (int i = 0, insert = 0; i < value.Length; i++)
            //    {
            //        int ExistingCropIndex = FindCropIndex(value[i]);
            //        if (ExistingCropIndex != -1)
            //        {
            //            if (insert != ExistingCropIndex)
            //            {
            //                SoilCrop C = Crops[ExistingCropIndex];  // grab the crop
            //                Crops.RemoveAt(ExistingCropIndex);      // remove it from the list.
            //                Crops.Insert(insert, C);                     // add it at the correct index.
            //            }
            //            insert++;
            //        }
            //    }

            //}
        }

        /// <summary>Return the specified crop to caller. Will return null if not found.</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
        private int FindCropIndex(string CropName)
        {
            for (int i = 0; i < Crops.Count; i++)
                if (Crops[i].Name.Equals(CropName, StringComparison.CurrentCultureIgnoreCase))
                    return i;
            return -1;
        }

    }
}
