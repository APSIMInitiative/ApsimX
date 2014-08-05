using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class Water : Model
    {
        private Soil soil
        {
            get
            {
                return this.ParentOfType(typeof(Soil)) as Soil;
            }
        }

        private double[] _Thickness;

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

        [Summary]
        [Description("BD")]
        [Units("g/cc")]
        [Display(Format = "N2")]
        public double[] BD { get; set; }

        [Summary]
        [Description("Air dry")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] AirDry { get; set; }

        [Summary]
        [Description("LL15")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] LL15 { get; set; }

        [Summary]
        [Description("DUL")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] DUL { get; set; }

        [Summary]
        [Description("SAT")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SAT { get; set; }

        [Summary]
        [Description("KS")]
        [Units("mm/day")]
        [Display(Format = "N1")]
        public double[] KS { get; set; }

        public string[] BDMetadata { get; set; }
        public string[] AirDryMetadata { get; set; }
        public string[] LL15Metadata { get; set; }
        public string[] DULMetadata { get; set; }
        public string[] SATMetadata { get; set; }
        public string[] KSMetadata { get; set; }

        [Description("Soil crop parameterisations")]
        [XmlElement("SoilCrop")]
        public List<SoilCrop> Crops { get; set; }

        public override void OnLoaded()
        {
            foreach (SoilCrop crop in Crops)
                crop.Soil = Parent as Soil;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Water()
        {
            Crops = new List<SoilCrop>();
        }

        /// <summary>
        /// Return the specified crop to caller. Will return null if not found.
        /// </summary>
        public SoilCrop Crop(string CropName)
        {
            SoilCrop C = FindCrop(CropName);
            if (C == null)
                return null;
            else
                return C;
        }

        /// <summary>
        /// Get or set the names of crops. Note: When setting, the crops will be reorded to match
        /// the setting list of names. Also new crops will be added / deleted as required.
        /// </summary>
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
            set
            {
                // Add in extra crops if necessary.
                for (int i = 0; i < value.Length; i++)
                {
                    SoilCrop Crop = FindCrop(value[i]);
                    if (Crop == null)
                    {
                        Crop = new SoilCrop { Name = value[i] };
                        Crops.Add(Crop);
                    }

                }

                // Remove unwanted crops if necessary.
                for (int i = Crops.Count - 1; i >= 0; i--)
                {
                    int Pos = Utility.String.IndexOfCaseInsensitive(value, Crops[i].Name);
                    if (Pos != -1 && value[Pos] != "")
                        Crops[i].Name = value[Pos];  // ensure case transfer.
                    else
                        Crops.Remove(Crops[i]);      // remove unwanted crop.
                }

                // Now reorder.
                for (int i = 0, insert = 0; i < value.Length; i++)
                {
                    int ExistingCropIndex = FindCropIndex(value[i]);
                    if (ExistingCropIndex != -1)
                    {
                        if (insert != ExistingCropIndex)
                        {
                            SoilCrop C = Crops[ExistingCropIndex];  // grab the crop
                            Crops.RemoveAt(ExistingCropIndex);      // remove it from the list.
                            Crops.Insert(insert, C);                     // add it at the correct index.
                        }
                        insert++;
                    }
                }

            }
        }

        /// <summary>
        /// Return the specified crop to caller. Will return null if not found.
        /// </summary>
        private SoilCrop FindCrop(string CropName)
        {
            foreach (SoilCrop Crop in Crops)
                if (Crop.Name.Equals(CropName, StringComparison.CurrentCultureIgnoreCase))
                    return Crop;
            return null;
        }

        /// <summary>
        /// Return the specified crop to caller. Will return null if not found.
        /// </summary>
        private int FindCropIndex(string CropName)
        {
            for (int i = 0; i < Crops.Count; i++)
                if (Crops[i].Name.Equals(CropName, StringComparison.CurrentCultureIgnoreCase))
                    return i;
            return -1;
        }

    }
}
