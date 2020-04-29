namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Encapsulates a collection of stock genotype parameters. It can read the GrazPlan .prm
    /// files as well as the APSIM ruminant JSON file format.
    /// </summary>
    [Serializable]
    public class Genotypes
    {
        /// <summary>
        /// User supplied genotypes. These are searched first when looking for genotypes.
        /// </summary>
        private List<Genotype> genotypes = new List<Genotype>();

        /// <summary>Constructor.</summary>
        public Genotypes()
        {
            var xmlString = ReflectionUtilities.GetResourceAsString("Models.Resources.ruminant.prm");
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            var parameters = xml.DocumentElement;

            ReadPRM(parameters);
        }

        /// <summary>Get a list of all genotypes.</summary>
        public IEnumerable<Genotype> All { get { return genotypes; } }

        /// <summary>Get a list of genotype names.</summary>
        public IEnumerable<string> Names { get { return All.Select(genotype => genotype.Name); } }

        /// <summary>
        /// Read a parameter set and append to the json array.
        /// </summary>
        /// <param name="xmlString">The XML string to read.</param>
        public void ReadPRM(string xmlString)
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            var parameters = xml.DocumentElement;

            ReadPRM(parameters);
        }

        /// <summary>Set the user specified genotypes.</summary>
        /// <param name="animalParameterSet">The user specified animal parameter set.</param>
        public void Add(AnimalParamSet animalParameterSet)
        {
            Add(new Genotype(animalParameterSet));
        }

        /// <summary>Get a genotype. Throws if not found.</summary>
        /// <param name="genotypeName"></param>
        public Genotype Get(string genotypeName)
        {
            var foundGenotype = All.Where(genotype => genotype.Name.Equals(genotypeName, StringComparison.InvariantCultureIgnoreCase));
            if (foundGenotype.Count() == 0)
                throw new Exception($"Cannot find stock genotype {genotypeName}");
            return foundGenotype.First();
        }


        /// <summary>
        /// Create a genotype cross.                                      
        /// </summary>
        /// <param name="nameOfNewGenotype">Name of new genotype. Can be null.</param>
        /// <param name="damBreedName">Dam breed name.</param>
        /// <param name="damProportion">Proportion dam.</param>
        /// <param name="sireBreedName">Sire breed name.</param>
        /// <param name="sireProportion">Proportion sire.</param>
        public AnimalParamSet CreateGenotypeCross(string nameOfNewGenotype,
                                                  string damBreedName, double damProportion,
                                                  string sireBreedName, double sireProportion)
        {
            if (damProportion + sireProportion != 1)
                throw new Exception("When creating a cross breed the total proportions must be equal to one.");

            var damBreedGenotype = Get(damBreedName);
            if (damBreedGenotype == null)
                throw new Exception($"Cannot find a stock genotype named {damBreedName}");
            var damBreed = damBreedGenotype.Parameters;
            damBreed.EnglishName = damBreedName;
            damBreed.DeriveParams();
            damBreed.Initialise();

            var sireBreedGenotype = Get(sireBreedName);
            var sireBreed = damBreedGenotype.Parameters;
            sireBreed.EnglishName = sireBreedName;
            sireBreed.DeriveParams();
            sireBreed.Initialise();

            var newGenotype = new AnimalParamSet();
            newGenotype.Name = nameOfNewGenotype;
            newGenotype.sEditor = damBreed.sEditor;
            newGenotype.sEditDate = damBreed.sEditDate;
            newGenotype.Animal = damBreed.Animal;
            newGenotype.bDairyBreed = damBreed.bDairyBreed;
            newGenotype.MaxYoung = damBreed.MaxYoung;
            newGenotype.OvulationPeriod = damBreed.OvulationPeriod;
            newGenotype.Puberty = damBreed.Puberty;

            newGenotype.FBreedSRW = damProportion * damBreed.FBreedSRW + sireProportion * sireBreed.FBreedSRW;
            newGenotype.FPotFleeceWt = damProportion * damBreed.FPotFleeceWt + sireProportion * sireBreed.FPotFleeceWt;
            newGenotype.FDairyIntakePeak = damProportion * damBreed.FDairyIntakePeak + sireProportion * sireBreed.FDairyIntakePeak;
            newGenotype.FleeceRatio = damProportion * damBreed.FleeceRatio + sireProportion * sireBreed.FleeceRatio;
            newGenotype.MaxFleeceDiam = damProportion * damBreed.MaxFleeceDiam + sireProportion * sireBreed.MaxFleeceDiam;
            newGenotype.PeakMilk = damProportion * damBreed.PeakMilk + sireProportion * sireBreed.PeakMilk;
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortRate[idx] = damProportion * damBreed.MortRate[idx] + sireProportion * sireBreed.MortRate[idx];
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortAge[idx] = damProportion * damBreed.MortAge[idx] + sireProportion * sireBreed.MortAge[idx];
            newGenotype.MortIntensity = damProportion * damBreed.MortIntensity + sireProportion * sireBreed.MortIntensity;
            newGenotype.MortCondConst = damProportion * damBreed.MortCondConst + sireProportion * sireBreed.MortCondConst;
            newGenotype.MortWtDiff = damProportion * damBreed.MortWtDiff + sireProportion * sireBreed.MortWtDiff;

            for (int idx = 0; idx < newGenotype.SRWScalars.Length; idx++) newGenotype.SRWScalars[idx] = damProportion * damBreed.SRWScalars[idx] + sireProportion * sireBreed.SRWScalars[idx];
            for (int idx = 1; idx < newGenotype.GrowthC.Length; idx++) newGenotype.GrowthC[idx] = damProportion * damBreed.GrowthC[idx] + sireProportion * sireBreed.GrowthC[idx];
            for (int idx = 1; idx < newGenotype.IntakeC.Length; idx++) newGenotype.IntakeC[idx] = damProportion * damBreed.IntakeC[idx] + sireProportion * sireBreed.IntakeC[idx];
            for (int idx = 0; idx < newGenotype.IntakeLactC.Length; idx++) newGenotype.IntakeLactC[idx] = damProportion * damBreed.IntakeLactC[idx] + sireProportion * sireBreed.IntakeLactC[idx];
            for (int idx = 1; idx < newGenotype.GrazeC.Length; idx++) newGenotype.GrazeC[idx] = damProportion * damBreed.GrazeC[idx] + sireProportion * sireBreed.GrazeC[idx];
            for (int idx = 1; idx < newGenotype.EfficC.Length; idx++) newGenotype.EfficC[idx] = damProportion * damBreed.EfficC[idx] + sireProportion * sireBreed.EfficC[idx];
            for (int idx = 1; idx < newGenotype.MaintC.Length; idx++) newGenotype.MaintC[idx] = damProportion * damBreed.MaintC[idx] + sireProportion * sireBreed.MaintC[idx];
            for (int idx = 1; idx < newGenotype.DgProtC.Length; idx++) newGenotype.DgProtC[idx] = damProportion * damBreed.DgProtC[idx] + sireProportion * sireBreed.DgProtC[idx];
            for (int idx = 1; idx < newGenotype.ProtC.Length; idx++) newGenotype.ProtC[idx] = damProportion * damBreed.ProtC[idx] + sireProportion * sireBreed.ProtC[idx];
            for (int idx = 1; idx < newGenotype.PregC.Length; idx++) newGenotype.PregC[idx] = damProportion * damBreed.PregC[idx] + sireProportion * sireBreed.PregC[idx];
            for (int idx = 1; idx < newGenotype.PregScale.Length; idx++) newGenotype.PregScale[idx] = damProportion * damBreed.PregScale[idx] + sireProportion * sireBreed.PregScale[idx];
            for (int idx = 1; idx < newGenotype.BirthWtScale.Length; idx++) newGenotype.BirthWtScale[idx] = damProportion * damBreed.BirthWtScale[idx] + sireProportion * sireBreed.BirthWtScale[idx];
            for (int idx = 1; idx < newGenotype.PeakLactC.Length; idx++) newGenotype.PeakLactC[idx] = damProportion * damBreed.PeakLactC[idx] + sireProportion * sireBreed.PeakLactC[idx];
            for (int idx = 1; idx < newGenotype.LactC.Length; idx++) newGenotype.LactC[idx] = damProportion * damBreed.LactC[idx] + sireProportion * sireBreed.LactC[idx];
            for (int idx = 1; idx < newGenotype.WoolC.Length; idx++) newGenotype.WoolC[idx] = damProportion * damBreed.WoolC[idx] + sireProportion * sireBreed.WoolC[idx];
            for (int idx = 1; idx < newGenotype.ChillC.Length; idx++) newGenotype.ChillC[idx] = damProportion * damBreed.ChillC[idx] + sireProportion * sireBreed.ChillC[idx];
            for (int idx = 1; idx < newGenotype.GainC.Length; idx++) newGenotype.GainC[idx] = damProportion * damBreed.GainC[idx] + sireProportion * sireBreed.GainC[idx];
            for (int idx = 1; idx < newGenotype.PhosC.Length; idx++) newGenotype.PhosC[idx] = damProportion * damBreed.PhosC[idx] + sireProportion * sireBreed.PhosC[idx];
            for (int idx = 1; idx < newGenotype.SulfC.Length; idx++) newGenotype.SulfC[idx] = damProportion * damBreed.SulfC[idx] + sireProportion * sireBreed.SulfC[idx];
            for (int idx = 1; idx < newGenotype.MethC.Length; idx++) newGenotype.MethC[idx] = damProportion * damBreed.MethC[idx] + sireProportion * sireBreed.MethC[idx];
            for (int idx = 1; idx < newGenotype.AshAlkC.Length; idx++) newGenotype.AshAlkC[idx] = damProportion * damBreed.AshAlkC[idx] + sireProportion * sireBreed.AshAlkC[idx];
            for (int idx = 1; idx < newGenotype.DayLengthConst.Length; idx++) newGenotype.DayLengthConst[idx] = damProportion * damBreed.DayLengthConst[idx] + sireProportion * sireBreed.DayLengthConst[idx];
            for (int idx = 0; idx < newGenotype.ToxaemiaSigs.Length; idx++) newGenotype.ToxaemiaSigs[idx] = damProportion * damBreed.ToxaemiaSigs[idx] + sireProportion * sireBreed.ToxaemiaSigs[idx];
            for (int idx = 0; idx < newGenotype.DystokiaSigs.Length; idx++) newGenotype.DystokiaSigs[idx] = damProportion * damBreed.DystokiaSigs[idx] + sireProportion * sireBreed.DystokiaSigs[idx];
            for (int idx = 0; idx < newGenotype.ExposureConsts.Length; idx++) newGenotype.ExposureConsts[idx] = damProportion * damBreed.ExposureConsts[idx] + sireProportion * sireBreed.ExposureConsts[idx];

            newGenotype.FertWtDiff = damProportion * damBreed.FertWtDiff + sireProportion * sireBreed.FertWtDiff;
            newGenotype.SelfWeanPropn = damProportion * damBreed.SelfWeanPropn + sireProportion * sireBreed.SelfWeanPropn;
            for (int idx = 1; idx < newGenotype.ConceiveSigs.Length; idx++)
                for (int Jdx = 0; Jdx < newGenotype.ConceiveSigs[idx].Length; Jdx++)
                    newGenotype.ConceiveSigs[idx][Jdx] = damProportion * damBreed.ConceiveSigs[idx][Jdx] + sireProportion * sireBreed.ConceiveSigs[idx][Jdx];

            for (int idx = 0; idx <= newGenotype.FParentage.Length - 1; idx++)
                newGenotype.FParentage[idx].fPropn = 0.0;

            SetParentage(damBreed, damProportion, newGenotype);
            SetParentage(sireBreed, sireProportion, newGenotype);

            // Add the new genotype into our list.
            Add(new Genotype(newGenotype));

            return newGenotype;
        }

        /// <summary>
        /// Set parentage of a genotype.
        /// </summary>
        /// <param name="parentBreed">The parent breed.</param>
        /// <param name="proportion">The proportion of the parent.</param>
        /// <param name="newGenotype">The genotype to set the parentage in.</param>
        private static void SetParentage(AnimalParamSet parentBreed, double proportion, AnimalParamSet newGenotype)
        {
            for (int k = 0; k <= parentBreed.FParentage.Length - 1; k++)
            {
                int i = 0;
                while ((i < newGenotype.FParentage.Length) && (parentBreed.FParentage[k].sBaseBreed != newGenotype.FParentage[i].sBaseBreed))
                    i++;
                if (i == newGenotype.FParentage.Length)
                {
                    Array.Resize(ref newGenotype.FParentage, i + 1);
                    newGenotype.FParentage[i].sBaseBreed = parentBreed.FParentage[k].sBaseBreed;
                    newGenotype.FParentage[i].fPropn = 0.0;
                }
                newGenotype.FParentage[i].fPropn = newGenotype.FParentage[i].fPropn
                                            + proportion * parentBreed.FParentage[k].fPropn;
            }
        }

        /// <summary>
        /// Read a parameter set and append to the json array.
        /// </summary>
        /// <param name="parameterNode">The XML parameter node to convert.</param>
        private void ReadPRM(XmlNode parameterNode)
        {
            Add(new Genotype(parameterNode));

            // recurse through child parameter sets.
            foreach (var child in XmlUtilities.ChildNodes(parameterNode, "set"))
                ReadPRM(child);
        }

        /// <summary>Add a genotype into the list of genotypes.</summary>
        /// <param name="genotypeToAdd">The genotype to add.</param>
        private void Add(Genotype genotypeToAdd)
        {
            var foundGenotype = genotypes.Find(genotype => genotype.Name == genotypeToAdd.Name);
            if (foundGenotype != null)
                genotypes.Remove(foundGenotype);
            genotypes.Add(genotypeToAdd);
        }
    }
}
