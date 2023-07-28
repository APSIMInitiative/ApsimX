using System;

namespace Models.Surface
{
    /// <summary>
    /// SurfaceOrganicMatterDecompPoolType
    /// </summary>
    [Serializable]
    public class SurfaceOrganicMatterDecompPoolType
    {
        /// <summary>The name</summary>
        public string Name = "";
        /// <summary>The organic matter type</summary>
        public string OrganicMatterType = "";
        /// <summary>The fom</summary>
        public FOMType FOM;
    }

    /// <summary>
    /// SurfaceOrganicMatterDecompType
    /// </summary>
    [Serializable]
    public class SurfaceOrganicMatterDecompType
    {
        /// <summary>The pool</summary>
        public SurfaceOrganicMatterDecompPoolType[] Pool;

        /// <summary>Add two pools together.</summary>
        /// <param name="from">The pool to add into this instance.</param>
        public void Add(SurfaceOrganicMatterDecompType from)
        {
            if (Pool.Length != from.Pool.Length)
                throw new Exception("Cannot add SurfaceOrganicMatterDecompType together. Pool lengths differ.");
            for (int p = 0; p < Pool.Length; p++)
            {
                Pool[p].FOM.amount += from.Pool[p].FOM.amount;
                Pool[p].FOM.C += from.Pool[p].FOM.C;
                Pool[p].FOM.N += from.Pool[p].FOM.N;
            }
        }

        /// <summary>Multiply the FOM C and N by a value.</summary>
        /// <param name="value">The value to multiply the C and N by.</param>
        public void Multiply(double value)
        {
            foreach (var pool in Pool)
            {
                pool.FOM.C *= value;
                pool.FOM.N *= value;
            }
        }
    }

    /// <summary>
    /// FOMType
    /// </summary>
    [Serializable]
    public class FOMType
    {
        /// <summary>The amount</summary>
        public double amount;
        /// <summary>The c</summary>
        public double C;
        /// <summary>The n</summary>
        public double N;
        /// <summary>The p</summary>
        public double P;
        /// <summary>The ash alk</summary>
        public double AshAlk;
    }

    /// <summary>
    /// FOMLayerType
    /// </summary>
    [Serializable]
    public class FOMLayerType
    {
        /// <summary>The type</summary>
        public string Type = "";
        /// <summary>The layer</summary>
        public FOMLayerLayerType[] Layer;
    }

    /// <summary>
    /// FOMLayerLayerType
    /// </summary>
    [Serializable]
    public class FOMLayerLayerType
    {
        /// <summary>The fom</summary>
        public FOMType FOM;
        /// <summary>The CNR</summary>
        public double CNR;
        /// <summary>The labile p</summary>
        public double LabileP;
    }

    /// <summary>
    /// FOMPoolType
    /// </summary>
    [Serializable]
    public class FOMPoolType
    {
        /// <summary>The layer</summary>
        public FOMPoolLayerType[] Layer;
    }

    /// <summary>
    /// FOMPoolLayerType
    /// </summary>
    [Serializable]
    public class FOMPoolLayerType
    {
        /// <summary>The thickness</summary>
        public double thickness;
        /// <summary>The no3</summary>
        public double no3;
        /// <summary>The NH4</summary>
        public double nh4;
        /// <summary>The po4</summary>
        public double po4;
        /// <summary>The pool</summary>
        public FOMType[] Pool;
    }

    /// <summary>Specifies the different types of deposition in calls to add a patch.</summary>
    public enum DepositionTypeEnum
    {
        /// <summary></summary>
        ToNewPatch,
        /// <summary></summary>
        NewOverlappingPatches,
        /// <summary></summary>
        ToSpecificPatch,
        /// <summary></summary>
        ToAllPaddock,
    }

    /// <summary>
    /// AddSoilCNPatch
    /// </summary>
    public class AddSoilCNPatchType
    {
        /// <summary>The Sender</summary>
        public String Sender = "";
        /// <summary>The SuppressMessages</summary>
        public bool SuppressMessages;
        /// <summary>The DepositionType</summary>
        public DepositionTypeEnum DepositionType = DepositionTypeEnum.ToAllPaddock;
        /// <summary>The AffectedPatches_nm</summary>
        public String[] AffectedPatches_nm;
        /// <summary>The AffectedPatches_id</summary>
        public Int32[] AffectedPatches_id;
        /// <summary>The AreaFraction</summary>
        public Double AreaFraction;
        /// <summary>The PatchName</summary>
        public String PatchName = "";
        /// <summary>The Water</summary>
        public Double[] Water;
        /// <summary>The Urea</summary>
        public Double[] Urea;
        /// <summary>The NH4</summary>
        public Double[] NH4;
        /// <summary>The NO3</summary>
        public Double[] NO3;
        /// <summary>The POX</summary>
        public Double[] POX;
        /// <summary>The SO4</summary>
        public Double[] SO4;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
        /// <summary>The FOM_C</summary>
        public Double[] FOM_C;
        /// <summary>The FOM_C_pool1</summary>
        public Double[] FOM_C_pool1;
        /// <summary>The FOM_C_pool2</summary>
        public Double[] FOM_C_pool2;
        /// <summary>The FOM_C_pool3</summary>
        public Double[] FOM_C_pool3;
        /// <summary>The FOM_N</summary>
        public Double[] FOM_N;
    }

