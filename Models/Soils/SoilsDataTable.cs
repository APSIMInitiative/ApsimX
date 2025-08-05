using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.WaterModel;

namespace Models.Soils
{

    /// <summary>
    /// A generic class for turning a DataTable of soil information into
    /// a list of sois.
    /// </summary>
    public class SoilsDataTable
    {
        /// <summary>Convert a table of soils data into a list of soils.</summary>
        public static List<Soil> ToSoils(DataTable table)
        {
            var soils = new List<Soil>();

            // Loop through all blocks of rows in datatable, create a
            // soil and store soil in correct location in the AllSoils XML.
            int row = 0;
            while (row < table.Rows.Count)
            {
                // Find the end of this soil i.e. the row that has a different value for 'Name'
                // to the current row.
                int endRow = row + 1;
                while (endRow < table.Rows.Count &&
                       table.Rows[endRow]["Name"].ToString() == table.Rows[row]["Name"].ToString())
                    endRow++;
                int numLayers = endRow - row;

                var soil = new Soil();
                soil.Name = table.Rows[row]["Name"].ToString();
                soil.Country = GetStringValue(table, row, "Country");
                soil.State = GetStringValue(table, row, "State");
                soil.Region = GetStringValue(table, row, "Region");
                soil.NearestTown = GetStringValue(table, row, "NearestTown");
                soil.Site = GetStringValue(table, row, "Site");
                soil.ApsoilNumber = GetStringValue(table, row, "APSoilNumber");
                soil.SoilType = GetStringValue(table, row, "Texture");
                soil.LocalName = GetStringValue(table, row, "LocalName");
                soil.ASCOrder = GetStringValue(table, row, "ASC_Order");
                soil.ASCSubOrder = GetStringValue(table, row, "ASC_Sub-order");
                soil.Latitude = GetDoubleValue(table, row, "Latitude");
                soil.Longitude = GetDoubleValue(table, row, "Longitude");
                soil.LocationAccuracy = GetStringValue(table, row, "LocationAccuracy");
                soil.YearOfSampling = GetStringValue(table, row, "YearOfSampling");
                soil.DataSource = GetStringValue(table, row, "DataSource");
                soil.Comments = GetStringValue(table, row, "Comments");
                soil.NaturalVegetation = GetStringValue(table, row, "NaturalVegetation");
                soil.RecordNumber = GetIntegerValue(table, row, "RecordNo");

                var physical = new Physical();
                soil.Children.Add(physical);
                physical.Thickness = MathUtilities.RemoveMissingValuesFromBottom(GetDoubleValues(table, "Thickness (mm)", row, numLayers));
                physical.BD = GetDoubleValues(table, "BD", row, numLayers);
                physical.BDMetadata = GetCodeValues(table, "BDCode", row, numLayers);
                physical.SAT = GetDoubleValues(table, "SAT (mm/mm)", row, numLayers);
                physical.SATMetadata = GetCodeValues(table, "SATCode", row, numLayers);
                physical.DUL = GetDoubleValues(table, "DUL (mm/mm)", row, numLayers);
                physical.DULMetadata = GetCodeValues(table, "DULCode", row, numLayers);
                physical.LL15 = GetDoubleValues(table, "LL15 (mm/mm)", row, numLayers);
                physical.LL15Metadata = GetCodeValues(table, "LL15Code", row, numLayers);
                physical.AirDry = GetDoubleValues(table, "Airdry (mm/mm)", row, numLayers);
                physical.AirDryMetadata = GetCodeValues(table, "AirdryCode", row, numLayers);
                physical.KS = GetDoubleValues(table, "KS (mm/day)", row, numLayers);
                physical.KSMetadata = GetCodeValues(table, "KSCode", row, numLayers);
                physical.Rocks = GetDoubleValues(table, "Rocks (%)", row, numLayers);
                physical.RocksMetadata = GetCodeValues(table, "RocksCode", row, numLayers);
                physical.Texture = GetStringValues(table, "Texture", row, numLayers);
                physical.TextureMetadata = GetCodeValues(table, "TextureCode", row, numLayers);
                physical.ParticleSizeSand = GetDoubleValues(table, "ParticleSizeSand (%)", row, numLayers);
                physical.ParticleSizeSandMetadata = GetCodeValues(table, "ParticleSizeSandCode", row, numLayers);
                physical.ParticleSizeSilt = GetDoubleValues(table, "ParticleSizeSilt (%)", row, numLayers);
                physical.ParticleSizeSiltMetadata = GetCodeValues(table, "ParticleSizeSiltCode", row, numLayers);
                physical.ParticleSizeClay = GetDoubleValues(table, "ParticleSizeClay (%)", row, numLayers);
                physical.ParticleSizeClayMetadata = GetCodeValues(table, "ParticleSizeClayCode", row, numLayers);

                var soilWater = new WaterBalance();
                soilWater.ResourceName = "WaterBalance";
                soil.Children.Add(soilWater);
                soilWater.Thickness = physical.Thickness;
                soilWater.SummerU = GetDoubleValue(table, row, "SummerU");
                soilWater.SummerCona = GetDoubleValue(table, row, "SummerCona");
                soilWater.WinterU = GetDoubleValue(table, row, "WinterU");
                soilWater.WinterCona = GetDoubleValue(table, row, "WinterCona");
                soilWater.SummerDate = GetStringValue(table, row, "SummerDate");
                soilWater.WinterDate = GetStringValue(table, row, "WinterDate");
                soilWater.Salb = GetDoubleValue(table, row, "Salb");
                soilWater.DiffusConst = GetDoubleValue(table, row, "DiffusConst");
                soilWater.DiffusSlope = GetDoubleValue(table, row, "DiffusSlope");
                soilWater.CN2Bare = GetDoubleValue(table, row, "Cn2Bare");
                soilWater.CNRed = GetDoubleValue(table, row, "CnRed");
                soilWater.CNCov = GetDoubleValue(table, row, "CnCov");
                soilWater.SWCON = GetDoubleValues(table, "SWCON (0-1)", row, numLayers);

                var organic = new Organic();
                soil.Children.Add(organic);
                organic.Thickness = physical.Thickness;
                organic.FOMCNRatio = GetDoubleValue(table, row, "RootCN");
                organic.FOM = MathUtilities.CreateArrayOfValues(GetDoubleValue(table, row, "RootWt"), numLayers);
                organic.SoilCNRatio = GetDoubleValues(table, "SoilCN", row, numLayers);
                organic.FBiom = GetDoubleValues(table, "FBIOM (0-1)", row, numLayers);
                organic.FInert = GetDoubleValues(table, "FINERT (0-1)", row, numLayers);
                organic.Carbon = GetDoubleValues(table, "OC", row, numLayers);
                organic.CarbonMetadata = GetCodeValues(table, "OCCode", row, numLayers);

                var chemical = new Chemical();
                soil.Children.Add(chemical);
                chemical.Thickness = physical.Thickness;
                chemical.EC = GetDoubleValues(table, "EC (1:5 dS/m)", row, numLayers);
                chemical.ECMetadata = GetCodeValues(table, "ECCode", row, numLayers);
                chemical.PH = GetDoubleValues(table, "PH", row, numLayers);
                chemical.PHMetadata = GetCodeValues(table, "PHCode", row, numLayers);
                chemical.ESP = GetDoubleValues(table, "ESP (%)", row, numLayers);
                chemical.ESPMetadata = GetCodeValues(table, "ESPCode", row, numLayers);

                var solute = new Solute();
                solute.Thickness = physical.Thickness;
                solute.InitialValues = GetDoubleValues(table, "CL (mg/kg)", row, numLayers);
                solute.InitialValuesUnits = Solute.UnitsEnum.ppm;
                soil.Children.Add(solute);

                // Add in some necessary models.
                var soilTemp = new CERESSoilTemperature();
                soilTemp.Name = "Temperature";
                soil.Children.Add(soilTemp);
                var nutrient = new Nutrients.Nutrient();
                nutrient.ResourceName = "Nutrient";
                soil.Children.Add(nutrient);
                var initialWater = new Water();
                soil.Children.Add(initialWater);

                // crops
                foreach (DataColumn Col in table.Columns)
                {
                    if (Col.ColumnName.ToLower().Contains(" ll"))
                    {
                        var nameBits = Col.ColumnName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (nameBits.Length == 3)
                        {
                            string cropName = nameBits[0];
                            SoilCrop crop = new SoilCrop();
                            crop.Name = cropName + "Soil";
                            crop.LL = GetDoubleValues(table, cropName + " ll (mm/mm)", row, numLayers);
                            crop.LLMetadata = GetCodeValues(table, cropName + " llCode", row, numLayers);
                            crop.KL = GetDoubleValues(table, cropName + " kl (/day)", row, numLayers);
                            crop.XF = GetDoubleValues(table, cropName + " xf (0-1)", row, numLayers);
                            if (MathUtilities.ValuesInArray(crop.LL) ||
                                MathUtilities.ValuesInArray(crop.KL))
                                physical.Children.Add(crop);
                        }
                    }
                }

                soils.Add(soil);

                row += numLayers;
            }

            return soils;
        }

