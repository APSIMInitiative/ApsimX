
namespace Models.PMF.Phenology
{
    public class PathwayParameters
    {
        public PathwayParameters()
        {
        }

        [ModelPar("GUUtv", "Base leaf nitrogen content at or below which PS = 0", "N", "b", "mmol N/m2", "", "m2 leaf")]
        public double structuralN { get; set; } = 25;
        [ModelPar("6awks", "Ratio of SLNo to SLNav", "SLN", "ratio_top", "")]
        public double SLNRatioTop { get; set; } = 1.32;
        [ModelPar("cNPBJ", "Average SLN over the canopy", "SLN", "av", "g N/m2", "", "m2 leaf")]
        public double SLNAv { get; set; } = 1.45;
        //Parmas based on C3
        [ModelPar("36Sr7", "Ratio of Ci to Ca for the sunlit and shade leaf fraction", "Ci/Ca", "", "")]
        public double CiCaRatio { get; set; } = 0.7;
        [ModelPar("WE2QN", "Intercept of linear relationship of Ci/Ca ratio to VPD", "b", "", "")]
        public double CiCaRatioIntercept { get; set; } = 0.90;
        [ModelPar("jC0xB", "Slope of linear relationship of Ci/Ca ratio to VPD", "a", "", "1/kPa")]
        public double CiCaRatioSlope { get; set; } = -0.12;
        [ModelPar("uk6BV", "Fraction of electrons at PSI that follow cyclic transport around PSI", "f", "cyc", "")]
        public double fcyc { get; set; } = 0;
        [ModelPar("3WXTb", "Slope of the linear relationship between Rd_l and N(L) at 25°C with intercept = 0", "psi", "Rd", "mmol/mol N/s")]
        public double psiRd { get; set; } = 0.0175;
        [ModelPar("l2mwD", "Slope of the linear relationship between Vcmax_l and N(L) at 25°C with intercept = 0", "psi", "Vc", "mmol/mol N/s")]
        public double psiVc { get; set; } = 1.75;
        [ModelPar("I0uh7", "Slope of the linear relationship between J2max_l and N(L) at 25°C with intercept = 0", "psi", "J2", "mmol/mol N/s")]
        public double psiJ2 { get; set; } = 2.43;
        [ModelPar("JwwUu", "Slope of the linear relationship between Jmax_l and N(L) at 25°C with intercept = 0", "psi", "J", "mmol/mol N/s")]
        public double psiJ { get; set; } = 3.20;
        [ModelPar("pYisy", "Slope of the linear relationship between Vpmax_l and N(L) at 25°C with interception = 0", "psi", "Vp", "mmol/mol N/s", "", "", true)]
        public double psiVp { get; set; } = 3.39;
        [ModelPar("tuksS", "Fraction of electron transport partitioned to mesophyll chlorplast", "x", "", "", "", "", true)]
        public double x { get; set; } = 0.4;

        #region KineticParams
        // Kc μbar	
        [ModelPar("Kc_P25", "", "", "", "")]
        public double Kc_P25 { get; set; } = 272.38;
        [ModelPar("Kc_c", "", "K", "c", "μbar")]
        public double Kc_c { get; set; } = 32.689;
        [ModelPar("Kc_b", "", "", "", "")]
        public double Kc_b { get; set; } = 9741.400;

        // Kc μbar	-- C4
        [ModelPar("Kp_P25", "", "", "", "")]
        public double Kp_P25 { get; set; } = 139;
        [ModelPar("Kp_c", "", "K", "p", "μbar")]
        public double Kp_c { get; set; } = 14.644;
        [ModelPar("Kp_b", "", "", "", "")]
        public double Kp_b { get; set; } = 4366.129;

        //Ko μbar	
        [ModelPar("Ko_P25", "", "", "", "")]
        public double Ko_P25 { get; set; } = 165820;
        [ModelPar("Ko_c", "", "K", "o", "μbar")]
        public double Ko_c { get; set; } = 9.574;
        [ModelPar("Ko_b", "", "", "", "")]
        public double Ko_b { get; set; } = 2853.019;

        //Vcmax/Vomax	-	
        [ModelPar("VcMax.VoMax_P25", "", "", "", "")]
        public double VcMax_VoMax_P25 { get; set; } = 4.580;
        [ModelPar("VcMax.VoMax_c", "", "Vc_max/Vo_max", "", "")]
        public double VcMax_VoMax_c { get; set; } = 13.241;
        [ModelPar("VcMax.VoMax_b", "", "", "", "")]
        public double VcMax_VoMax_b { get; set; } = 3945.722;
        //Vomax/Vcmax	-	
        //   -	-	-
        //Vcmax μmol/m2/s*	
        [ModelPar("VcMax_c", "", "V", "c_max", "μmol/m2/s")]
        public double VcMax_c { get; set; } = 26.355;
        [ModelPar("VcMax_b", "", "", "", "")]
        public double VcMax_b { get; set; } = 7857.830;

        //Vpmax μmol/m2/s*	
        [ModelPar("VpMax_c", "", "V", "p_max", "μmol/m2/s", "", "", true)]
        public double VpMax_c { get; set; } = 26.355;
        [ModelPar("VpMax_b", "", "", "", "", "", "", true)]
        public double VpMax_b { get; set; } = 7857.830;

        //Rd μmol/m2/s*	
        [ModelPar("Rd_c", "", "R", "d", "μmol/m2/s")]
        public double Rd_c { get; set; } = 18.715;
        [ModelPar("Rd_b", "", "", "", "")]
        public double Rd_b { get; set; } = 5579.745;

        //Jmax(Barley, Farquhar 1980)    μmol/m2/s*	
        [ModelPar("JMax_TOpt", "", "", "", "")]
        public double JMax_TOpt { get; set; } = 28.796;
        [ModelPar("JMax_Omega", "", "J", "max", "μmol/m2/s")]
        public double JMax_Omega { get; set; } = 15.536;

        //gm(Arabidopsis, Bernacchi 2002)    μmol/m2/s/bar	
        [ModelPar("gm_P25", "", "", "", "")]
        public double gm_P25 { get; set; } = 0.55;
        [ModelPar("gm_TOpt", "", "", "", "")]
        public double gm_TOpt { get; set; } = 34.309;
        [ModelPar("gm_Omega", "", "g", "m", "mol/m2/s/bar")]
        public double gm_Omega { get; set; } = 20.791;

        [ModelPar("THGeL", "Quantum efficiency of PSII e- folow on PSII-absorbed light basis at the strictly limiting light level", "ɸ", "2(LL)", "")]
        public double F2 = 0.75;
        [ModelPar("6RyTa", "Quantum efficiency of PSI e- flow at the strictly limiting light level", "ɸ", "1(LL)", "")]
        public double F1 = 0.95;

        #endregion
    }
}
