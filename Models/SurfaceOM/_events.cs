using System;
using System.Collections.Generic;
using System.Text;
using Models.Soils;

namespace Models.SurfaceOM
{

    public partial class SurfaceOrganicMatter
    {

        public event Models.Soils.SoilNitrogen.ExternalMassFlowDelegate ExternalMassFlow;
        private void publish_ExternalMassFlow(ExternalMassFlowType massBalanceChange)
        {
            if (ExternalMassFlow != null)
                ExternalMassFlow.Invoke(massBalanceChange);
        }

        public delegate void SurfaceOrganicMatterDecompDelegate(SurfaceOrganicMatterDecompType Data);
        public event SurfaceOrganicMatterDecompDelegate PotentialResidueDecompositionCalculated;

        private void publish_SurfaceOrganicMatterDecomp(SurfaceOrganicMatterDecompType SOMDecomp)
        {
            if (PotentialResidueDecompositionCalculated != null)
                PotentialResidueDecompositionCalculated.Invoke(SOMDecomp);
        }

        public delegate void FOMPoolDelegate(FOMPoolType Data);
        public event FOMPoolDelegate IncorpFOMPool;
        private void publish_FOMPool(FOMPoolType data)
        {
            if (IncorpFOMPool != null)
                IncorpFOMPool.Invoke(data);
        }

        public class SurfaceOrganicMatterPoolType
        {
            public string Name = "";
            public string OrganicMatterType = "";
            public double PotDecompRate;
            public double no3;
            public double nh4;
            public double po4;
            public FOMType[] StandingFraction;
            public FOMType[] LyingFraction;
        }
        public class SurfaceOrganicMatterType
        {
            public SurfaceOrganicMatterPoolType[] Pool;
        }

        public delegate void SurfaceOrganicMatterDelegate(SurfaceOrganicMatterType Data);
        public event SurfaceOrganicMatterDelegate SurfaceOrganicMatterState;
        private void publish_SurfaceOrganicMatter(SurfaceOrganicMatterType SOM)
        {
            if (SurfaceOrganicMatterState != null)
                SurfaceOrganicMatterState.Invoke(SOM);
        }

        public class ResidueRemovedType
        {
            public string residue_removed_action = "";
            public double dlt_residue_fraction;
            public double[] residue_incorp_fraction;
        }

        public class ResidueAddedType
        {
            public string residue_type = "";
            public string dm_type = "";
            public double dlt_residue_wt;
            public double dlt_dm_n;
            public double dlt_dm_p;
        }
        public delegate void Residue_addedDelegate(ResidueAddedType Data);
        public event Residue_addedDelegate Residue_added;

        public delegate void Residue_removedDelegate(ResidueRemovedType Data);
        public event Residue_removedDelegate Residue_removed;

        public class SurfaceOMRemovedType
        {
            public string SurfaceOM_type = "";
            public string SurfaceOM_dm_type = "";
            public double dlt_SurfaceOM_wt;
            public double SurfaceOM_dlt_dm_n;
            public double SurfaceOM_dlt_dm_p;
        }

        public delegate void SurfaceOM_removedDelegate(SurfaceOMRemovedType Data);
        public event SurfaceOM_removedDelegate SurfaceOM_removed;

        public event NitrogenChangedDelegate NitrogenChanged;


    }

}