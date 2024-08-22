import sys
import os
sys.path.append(os.getcwd())

import pandas
import glob
from pandas.io.formats import excel
excel.ExcelFormatter.header_style = None

files = glob.glob(os.getcwd()+"**/**.xlsx", recursive=True)

changes = {
    "Wheat.Leaf.AppearedCohortNo": "Wheat.Leaf.Tips",
    "Wheat.Leaf.ExpandedCohortNo": "Wheat.Leaf.Ligules",
    "Wheat.Structure.Height": "Wheat.Leaf.Height",
    "Wheat.Structure.LeafTipsAppeared": "Wheat.Leaf.Tips",
    "Wheat.Structure.FinalLeafNumber": "Wheat.Leaf.FinalLeafNumber",
    "Wheat.Structure.MainStemPopn": "Wheat.Leaf.MainStemPopulation",
    "Wheat.Structure.TotalStemPopn": "Wheat.Leaf.StemPopulation",
    "Wheat.Structure.BranchNumber": "Wheat.Leaf.StemNumberPerPlant",
    "Wheat.Structure.Phyllochron": "Wheat.Phenology.Phyllochron",
    
    #the FAR data also had .se on some varaibles, so I've added that here as well to keep consistent
    "Wheat.Leaf.AppearedCohortNo.se": "Wheat.Leaf.Tips.se",
    "Wheat.Leaf.ExpandedCohortNo.se": "Wheat.Leaf.Ligules.se",
    "Wheat.Structure.Height.se": "Wheat.Leaf.Height.se",
    "Wheat.Structure.LeafTipsAppeared.se": "Wheat.Leaf.Tips.se",
    "Wheat.Structure.FinalLeafNumber.se": "Wheat.Leaf.FinalLeafNumber.se",
    "Wheat.Structure.MainStemPopn.se": "Wheat.Leaf.MainStemPopulation.se",
    "Wheat.Structure.TotalStemPopn.se": "Wheat.Leaf.StemPopulation.se",
    "Wheat.Structure.BranchNumber.se": "Wheat.Leaf.StemNumberPerPlant.se",
    "Wheat.Structure.Phyllochron.se": "Wheat.Phenology.Phyllochron.se"
}

for filename in files:
    if "KonyaMetData" not in filename and "Copy" not in filename:
        #print("Openning: " + filename)
        df = pandas.read_excel(filename, index_col="SimulationName")
        df_changed = df.rename(columns=changes)
        if not df.equals(df_changed):
            print("Writing: " + filename)
            df_changed.to_excel(filename, sheet_name="Observed")