        /// <summary>Convert a collection of soils to a DataTable.</summary>
        public static DataTable FromSoils(List<Soil> soils)
        {
            DataTable table = new DataTable();

            foreach (var soil in soils)
            {
                var organic = soil.Structure.FindChild<Organic>();
                if (organic == null)
                    throw new Exception($"Cannot find organic node in soil {soil.Name}");
                var physical = soil.Structure.FindChild<Physical>();
                if (physical == null)
                    throw new Exception($"Cannot find physical node in soil {soil.Name}");
                var chemical = soil.Structure.FindChild<Chemical>();
                if (chemical == null)
                    throw new Exception($"Cannot find chemical node in soil {soil.Name}");
                var waterBalance = soil.Structure.FindChild<WaterBalance>();
                if (waterBalance == null)
                    throw new Exception($"Cannot find Water Balance node in soil {soil.Name}");

                int startRow = table.Rows.Count;
                int numValues = Math.Max(physical.Thickness.Length, chemical.Thickness.Length);

                double[] layerNo = new double[physical.Thickness.Length];
                for (int i = 1; i <= physical.Thickness.Length; i++)
                    layerNo[i - 1] = i;

                SetStringValue(table, "Name", soil.Name, startRow, numValues);
                SetDoubleValue(table, "RecordNo", soil.RecordNumber, startRow, numValues);
                SetStringValue(table, "Country", soil.Country, startRow, numValues);
                SetStringValue(table, "State", soil.State, startRow, numValues);
                SetStringValue(table, "Region", soil.Region, startRow, numValues);
                SetStringValue(table, "NearestTown", soil.NearestTown, startRow, numValues);
                SetStringValue(table, "Site", soil.Site, startRow, numValues);
                SetStringValue(table, "APSoilNumber", soil.ApsoilNumber, startRow, numValues);
                SetStringValue(table, "Soil type texture or other descriptor", soil.SoilType, startRow, numValues);
                SetStringValue(table, "Local name", soil.LocalName, startRow, numValues);
                SetStringValue(table, "ASC_Order", soil.ASCOrder, startRow, numValues);
                SetStringValue(table, "ASC_Sub-order", soil.ASCSubOrder, startRow, numValues);
                SetDoubleValue(table, "Latitude", soil.Latitude, startRow, numValues);
                SetDoubleValue(table, "Longitude", soil.Longitude, startRow, numValues);
                SetStringValue(table, "LocationAccuracy", soil.LocationAccuracy, startRow, numValues);
                SetStringValue(table, "YearOfSampling", soil.YearOfSampling, startRow, numValues);
                SetStringValue(table, "DataSource", StringUtilities.DQuote(soil.DataSource), startRow, numValues);
                SetStringValue(table, "Comments", StringUtilities.DQuote(soil.Comments), startRow, numValues);
                SetStringValue(table, "NaturalVegetation", soil.NaturalVegetation, startRow, numValues);
                SetStringValue(table, "MunsellColour", null, startRow, numValues);
                SetStringValue(table, "MunsellColourCode", null, startRow, numValues);
                SetDoubleValues(table, "LayerNo", layerNo, startRow);
                SetDoubleValues(table, "Thickness (mm)", physical.Thickness, startRow);
                SetDoubleValues(table, "BD (g/cc)", physical.BD, startRow);
                SetCodeValues(table, "BDCode", physical.BDMetadata, startRow);
                SetDoubleValues(table, "Rocks (%)", physical.Rocks, startRow);
                SetCodeValues(table, "RocksCode", physical.RocksMetadata, startRow);
                SetStringValues(table, "Texture", physical.Texture, startRow);
                SetStringValues(table, "TextureCode", physical.TextureMetadata, startRow);
                SetDoubleValues(table, "SAT (mm/mm)", physical.SAT, startRow);
                SetCodeValues(table, "SATCode", physical.SATMetadata, startRow);
                SetDoubleValues(table, "DUL (mm/mm)", physical.DUL, startRow);
                SetCodeValues(table, "DULCode", physical.DULMetadata, startRow);
                SetDoubleValues(table, "LL15 (mm/mm)", physical.LL15, startRow);
                SetCodeValues(table, "LL15Code", physical.LL15Metadata, startRow);
                SetDoubleValues(table, "Airdry (mm/mm)", physical.AirDry, startRow);
                SetCodeValues(table, "AirdryCode", physical.AirDryMetadata, startRow);

                SetDoubleValue(table, "SummerU", waterBalance.SummerU, startRow, numValues);
                SetDoubleValue(table, "SummerCona", waterBalance.SummerCona, startRow, numValues);
                SetDoubleValue(table, "WinterU", waterBalance.WinterU, startRow, numValues);
                SetDoubleValue(table, "WinterCona", waterBalance.WinterCona, startRow, numValues);
                SetStringValue(table, "SummerDate", "=\"" + waterBalance.SummerDate + "\"", startRow, numValues);
                SetStringValue(table, "WinterDate", "=\"" + waterBalance.WinterDate + "\"", startRow, numValues);
                SetDoubleValue(table, "Salb", waterBalance.Salb, startRow, numValues);
                SetDoubleValue(table, "DiffusConst", waterBalance.DiffusConst, startRow, numValues);
                SetDoubleValue(table, "DiffusSlope", waterBalance.DiffusSlope, startRow, numValues);
                SetDoubleValue(table, "CN2Bare", waterBalance.CN2Bare, startRow, numValues);
                SetDoubleValue(table, "CNRed", waterBalance.CNRed, startRow, numValues);
                SetDoubleValue(table, "CNCov", waterBalance.CNCov, startRow, numValues);
                SetDoubleValue(table, "RootCN", organic.FOMCNRatio, startRow, numValues);
                SetDoubleValues(table, "RootWT", organic.FOM, startRow);
                SetDoubleValues(table, "SoilCN", organic.SoilCNRatio, startRow);
                SetDoubleValue(table, "EnrACoeff", 7.4, startRow, numValues);
                SetDoubleValue(table, "EnrBCoeff", 0.2, startRow, numValues);
                SetDoubleValues(table, "SWCON (0-1)", waterBalance.SWCON, startRow);
                SetDoubleValues(table, "MWCON (0-1)", null, startRow);
                SetDoubleValues(table, "FBIOM (0-1)", organic.FBiom, startRow);
                SetDoubleValues(table, "FINERT (0-1)", organic.FInert, startRow);
                SetDoubleValues(table, "KS (mm/day)", physical.KS, startRow);

                SetDoubleValues(table, "ThicknessChem (mm)", organic.Thickness, startRow);
                SetDoubleValues(table, "OC", organic.Carbon, startRow);
                SetCodeValues(table, "OCCode", organic.CarbonMetadata, startRow);
                SetDoubleValues(table, "EC (1:5 dS/m)", chemical.EC, startRow);
                SetCodeValues(table, "ECCode", chemical.ECMetadata, startRow);
                SetDoubleValues(table, "PH", chemical.PH, startRow);
                SetCodeValues(table, "PHCode", chemical.PHMetadata, startRow);

                var cl = soil.Children
                             .Where(child => child.Name.Equals("CL", StringComparison.InvariantCultureIgnoreCase))
                             .First() as Solute;
                if (cl != null)
                    SetDoubleValues(table, "CL (mg/kg)", cl.InitialValues, startRow);
                SetDoubleValues(table, "Boron (Hot water mg/kg)", null, startRow);
                SetCodeValues(table, "BoronCode", null, startRow);
                SetDoubleValues(table, "CEC (cmol+/kg)", null, startRow);
                SetCodeValues(table, "CECCode", null, startRow);
                SetDoubleValues(table, "Ca (cmol+/kg)", null, startRow);
                SetCodeValues(table, "CaCode", null, startRow);
                SetDoubleValues(table, "Mg (cmol+/kg)", null, startRow);
                SetCodeValues(table, "MgCode", null, startRow);
                SetDoubleValues(table, "Na (cmol+/kg)", null, startRow);
                SetCodeValues(table, "NaCCode", null, startRow);
                SetDoubleValues(table, "K (cmol+/kg)", null, startRow);
                SetCodeValues(table, "KCode", null, startRow);

                SetDoubleValues(table, "ESP (%)", chemical.ESP, startRow);
                SetCodeValues(table, "ESPCode", chemical.ESPMetadata, startRow);

                SetDoubleValues(table, "Mn (mg/kg)", null, startRow);
                SetCodeValues(table, "MnCode", null, startRow);
                SetDoubleValues(table, "Al (cmol+/kg)", null, startRow);
                SetCodeValues(table, "AlCode", null, startRow);

                SetDoubleValues(table, "ParticleSizeSand (%)", physical.ParticleSizeSand, startRow);
                SetCodeValues(table, "ParticleSizeSandCode", physical.ParticleSizeSandMetadata, startRow);
                SetDoubleValues(table, "ParticleSizeSilt (%)", physical.ParticleSizeSilt, startRow);
                SetCodeValues(table, "ParticleSizeSiltCode", physical.ParticleSizeSiltMetadata, startRow);
                SetDoubleValues(table, "ParticleSizeClay (%)", physical.ParticleSizeClay, startRow);
                SetCodeValues(table, "ParticleSizeClayCode", physical.ParticleSizeClayMetadata, startRow);

                var crops = soil.Node.FindChildren<SoilCrop>(recurse: true);
                foreach (var soilCrop in crops)
                    SetCropValues(table, soilCrop, startRow);
            }

            return table;
        }