    /// <summary>
    /// AddSoilCNPatchwithFOM
    /// </summary>
    public class AddSoilCNPatchwithFOMType
    {
        /// <summary>The Sender</summary>
        public String Sender = "";
        /// <summary>The SuppressMessages</summary>
        public bool SuppressMessages;
        /// <summary>The DepositionType</summary>
        public DepositionTypeEnum DepositionType = DepositionTypeEnum.ToAllPaddock;
        /// <summary>The AffectedPatches_nm</summary>
        public String[] AffectedPatches_nm;
        /// <summary>The AffectedPatches_id</summary>
        public Int32[] AffectedPatches_id;
        /// <summary>The AreaNewPatch</summary>
        public Double AreaNewPatch;
        /// <summary>The PatchName</summary>
        public String PatchName = "";
        /// <summary>The Water</summary>
        public Double[] Water;
        /// <summary>The Urea</summary>
        public Double[] Urea;
        /// <summary>The NH4</summary>
        public Double[] NH4;
        /// <summary>The NO3</summary>
        public Double[] NO3;
        /// <summary>The POX</summary>
        public Double[] POX;
        /// <summary>The SO4</summary>
        public Double[] SO4;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
        /// <summary>The AddSoilCNPatchwithFOMFOMType</summary>
        public AddSoilCNPatchwithFOMFOMType FOM;
    }

    /// <summary>
    /// AddSoilCNPatchwithFOMFOM
    /// </summary>
    public class AddSoilCNPatchwithFOMFOMType
    {
        /// <summary>The Name</summary>
        public String Name = "";
        /// <summary>The Type</summary>
        public String Type = "";
        /// <summary>The SoilOrganicMaterialType</summary>
        public SoilOrganicMaterialType[] Pool;
    }

    /// <summary>
    /// CNPatchVariableType
    /// </summary>
    public class CNPatchVariableType
    {
        /// <summary>The Patch</summary>
        public CNPatchVariablePatchType[] Patch;
    }

    /// <summary>
    /// CNPatchVariablePatchType
    /// </summary>
    public class CNPatchVariablePatchType
    {
        /// <summary>The Value</summary>
        public Double[] Value;
    }

    /// <summary>
    /// SoilOrganicMaterial
    /// </summary>
    /// <remarks>
    /// This was a trial and was meant to replace FOM type (RCichota)
    /// </remarks>
    public class SoilOrganicMaterialType
    {
        /// <summary>The Name</summary>
        public String Name = "";
        /// <summary>The Type</summary>
        public String Type = "";
        /// <summary>The C</summary>
        public Double[] C;
        /// <summary>The N</summary>
        public Double[] N;
        /// <summary>The P</summary>
        public Double[] P;
        /// <summary>The S</summary>
        public Double[] S;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
    }

    /// <summary>
    /// TilledType
    /// </summary>
    [Serializable]
    public class TilledType : EventArgs
    {
        /// <summary>The fraction</summary>
        public double Fraction;
        /// <summary>The depth</summary>
        public double Depth;
    }

    /// <summary>
    /// AddUrineType
    /// </summary>
    /// <remarks>
    /// This was used in an early attempt to add excreta to SoilNitrogen in classic Apsim.
    /// It was never really implemented and was superseeded (partially anyways) by CNPatches
    /// It probably can be eliminated, or fully developed
    /// </remarks>
    public class AddUrineType
    {
        /// <summary>The number of urinations in the time step</summary>
        public double Urinations;
        /// <summary>The volume per urination in m^3</summary>
        public double VolumePerUrination;
        /// <summary>The area per urination in m^2</summary>
        public double AreaPerUrination;
        /// <summary>The eccentricity</summary>
        public double Eccentricity;
        /// <summary>The urea in kg N/ha</summary>
        public double Urea;
        /// <summary>The pox in kg P/ha</summary>
        public double POX;
        /// <summary>The s o4 in kg S/ha</summary>
        public double SO4;
        /// <summary>The ash alkin mol/ha</summary>
        public double AshAlk;
    }
}