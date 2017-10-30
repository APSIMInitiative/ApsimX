
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class PathwayParameters
    {
        /// <summary></summary>
        public PathwayParameters()
        {
            structuralN = 25;
            SLNRatioTop = 1.32;
            SLNAv = 1.45;
            CiCaRatio = 0.7;
            CiCaRatioIntercept = 0.90;
            CiCaRatioSlope = -0.12;
            fcyc = 0;
            psiRd = 0.0175;
            psiVc = 1.75;
            psiJ2 = 2.43;
            psiJ = 3.20;
            psiVp = 3.39;
            x = 0.4;
        }

        /// <summary></summary>
        [ModelPar("GUUtv", "Base leaf nitrogen content at or below which PS = 0", "N", "b", "mmol N/m2", "", "m2 leaf")]
        public double structuralN { get; set; }
        /// <summary></summary>
        [ModelPar("6awks", "Ratio of SLNo to SLNav", "SLN", "ratio_top", "")]
        public double SLNRatioTop { get; set; }
        /// <summary></summary>
        [ModelPar("cNPBJ", "Average SLN over the canopy", "SLN", "av", "g N/m2", "", "m2 leaf")]
        public double SLNAv { get; set; }
        //Parmas based on C3
        /// <summary></summary>
        [ModelPar("36Sr7", "Ratio of Ci to Ca for the sunlit and shade leaf fraction", "Ci/Ca", "", "")]
        public double CiCaRatio { get; set; }
        /// <summary></summary>
        [ModelPar("WE2QN", "Intercept of linear relationship of Ci/Ca ratio to VPD", "b", "", "")]
        public double CiCaRatioIntercept { get; set; }
        /// <summary></summary>
        [ModelPar("jC0xB", "Slope of linear relationship of Ci/Ca ratio to VPD", "a", "", "1/kPa")]
        public double CiCaRatioSlope { get; set; }
        /// <summary></summary>
        [ModelPar("uk6BV", "Fraction of electrons at PSI that follow cyclic transport around PSI", "f", "cyc", "")]
        public double fcyc { get; set; }
        /// <summary></summary>
        [ModelPar("3WXTb", "Slope of the linear relationship between Rd_l and N(L) at 25°C with intercept = 0", "psi", "Rd", "mmol/mol N/s")]
        public double psiRd { get; set; }
        /// <summary></summary>
        [ModelPar("l2mwD", "Slope of the linear relationship between Vcmax_l and N(L) at 25°C with intercept = 0", "psi", "Vc", "mmol/mol N/s")]
        public double psiVc { get; set; }
        /// <summary></summary>
        [ModelPar("I0uh7", "Slope of the linear relationship between J2max_l and N(L) at 25°C with intercept = 0", "psi", "J2", "mmol/mol N/s")]
        public double psiJ2 { get; set; }
        /// <summary></summary>
        [ModelPar("JwwUu", "Slope of the linear relationship between Jmax_l and N(L) at 25°C with intercept = 0", "psi", "J", "mmol/mol N/s")]
        public double psiJ { get; set; }
        /// <summary></summary>
        [ModelPar("pYisy", "Slope of the linear relationship between Vpmax_l and N(L) at 25°C with interception = 0", "psi", "Vp", "mmol/mol N/s", "", "", true)]
        public double psiVp { get; set; }
        /// <summary></summary>
        [ModelPar("tuksS", "Fraction of electron transport partitioned to mesophyll chlorplast", "x", "", "", "", "", true)]
        public double x { get; set; }

        #region KineticParams
        // Kc μbar	
        /// <summary></summary>
        [ModelPar("Kc_P25", "", "", "", "")]
        public double Kc_P25 { get; set; } = 272.38;
        /// <summary></summary>
        [ModelPar("Kc_c", "", "K", "c", "μbar")]
        public double Kc_c { get; set; } = 32.689;
        /// <summary></summary>
        [ModelPar("Kc_b", "", "", "", "")]
        public double Kc_b { get; set; } = 9741.400;

        // Kc μbar	-- C4
        /// <summary></summary>
        [ModelPar("Kp_P25", "", "", "", "")]
        public double Kp_P25 { get; set; } = 139;
        /// <summary></summary>
        [ModelPar("Kp_c", "", "K", "p", "μbar")]
        public double Kp_c { get; set; } = 14.644;
        /// <summary></summary>
        [ModelPar("Kp_b", "", "", "", "")]
        public double Kp_b { get; set; } = 4366.129;

        //Ko μbar	
        /// <summary></summary>
        [ModelPar("Ko_P25", "", "", "", "")]
        public double Ko_P25 { get; set; } = 165820;
        /// <summary></summary>
        [ModelPar("Ko_c", "", "K", "o", "μbar")]
        public double Ko_c { get; set; } = 9.574;
        /// <summary></summary>
        [ModelPar("Ko_b", "", "", "", "")]
        public double Ko_b { get; set; } = 2853.019;

        //Vcmax/Vomax	-	
        /// <summary></summary>
        [ModelPar("VcMax.VoMax_P25", "", "", "", "")]
        public double VcMax_VoMax_P25 { get; set; } = 4.580;
        /// <summary></summary>
        [ModelPar("VcMax.VoMax_c", "", "Vc_max/Vo_max", "", "")]
        public double VcMax_VoMax_c { get; set; } = 13.241;
        /// <summary></summary>
        [ModelPar("VcMax.VoMax_b", "", "", "", "")]
        public double VcMax_VoMax_b { get; set; } = 3945.722;
        //Vomax/Vcmax	-	
        //   -	-	-
        //Vcmax μmol/m2/s*	
        /// <summary></summary>
        [ModelPar("VcMax_c", "", "V", "c_max", "μmol/m2/s")]
        public double VcMax_c { get; set; } = 26.355;
        /// <summary></summary>
        [ModelPar("VcMax_b", "", "", "", "")]
        public double VcMax_b { get; set; } = 7857.830;

        //Vpmax μmol/m2/s*	
        /// <summary></summary>
        [ModelPar("VpMax_c", "", "V", "p_max", "μmol/m2/s", "", "", true)]
        public double VpMax_c { get; set; } = 26.355;
        /// <summary></summary>
        [ModelPar("VpMax_b", "", "", "", "", "", "", true)]
        public double VpMax_b { get; set; } = 7857.830;

        //Rd μmol/m2/s*	
        /// <summary></summary>
        [ModelPar("Rd_c", "", "R", "d", "μmol/m2/s")]
        public double Rd_c { get; set; } = 18.715;
        /// <summary></summary>
        [ModelPar("Rd_b", "", "", "", "")]
        public double Rd_b { get; set; } = 5579.745;

        //Jmax(Barley, Farquhar 1980)    μmol/m2/s*	
        /// <summary></summary>
        [ModelPar("JMax_TOpt", "", "", "", "")]
        public double JMax_TOpt { get; set; } = 28.796;
        /// <summary></summary>
        [ModelPar("JMax_Omega", "", "J", "max", "μmol/m2/s")]
        public double JMax_Omega { get; set; } = 15.536;

        //gm(Arabidopsis, Bernacchi 2002)    μmol/m2/s/bar	
        /// <summary></summary>
        [ModelPar("gm_P25", "", "", "", "")]
        public double gm_P25 { get; set; } = 0.55;
        /// <summary></summary>
        [ModelPar("gm_TOpt", "", "", "", "")]
        public double gm_TOpt { get; set; } = 34.309;
        /// <summary></summary>
        [ModelPar("gm_Omega", "", "g", "m", "mol/m2/s/bar")]
        public double gm_Omega { get; set; } = 20.791;

        /// <summary></summary>
        [ModelPar("THGeL", "Quantum efficiency of PSII e- folow on PSII-absorbed light basis at the strictly limiting light level", "ɸ", "2(LL)", "")]
        public double F2 = 0.75;
        /// <summary></summary>
        [ModelPar("6RyTa", "Quantum efficiency of PSI e- flow at the strictly limiting light level", "ɸ", "1(LL)", "")]
        public double F1 = 0.95;

        #endregion
    }
}