        /// <summary>Return a single string value from the specified table and row</summary>
        private static string GetStringValue(DataTable table, int row, string variableName)
        {
            if (table.Columns.Contains(variableName))
                return table.Rows[row][variableName].ToString();
            return null;
        }

        /// <summary>Return a single double value from the specified table and row.</summary>
        private static double GetDoubleValue(DataTable table, int row, string variableName)
        {
            if (!table.Columns.Contains(variableName) ||
                table.Rows[row][variableName] == DBNull.Value)
                return double.NaN;
            try
            {
                return Convert.ToDouble(table.Rows[row][variableName]);
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>Return a single integer value from the specified table and row.</summary>
        private static int GetIntegerValue(DataTable table, int row, string variableName)
        {
            if (!table.Columns.Contains(variableName) ||
                table.Rows[row][variableName] == DBNull.Value)
                return 0;
            return Convert.ToInt32(table.Rows[row][variableName]);
        }

        /// <summary>Return an array of values for the specified column.</summary>
        private static double[] GetDoubleValues(DataTable table, string variableName, int row, int numRows)
        {
            if (table.Columns.Contains(variableName))
            {
                double[] Values = DataTableUtilities.GetColumnAsDoubles(table, variableName, numRows, row, CultureInfo.InvariantCulture);
                if (MathUtilities.ValuesInArray(Values))
                {
                    // Convert MissingValue for Nan
                    for (int i = 0; i != Values.Length; i++)
                        if (Values[i] == MathUtilities.MissingValue)
                            Values[i] = double.NaN;
                    return Values;
                }
            }
            return null;
        }

        /// <summary>Set a column of double values in the specified table.</summary>
        private static void SetDoubleValues(DataTable table, string columnName, double[] values, int startRow)
        {
            if (MathUtilities.ValuesInArray(values))
                DataTableUtilities.AddColumn(table, columnName, values, startRow, values.Length);
            else if (!table.Columns.Contains(columnName))
                table.Columns.Add(columnName, typeof(double));
        }

        /// <summary>Set a column of string values in the specified table.</summary>
        private static void SetStringValues(DataTable table, string columnName, string[] values, int startRow)
        {
            if (MathUtilities.ValuesInArray(values))
                DataTableUtilities.AddColumn(table, columnName, values, startRow, values.Length);
            else if (!table.Columns.Contains(columnName))
                table.Columns.Add(columnName, typeof(string));
        }

        /// <summary>Set a column to the specified Value a specificed numebr of times.</summary>
        private static void SetDoubleValue(DataTable table, string columnName, double value, int startRow, int numValues)
        {
            double[] values = new double[numValues];
            for (int i = 0; i < numValues; i++)
                values[i] = value;
            SetDoubleValues(table, columnName, values, startRow);
        }

        /// <summary>Set a column to the specified Value a specificed numebr of times.</summary>
        private static void SetStringValue(DataTable table, string columnName, string value, int startRow, int numValues)
        {
            string[] values = StringUtilities.CreateStringArray(value, numValues);
            SetStringValues(table, columnName, values, startRow);
        }

        /// <summary>
        /// Return an array of values for the specified column.
        /// </summary>
        private static string[] GetStringValues(DataTable table, string variableName, int startRow, int numRows)
        {
            if (table.Columns.Contains(variableName))
            {
                string[] Values = DataTableUtilities.GetColumnAsStrings(table, variableName, numRows, startRow, CultureInfo.InvariantCulture);
                if (MathUtilities.ValuesInArray(Values))
                    return Values;
            }
            return null;
        }

        /// <summary>Set the crop values in the table for the specified crop name.</summary>
        private static void SetCropValues(DataTable table, SoilCrop crop, int startRow)
        {
            SetDoubleValues(table, crop.Name + " ll (mm/mm)", crop.LL, startRow);
            SetCodeValues(table, crop.Name + " llCode", crop.LLMetadata, startRow);
            SetDoubleValues(table, crop.Name + " kl (/day)", crop.KL, startRow);
            SetDoubleValues(table, crop.Name + " xf (0-1)", crop.XF, startRow);
        }

        /// <summary>Return a list of code values for the specified variable.</summary>
        private static string[] GetCodeValues(DataTable table, string variableName, int row, int numRows)
        {
            if (!table.Columns.Contains(variableName))
                return null;

            return DataTableUtilities.GetColumnAsStrings(table, variableName, numRows, row, CultureInfo.InvariantCulture);
        }

        /// <summary>Set a column of metadata values for the specified column.</summary>
        private static void SetCodeValues(DataTable table, string columnName, string[] metadata, int startRow)
        {
            SetStringValues(table, columnName, metadata, startRow);
        }

        /// <summary>Convert an APSoil code into metadata string.</summary>
        private static string[] CodeToMetaData(string[] codes)
        {
            string[] metadata = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++)
                if (codes[i] == "FM")
                    metadata[i] = "Field measured and checked for sensibility";
                else if (codes[i] == "C_grav")
                    metadata[i] = "Calculated from gravimetric moisture when profile wet but drained";
                else if (codes[i] == "E")
                    metadata[i] = "Estimated based on local knowledge";
                else if (codes[i] == "U")
                    metadata[i] = "Unknown source or quality of data";
                else if (codes[i] == "LM")
                    metadata[i] = "Laboratory measured";
                else if (codes[i] == "V")
                    metadata[i] = "Volumetric measurement";
                else if (codes[i] == "M")
                    metadata[i] = "Measured";
                else if (codes[i] == "C_bd")
                    metadata[i] = "Calculated from measured, estimated or calculated BD";
                else if (codes[i] == "C_pt")
                    metadata[i] = "Developed using a pedo-transfer function";
                else
                    metadata[i] = codes[i];
            return metadata;
        }

        /// <summary>Convert a metadata into an abreviated code.</summary>
        static public string[] MetaDataToCode(string[] Metadata)
        {
            if (Metadata == null)
                return null;

            string[] Codes = new string[Metadata.Length];
            for (int i = 0; i < Metadata.Length; i++)
                if (Metadata[i] == "Field measured and checked for sensibility")
                    Codes[i] = "FM";
                else if (Metadata[i] == "Calculated from gravimetric moisture when profile wet but drained")
                    Codes[i] = "C_grav";
                else if (Metadata[i] == "Estimated based on local knowledge")
                    Codes[i] = "E";
                else if (Metadata[i] == "Unknown source or quality of data")
                    Codes[i] = "U";
                else if (Metadata[i] == "Laboratory measured")
                    Codes[i] = "LM";
                else if (Metadata[i] == "Volumetric measurement")
                    Codes[i] = "V";
                else if (Metadata[i] == "Measured")
                    Codes[i] = "M";
                else if (Metadata[i] == "Calculated from measured, estimated or calculated BD")
                    Codes[i] = "C_bd";
                else if (Metadata[i] == "Developed using a pedo-transfer function")
                    Codes[i] = "C_pt";
                else
                    Codes[i] = Metadata[i];
            return Codes;
        }
    }
}