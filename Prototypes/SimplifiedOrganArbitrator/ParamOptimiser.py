# ---
# jupyter:
#   jupytext:
#     formats: ipynb,py:light
#     text_representation:
#       extension: .py
#       format_name: light
#       format_version: '1.5'
#       jupytext_version: 1.4.2
#   kernelspec:
#     display_name: Python 3
#     language: python
#     name: python3
# ---

# +
import datetime as dt
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import APSIMGraphHelpers as AGH
import GraphHelpers as GH
from scipy import stats
import statsmodels.api as sm
from statsmodels.formula.api import ols
import matplotlib.dates as mdates
import MathsUtilities as MUte
import shlex # package to construct the git command to subprocess format
import subprocess 
import ProcessWheatFiles as pwf
import xmltodict, json
import sqlite3
import scipy.optimize 
from skopt import gp_minimize
from skopt.callbacks import CheckpointSaver
from skopt import load
import re

from py_expression_eval import Parser
parser = Parser()

import winsound
frequency = 2500  # Set Frequency To 2500 Hertz
duration = 1000  # Set Duration To 1000 ms == 1 second
# %matplotlib inline

# +
Path = 'C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise'

BlankManager = {'$type': 'Models.Manager, Models',
            'Code': '',
            'Parameters': None,
            'Name': 'SetCropParameters',
            'IncludeInDocumentation': False,
            'Enabled': True,
            'ReadOnly': False}

SetCropParams = {
          "$type": "Models.Manager, Models",
          "Code": "using Models.Core;\r\nusing System;\r\nnamespace Models\r\n{\r\n\t[Serializable]\r\n    public class Script : Model\r\n    {\r\n        [Link] Zone zone;\r\n        [EventSubscribe(\"PlantSowing\")]\r\n        private void OnPlantSowing(object sender, EventArgs e)\r\n        {\r\n            object PpFac12 = 0.8;\r\n            zone.Set(\"Wheat.Phenology.CAMP.PpResponse.XYPairs.Y[3]\", PpFac12);  \r\n            object DeVernFac = -.3;\r\n            zone.Set(\"Wheat.Phenology.CAMP.DailyColdVrn1.Response.DeVernalisationRate.FixedValue\", DeVernFac);  \r\n        }\r\n    }\r\n}\r\n                \r\n",
          "Parameters": [],
          "Name": "SetCropParameters",
          "IncludeInDocumentation": False,
          "Enabled": True,
          "ReadOnly": False}

def AppendModeltoModelofTypeAndDeleteOldIfPresent(Parent,TypeToAppendTo,ModelToAppend):
    try:
        for child in Parent['Children']:
            if child['$type'] == TypeToAppendTo:
                pos = 0
                for g in child['Children']:
                    if g['Name'] == ModelToAppend['Name']:
                        del child['Children'][pos]
                        #print('Model ' + ModelToAppend['Name'] + ' found and deleted')
                    pos+=1
                child['Children'].append(ModelToAppend)
            else:
                Parent = AppendModeltoModelofTypeAndDeleteOldIfPresent(child,TypeToAppendTo,ModelToAppend)
        return Parent
    except:
        return Parent
    
def AppendModeltoModelofType(Parent,TypeToAppendTo,ModelToAppend):
    try:
        for child in Parent['Children']:
            if child['$type'] == TypeToAppendTo:
                child['Children'].append(ModelToAppend)
            else:
                Parent = AppendModeltoModelofType(child,TypeToAppendTo,ModelToAppend)
        return Parent
    except:
        return Parent
    
def findNextChild(Parent,ChildName):
    if len(Parent['Children']) >0:
        for child in range(len(Parent['Children'])):
            if Parent['Children'][child]['Name'] == ChildName:
                return Parent['Children'][child]
    else:
        return Parent[ChildName]

def findModel(Parent,PathElements):
    for pe in PathElements:
        Parent = findNextChild(Parent,pe)
    return Parent    

def StopReporting(WheatApsimx,modelPath):
    PathElements = modelPath.split('.')
    report = findModel(WheatApsimx,PathElements)
    report["EventNames"] = []

def removeModel(Parent,modelPath):
    PathElements = modelPath.split('.')
    Parent = findModel(Parent,PathElements[:-1])
    pos = 0
    found = False
    for c in Parent['Children']:
        if c['Name'] == PathElements[-1]:
            del Parent['Children'][pos]
            found = True
            break
        pos += 1
    if found == False:
        print('Failed to find ' + PathElements[-1] + ' to delete')

def ApplyParamReplacementSet(paramValues,paramNames,filePath):
    with open(filePath,'r',encoding="utf8") as ApsimxJSON:
        Apsimx = json.load(ApsimxJSON)
        ApsimxJSON.close()
    
    ## Remove old prameterSet manager in replacements
    removeModel(Apsimx,'Replacements.SetCropParameters')

    ## Add crop coefficient overwrite into replacements
    codeString = "using Models.Core;\r\nusing System;\r\nnamespace Models\r\n{\r\n\t[Serializable]\r\n    public class Script : Model\r\n    {\r\n        [Link] Zone zone;\r\n        [EventSubscribe(\"Sowing\")]\r\n        private void OnSowing(object sender, EventArgs e)\r\n     {\r\n        object Pval = 0; \r\n    "
    for p in range(len(paramValues)):
        codeString +=  "         Pval ="
        codeString += str(paramValues[p])
        codeString += ';\r\n         zone.Set(\"'
        codeString += paramNames[p]
        codeString += '\", Pval);  \r\n'
        
    codeString += '\r\n}\r\n}\r\n  }'

    SetCropParams["Code"] = codeString

    AppendModeltoModelofType(Apsimx,'Models.Core.Replacements, Models',SetCropParams)

    with open(filePath,'w') as ApsimxJSON:
        json.dump(Apsimx,ApsimxJSON,indent=2)
        
def makeLongString(SimulationSet):
    longString =  '/SimulationNameRegexPattern:"'
    longString =  longString + '(' + SimulationSet[0]  + ')|' # Add first on on twice as apsim doesn't run the first in the list
    for sim in SimulationSet[:]:
        longString = longString + '(' + sim + ')|'
    longString = longString + '(' + SimulationSet[-1] + ')'#|' ## Add Last on on twice as apsim doesnt run the last in the list
    #longString = longString + '(' + SimulationSet[-1] + ')"'
    return longString

def CalcScaledValue(Value,RMax,RMin):
    return (Value - RMin)/(RMax-RMin)
# +
def Preparefile(filePath):
    ## revert .apximx file to last comitt
#     !del C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise.db
    command= "git --git-dir=C:/GitHubRepos/ApsimX/.git --work-tree=C:/GitHubRepos/ApsimX checkout " + filePath 
    comm=shlex.split(command) # This will convert the command into list format
    subprocess.run(comm, shell=True) 
    ## Add blank manager into each simulation
    with open(filePath,'r',encoding="utf8") as ApsimxJSON:
        Apsimx = json.load(ApsimxJSON)
    ApsimxJSON.close()
    AppendModeltoModelofTypeAndDeleteOldIfPresent(Apsimx,'Models.Core.Zone, Models',BlankManager)
    with open(filePath,'w') as ApsimxJSON:
        json.dump(Apsimx,ApsimxJSON,indent=2)
    
def runModelItter(paramNames,paramValues,OptimisationVariables,SimulationSet,paramsTried):        
    ApplyParamReplacementSet(paramValues,paramNames,Path+'.apsimx')
    start = dt.datetime.now()
    simSet = makeLongString(SimulationSet)
    subprocess.run(['C:/GitHubRepos/ApsimX/bin/Debug/netcoreapp3.1/Models.exe',
                    Path+'.apsimx',
                   simSet], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    endrun = dt.datetime.now()
    runtime = (endrun-start).seconds
    con = sqlite3.connect(r'C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise.db')
    ObsPred = pd.read_sql("Select * from PredictedObserved",con)
    con.close()
    ScObsPre = pd.DataFrame(columns = ['ScObs','ScPred','Var','SimulationID'])
    indloc = 0
    for var in OptimisationVariables:
        DataPairs = ObsPred.reindex(['Observed.'+var,'Predicted.'+var,'SimulationID'],axis=1).dropna()
        for c in DataPairs.columns:
            DataPairs.loc[:,c] = pd.to_numeric(DataPairs.loc[:,c])
        VarMax = max(DataPairs.loc[:,'Observed.'+var].max(),DataPairs.loc[:,'Predicted.'+var].max())
        VarMin = min(DataPairs.loc[:,'Observed.'+var].min(),DataPairs.loc[:,'Predicted.'+var].min())
        for x in DataPairs.index:
            ScObsPre.loc[indloc,'ScObs'] = CalcScaledValue(DataPairs.loc[x,'Observed.'+var],VarMax,VarMin)
            ScObsPre.loc[indloc,'ScPred'] = CalcScaledValue(DataPairs.loc[x,'Predicted.'+var],VarMax,VarMin)
            ScObsPre.loc[indloc,'Var'] = var
            ScObsPre.loc[indloc,'SimulationID'] = DataPairs.loc[x,'SimulationID']
            indloc+=1
    RegStats = MUte.MathUtilities.CalcRegressionStats('LN',ScObsPre.loc[:,'ScPred'].values,ScObsPre.loc[:,'ScObs'].values)
    
    retVal = max(RegStats.NSE,0) *-1
    globals()["itteration"] += 1
    print(str(globals()["itteration"] )+"  "+str(paramsTried) + " run completed " +str(ObsPred.SimulationID.drop_duplicates().count()) + ' sims in ' + str(runtime) + ' seconds.  NSE = '+str(RegStats.NSE))
    return retVal

def runFittingItter(fittingParams):
    
    paramAddresses = ParamData.Address.values.tolist()
    paramSetForItter = currentParamVals.copy() #Start off with full current param set
    fittingParamsDF = pd.Series(index = paramsToOptimise,data=fittingParams)
    for p in fittingParamsDF.index:
        paramSetForItter[p] = fittingParamsDF[p] #replace parameters being fitted with current itteration values
    for p in paramSetForItter.index:
        if ParamData.loc[p,'Min_feasible']==np.nan: #for paramteters that reference another
            paramSetForItter[p] = paramSetForItter[ParamData.loc[p,'BestValue']] #update with current itterations value
    paramValues = paramSetForItter.values.tolist()
    return runModelItter(paramAddresses,paramValues,OptimisationVariables,SimulationSet,fittingParams)

def evalExpressions(value,refCol):
    if type(value) != str:
        ret = value
    else:
        members = value.split()
        if len(members)==1:
            ret = ParamData.loc[members[0],refCol]
        else:
            ref = ParamData.loc[members[0],refCol]
            opp = members[1]
            num = float(members[2])
            expression = 'ref'+opp+'num'
            ret = parser.parse(expression).evaluate({'ref':ref,'num':num})
    return float(ret)


# -

ParamData = pd.read_excel('OptimiseConfig.xlsx',sheet_name='ParamData',engine="openpyxl",index_col='Param')
SimSet = pd.read_excel('OptimiseConfig.xlsx',sheet_name='SimSet',engine="openpyxl")
VariableWeights = pd.read_excel('OptimiseConfig.xlsx',sheet_name='VariableWeights',engine="openpyxl")
OptimisationSteps = SimSet.columns.values.tolist()
paramsToOptimise = []
itteration = 0

OptimisationSteps

bestParamVals = pd.Series(index = ParamData.index,data=[evalExpressions(x,'BestValue') for x in ParamData.loc[:,'BestValue']])
bestParamVals

bounds = pd.Series(index= ParamData.index,
                   data = [(evalExpressions(ParamData.loc[x,'Min_feasible'],'Min_feasible'),evalExpressions(ParamData.loc[x,'Max_feasible'],'Max_feasible')) for x in ParamData.index])
bounds

# +
# step = "Potential canopy"
# paramAddresses = ParamData[0:1].Address.values.tolist()
# paramValues = currentParamVals[0:1].copy()
# OptimisationVariables = VariableWeights.loc[:,['Variable',step]].dropna().loc[:,'Variable'].values.tolist()
# SimulationSet = SimSet.loc[:,step].dropna().values.tolist()
# fittingParams = []
# runModelItter(paramAddresses,paramValues,OptimisationVariables,SimulationSet,fittingParams)
# -

ParamData.loc[:,step].dropna()

step = 'Biomass partitioning'
currentParamVals = bestParamVals.copy() #Get current set of best fits
for p in ParamData.loc[:,step].dropna().index:
    currentParamVals[p] = ParamData.loc[p,step] #apply fitting step specific overwrites
currentParamVals

for step in OptimisationSteps[:]:
    itteration = 0
    print(step + " Optimistion step")
    paramsToOptimise = ParamData.loc[ParamData.loc[:,step] == 'fit',step].index.values.tolist()
    print("fitting these parameters")
    print(paramsToOptimise)
    OptimisationVariables = VariableWeights.loc[:,['Variable',step]].dropna().loc[:,'Variable'].values.tolist()
    print("using these variables")
    print(OptimisationVariables)
    SimulationSet = SimSet.loc[:,step].dropna().values.tolist()
    print("from these simulations")
    print(SimulationSet)
    FirstX = bestParamVals.loc[paramsToOptimise].values.tolist()
    print("start params values are")
    print(FirstX)
    boundSet = bounds.loc[paramsToOptimise].values.tolist()
    print("parameter bounds are")
    print(boundSet)
    
    currentParamVals = bestParamVals.copy() #Get current set of best fits
    for p in ParamData.loc[:,step].dropna().index:
        if ParamData.loc[p,step] != 'fit':
            currentParamVals[p] = float(ParamData.loc[p,step]) #apply fitting step specific overwrites
    
    pos = 0
    for x in FirstX:
        if x < boundSet[pos][0]:
            FirstX[pos] = boundSet[pos][0]
        if x > boundSet[pos][1]:
            FirstX[pos] = boundSet[pos][1]
        pos +=1
    print("bound constrained start params values are")
    print(FirstX)
    
    Preparefile(Path+'.apsimx')

    RandomCalls = len(paramsToOptimise) * 12
    print(str(RandomCalls)+" Random calls")
    OptimizerCalls = max(20,len(paramsToOptimise) * 4)
    print(str(OptimizerCalls)+" Optimizer calls")
    TotalCalls = RandomCalls + OptimizerCalls

    checkpoint_saver = CheckpointSaver("./"+step+"checkpoint.pkl", compress=9)
    ret = gp_minimize(runFittingItter, boundSet, n_calls=TotalCalls,n_initial_points=RandomCalls,
                  initial_point_generator='sobol',callback=[checkpoint_saver],x0=FirstX)
    
    bestfits = ret.x
    pi=0
    for p in paramsToOptimise:
        bestParamVals[p]= bestfits[pi]
        pi +=1
    print("")
    print("BestFits for "+step)
    print(paramsToOptimise)
    print(bestfits)
    print("")

# ### First run of optimiser, Use only full N full irrigation treatment.  Fit for total biomass, organ bioamss, cover and LAI.  Fit RUE, k leaf size and senescence params

# +
SimulationSet = ["LincolnRS2016IrrFullNit300"]#,"LincolnRS2016IrrFullNit50","LincolnRS2016IrrFullNit0"]

OptimisationVariables = ['FodderBeet.AboveGround.Wt',
                         'FodderBeet.Leaf.Live.Wt',
                         'FodderBeet.Petiole.Live.Wt',
                         'FodderBeet.StorageRoot.Live.Wt',
                         'FodderBeet.Leaf.Canopy.CoverGreen',
                         'FodderBeet.Leaf.Canopy.CoverTotal',
                         'FodderBeet.Leaf.Canopy.LAI']

x0sDF = pd.DataFrame(data=[['RUE',22],
                           ['k',80],
                           ['LargestLeafArea',75],
                           ['leafSizeBreak',50],
                           ['SenCoverRate',5],
                           ['SenCoverBreak',70],
                           ['SenAgeRate',7],
                           ['SenAgeBreak',35]],columns=['param','value']).set_index('param')
                           
boundsDF = pd.DataFrame(data=[['RUE',(15,25)],
                              ['k',(50,100)],
                              ['LargestLeafArea',(50,100)],
                              ['leafSizeBreak',(30,80)],
                              ['SenCoverRate',(1,25)],
                              ['SenCoverBreak',(50,95)],
                              ['SenAgeRate',(0,30)],
                              ['SenAgeBreak',(5,100)]],columns=['param','range']).set_index('param')

paramNames=["FodderBeet.Leaf.Photosynthesis.RUE.FixedValue", 
            "FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue", 
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]",
            "FodderBeet.Leaf.AreaLargestLeaf.FixedValue", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[3]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]",
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]",
            "FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"]

def calcModelParamValues(fP):
    mP = []
    mP.append(fP[0]/10)#"FodderBeet.Leaf.Photosynthesis.RUE.FixedValue"
    mP.append(.01)#"FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
    mP.append(fP[1]/100)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue"# 
    mP.append(1.0)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]"
    mP.append(fP[2]/1000)#"FodderBeet.Leaf.AreaLargestLeaf.FixedValue"# 
    mP.append(fP[3])#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(fP[3]+2)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(1.0)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]"#
    mP.append(fP[4]/1000)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]"
    mP.append(fP[5]/100)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]"
    mP.append(fP[6]/1000)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]"
    mP.append(fP[7]/10)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]"
    mP.append(fP[7]/10+3)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]"
    mP.append(0.025)#"FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#                        
    
    return mP

Preparefile(Path+'.apsimx')
x0 = x0sDF.value.values.tolist()
bounds = boundsDF.range.values.tolist()

RandomCalls = 100
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./checkpoint.pkl", compress=9)
#CheckPoint = load("./checkpoint.pkl")
#x0 = CheckPoint.x_iters
#y0 = CheckPoint.func_vals
ret = gp_minimize(runFittingItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#y0=y0)
# + tags=[]
Params = pd.DataFrame(data = ret.x_iters,columns=x0sDF.index)
Params.loc[:,"fits"] = ret.func_vals
graph = plt.figure(figsize=(10,10))
pos = 1
for var in Params.columns:
    ax = graph.add_subplot(6,3,pos)
    plt.plot(Params.loc[:,var],-1*Params.loc[:,'fits'],'o')
    plt.title(var)
    pos+=1
# -

pd.DataFrame(index = paramNames, data =calcModelParamValues(ret.x))

# ### Next, fix parameters fitted for leaf size, extinction coeff and senescence, optimise RUE and partitioning params

# +
SimulationSet = ["LincolnRS2016IrrFullNit300"]#,"LincolnRS2016IrrFullNit50","LincolnRS2016IrrFullNit0"]

OptimisationVariables = ['FodderBeet.AboveGround.Wt',
                         'FodderBeet.Leaf.Live.Wt',
                         'FodderBeet.Petiole.Live.Wt',
                         'FodderBeet.StorageRoot.Live.Wt',
                         #'FodderBeet.Leaf.Canopy.CoverGreen',
                         #'FodderBeet.Leaf.Canopy.CoverTotal',
                         'FodderBeet.Leaf.Canopy.LAI']

x0sDF = pd.DataFrame(data=[['RUE',210],
                           ['StorageRoot_C_StructuralPriority',91],
                           ['StorageRoot_C_StoragePriority',186],
                           ['Leaf_C_StructuralPriority',61],
                           ['Leaf_C_StoragePriority',200],
                           ['Petiole_C_StructuralPriority',78],
                           ['Petiole_C_StoragePriority',200]],columns=['param','value']).set_index('param')
                           
boundsDF = pd.DataFrame(data=[['RUE',(180,220)],
                              ['StorageRoot_C_StructuralPriority',(0,200)],
                              ['StorageRoot_C_StoragePriority',(0,300)],
                              ['Leaf_C_StructuralPriority',(0,200)],
                              ['Leaf_C_StoragePriority',(0,300)],
                              ['Petiole_C_StructuralPriority',(0,200)],
                              ['Petiole_C_StoragePriority',(0,300)]],columns=['param','range']).set_index('param')

paramNames=["FodderBeet.Leaf.Photosynthesis.RUE.FixedValue", 
            "FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue", 
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]",
            "FodderBeet.Leaf.AreaLargestLeaf.FixedValue", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[3]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]",
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]",
            "FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"]

def calcModelParamValues(fP):
    mP = []
    mP.append(fP[0]/100)#"FodderBeet.Leaf.Photosynthesis.RUE.FixedValue"
    mP.append(.01)#"FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
    mP.append(0.76)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue"# 
    mP.append(1.0)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]"
    mP.append(0.091)#"FodderBeet.Leaf.AreaLargestLeaf.FixedValue"# 
    mP.append(66)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(68)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(1.0)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]"#
    mP.append(0.011)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]"
    mP.append(0.79)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]"
    mP.append(0.002)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]"
    mP.append(3.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]"
    mP.append(6.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]"
    mP.append(0.025)#"FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue"#
    mP.append(fP[1]/10)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(fP[1]/10)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(fP[2]/10)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(fP[3]/10)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(fP[3]/10)#"FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(fP[4]/10)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(fP[5]/10)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(fP[5]/10)#"FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(fP[6]/10)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#                        
    
    return mP

Preparefile(Path+'.apsimx')
x0 = x0sDF.value.values.tolist()
bounds = boundsDF.range.values.tolist()

RandomCalls = 100
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./checkpoint.pkl", compress=9)
#CheckPoint = load("./checkpoint.pkl")
#x0 = CheckPoint.x_iters
#y0 = CheckPoint.func_vals
ret = gp_minimize(runFittingItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#y0=y0)
# -
ret

# + tags=[]
Params = pd.DataFrame(data = ret.x_iters,columns=x0sDF.index)
Params.loc[:,"fits"] = ret.func_vals
graph = plt.figure(figsize=(10,10))
pos = 1
for var in Params.columns:
    ax = graph.add_subplot(6,3,pos)
    plt.plot(Params.loc[:,var],-1*Params.loc[:,'fits'],'o')
    plt.title(var)
    pos+=1
# -
# ### Fit carbon partitioning factors

# +
SimulationSet = ["LincolnRS2016IrrFullNit300"]#,"LincolnRS2016IrrFullNit50","LincolnRS2016IrrFullNit0"]

OptimisationVariables = ['FodderBeet.AboveGround.Wt',
                         'FodderBeet.Leaf.Live.Wt',
                         'FodderBeet.Petiole.Live.Wt',
                         'FodderBeet.StorageRoot.Live.Wt',
                         #'FodderBeet.Leaf.Canopy.CoverGreen',
                         #'FodderBeet.Leaf.Canopy.CoverTotal',
                         'FodderBeet.Leaf.Canopy.LAI']

x0sDF = pd.DataFrame(data=[['RUE',22],
                           ['StorageRoot_C_StructuralPriority',10],
                           ['StorageRoot_C_StoragePriority',10],
                           ['Leaf_C_StructuralPriority',10],
                           ['Leaf_C_StoragePriority',10],
                           ['Petiole_C_StructuralPriority',10],
                           ['Petiole_C_StoragePriority',10]],columns=['param','value']).set_index('param')
                           
boundsDF = pd.DataFrame(data=[['RUE',(15,25)],
                              ['StorageRoot_C_StructuralPriority',(0,200)],
                              ['StorageRoot_C_StoragePriority',(0,200)],
                              ['Leaf_C_StructuralPriority',(0,200)],
                              ['Leaf_C_StoragePriority',(0,200)],
                              ['Petiole_C_StructuralPriority',(0,200)],
                              ['Petiole_C_StoragePriority',(0,200)]],columns=['param','range']).set_index('param')

paramNames=["FodderBeet.Leaf.Photosynthesis.RUE.FixedValue", 
            "FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue", 
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]",
            "FodderBeet.Leaf.AreaLargestLeaf.FixedValue", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[3]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]",
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]",
            "FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"]

def calcModelParamValues(fP):
    mP = []
    mP.append(1.97)#"FodderBeet.Leaf.Photosynthesis.RUE.FixedValue"
    mP.append(.01)#"FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
    mP.append(0.76)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue"# 
    mP.append(1.0)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]"
    mP.append(0.091)#"FodderBeet.Leaf.AreaLargestLeaf.FixedValue"# 
    mP.append(66)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(68)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(1.0)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]"#
    mP.append(0.011)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]"
    mP.append(0.79)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]"
    mP.append(0.002)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]"
    mP.append(3.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]"
    mP.append(6.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]"
    mP.append(0.025)#"FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue"#
    mP.append(10.1)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(10.1)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(15.7)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(7.7)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(7.7)#"FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(13.6)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(0.5)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(0.5)#"FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(6.9)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#                        
    
    return mP

Preparefile(Path+'.apsimx')
x0 = x0sDF.value.values.tolist()
bounds = boundsDF.range.values.tolist()

RandomCalls = 100
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./checkpoint.pkl", compress=9)
#CheckPoint = load("./checkpoint.pkl")
#x0 = CheckPoint.x_iters
#y0 = CheckPoint.func_vals
ret = gp_minimize(runFittingItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#y0=y0)
# -
ret

# ### Fit water stress responses

# +
OptimisationVariables = ['FodderBeet.AboveGround.Wt',
'FodderBeet.Leaf.Live.Wt',
'FodderBeet.Petiole.Live.Wt',
'FodderBeet.StorageRoot.Live.Wt',
'FodderBeet.AboveGround.N',
'FodderBeet.Leaf.Live.N',
'FodderBeet.Petiole.Live.N',
'FodderBeet.StorageRoot.Live.N',
'FodderBeet.Leaf.Canopy.CoverGreen',
'FodderBeet.Leaf.Canopy.CoverTotal',
'FodderBeet.Leaf.Canopy.LAI',
'Soil.SoilWater.SW(1)',
'Soil.SoilWater.SW(2)',
'Soil.SoilWater.SW(3)',
'Soil.SoilWater.SW(4)',
'Soil.SoilWater.SW(5)',
'Soil.SoilWater.SW(6)',
'Soil.SoilWater.SW(7)',
'ProfileWater']

x0sDF = pd.DataFrame(data=[['RUE_Stress_Break',10],
                           ['k_fullStressValue',5],
                           ['k_Stress_Break',10],
                           ['LArea_lowerBreak',5],
                           ['LArea_upperBreak',10],
                           ['RFV',10],
                           ['Petiole_C_StoragePriority',10]],columns=['param','value']).set_index('param')
                           
boundsDF = pd.DataFrame(data=[['RUE',(15,25)],
                              ['StorageRoot_C_StructuralPriority',(0,200)],
                              ['StorageRoot_C_StoragePriority',(0,200)],
                              ['Leaf_C_StructuralPriority',(0,200)],
                              ['Leaf_C_StoragePriority',(0,200)],
                              ['RFV',(0,200)],
                              ['Petiole_C_StoragePriority',(0,200)]],columns=['param','range']).set_index('param')

paramNames=["FodderBeet.Leaf.Photosynthesis.RUE.FixedValue", 
            "FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue", 
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]",
            "FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]",
            "FodderBeet.Leaf.AreaLargestLeaf.FixedValue", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[3]", 
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[1]",
            "FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]",
            "FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]",
            "FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
            "FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
            "FodderBeet.Root.Network.RootFrontVelocity.PhaseLookupValue.Constant.FixedValue",
            "FodderBeet.Root.Network.NUptakeSWFactor.XYPairs.X[2]",
            "FodderBeet.Leaf.Canopy.Gsmax350",
            "FodderBeet.Leaf.Canopy.R50"]

def calcModelParamValues(fP):
    mP = []
    mP.append(1.97)#"FodderBeet.Leaf.Photosynthesis.RUE.FixedValue"
    mP.append(.01)#"FodderBeet.Leaf.Photosynthesis.FW.XYPairs.X[2]",
    mP.append(0.76)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue"# 
    mP.append(1.0)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X[2]"
    mP.append(0.091)#"FodderBeet.Leaf.AreaLargestLeaf.FixedValue"# 
    mP.append(66)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(68)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.RelativeArea.XYPairs.X[2]"# 
    mP.append(1.0)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.Y[1]"#
    mP.append(0.5)#"FodderBeet.Leaf.DeltaLAI.Vegetative.Delta.FW.XYPairs.X[2]"#
    mP.append(0.011)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.Y[3]"
    mP.append(0.79)#"FodderBeet.Leaf.SenescenceRateFunction.CoverEffect.XYPairs.X[2]"
    mP.append(0.002)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.Y[3]"
    mP.append(3.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[2]"
    mP.append(6.7)#"FodderBeet.Leaf.SenescenceRateFunction.AgeEffect.XYPairs.X[3]"
    mP.append(0.025)#"FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue"#
    mP.append(10.1)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(10.1)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(15.7)#"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(7.7)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(7.7)#"FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(13.6)#"FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(0.5)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(0.5)#"FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(6.9)#"FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue"#
    mP.append(1.0)#"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"#                        
    
    return mP

Preparefile(Path+'.apsimx')
x0 = x0sDF.value.values.tolist()
bounds = boundsDF.range.values.tolist()

RandomCalls = 100
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./checkpoint.pkl", compress=9)
#CheckPoint = load("./checkpoint.pkl")
#x0 = CheckPoint.x_iters
#y0 = CheckPoint.func_vals
ret = gp_minimize(runFittingItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#y0=y0)
# +
OptimisationVariables = ['FodderBeet.AboveGround.Wt',
'FodderBeet.Leaf.Live.Wt',
'FodderBeet.Petiole.Live.Wt',
'FodderBeet.StorageRoot.Live.Wt',
'FodderBeet.AboveGround.N',
'FodderBeet.Leaf.Live.N',
'FodderBeet.Petiole.Live.N',
'FodderBeet.StorageRoot.Live.N',
'FodderBeet.Leaf.Live.NConc',
'FodderBeet.Petiole.Live.NConc',
'FodderBeet.StorageRoot.Live.NConc',
'FodderBeet.Leaf.Canopy.CoverGreen',
'FodderBeet.Leaf.Canopy.LAI']

#divide by 10 for model

x0sDF = pd.DataFrame(data=[['LeafMinNconc',10],
    ['StorageRoot_C_StoragePriority',10],
['StorageRoot_N_StoragePriority',10],
['Leaf_C_StructuralPriority',10],
['Leaf_C_StoragePriority',10],
['Leaf_N_StructalPriority',10],
['Leaf_N_StoragePriority',10],
['Petiole_C_StructuralPriority',10],
['Petiole_C_StoragePriority',10],
['Petiole_N_StructuralPriority',10],
['Petiole_N_StoragePriority',10]],columns=['param','value']).set_index('param')

boundsDF = pd.DataFrame(data=[['LeafMinNconc',(1,25)],
                              ['StorageRoot_C_StoragePriority',(0,100)],
['StorageRoot_N_StoragelPriority',(0,200)],
['Leaf_C_StructuralPriority',(0,200)],
['Leaf_C_StoragePriority',(0,200)],
['Leaf_N_StructalPriority',(0,200)],
['Leaf_N_StoragePriority',(0,200)],
['Petiole_C_StructuralPriority',(0,200)],
['Petiole_C_StoragePriority',(0,200)],
['Petiole_N_StructuralPriority',(0,200)],
['Petiole_N_StoragePriority',(0,200)]],columns=['param','range']).set_index('param')

paramNames=["FodderBeet.Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue",
    "FodderBeet.StorageRoot.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.StorageRoot.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.StorageRoot.Carbon.DemandFunctions.QStoragePriority.FixedValue",
"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.StorageRoot.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
"FodderBeet.Leaf.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.Leaf.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.Leaf.Carbon.DemandFunctions.QStoragePriority.FixedValue",
"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.Leaf.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.Leaf.Nitrogen.DemandFunctions.QStoragePriority.FixedValue",
"FodderBeet.Petiole.Carbon.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.Petiole.Carbon.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.Petiole.Carbon.DemandFunctions.QStoragePriority.FixedValue",
"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStructuralPriority.FixedValue",
"FodderBeet.Petiole.Nitrogen.DemandFunctions.QMetabolicPriority.FixedValue",
"FodderBeet.Petiole.Nitrogen.DemandFunctions.QStoragePriority.FixedValue"]

def calcModelParamValues(fittingparams):
    MinLN = fittingparams[0]/1000
    
    SRCstr = 1
    SRCmet = 1
    SRCsto = 1 * fittingparams[1]/10
    
    SRNstr = 1
    SRNmet = 1
    SRNsto = 1 * fittingparams[2]/10
    
    LCstr =  1 * fittingparams[3]/10
    LCmet =  LCstr
    LCsto =  1 * fittingparams[4]/10
    
    LNstr =  1 * fittingparams[5]/10
    LNmet =  LNstr
    LNsto =  1 * fittingparams[6]/10
    
    PCstr =  1 * fittingparams[7]/10
    PCmet =  PCstr
    PCsto =  1 * fittingparams[8]/10
    
    PNstr =  1 * fittingparams[9]
    PNmet =  PNstr
    PNsto =  1 * fittingparams[10]/10
    
    return [MinLN, SRCstr, SRCmet, SRCsto, SRNstr, SRNmet, SRNsto,
                   LCstr,  LCmet,  LCsto,  LNstr,  LNmet,  LNsto,
                   PCstr,  PCmet,  PCsto,  PNstr,  PNmet,  PNsto]

Preparefile(Path+'.apsimx')

x0 = x0sDF.value.values.tolist()
bounds = boundsDF.range.values.tolist()

RandomCalls = 100
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./checkpoint.pkl", compress=9)
#CheckPoint = load("./checkpoint.pkl")
#x0 = CheckPoint.x_iters
#y0 = CheckPoint.func_vals
ret = gp_minimize(runPartitingModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#y0=y0)
# -
x0sDF

pd.DataFrame(data=calcModelParamValues(ret.x),index=paramNames)

plt.plot(-ret.func_vals)
plt.ylim(0,1)

from skopt.plots import plot_convergence
plot_convergence(ret);
plt.ylim(-1,0)


from skopt.plots import plot_objective
plot_objective(ret)#,minimum='expected_minimum')

Graph=plt.figure(figsize=(18,18))
Params = ['LDbase','PpSens']
#Params = ['MinLN','VS','PPS','SDVS','LDHtt','Hpp']
threshold = ParamCombs.NSE.min() * .8
goodFits = ParamCombs.loc[ParamCombs.NSE<threshold]#.sort_values('NSE',inplace=True)
parampos = pd.DataFrame(index=Params,data=[0,1],columns=['pos'])
for p in Params:
    rowpos = parampos.loc[p,'pos']
    cParams = Params.copy()
    cParams.remove(p)
    for c in cParams:
        colpos = parampos.loc[c,'pos']
        pos = rowpos * 6 + colpos +1
        ax = Graph.add_subplot(6,6,pos)
        plt.plot(goodFits.loc[:,c],goodFits.loc[:,p],'o',color='g')
        plt.ylabel(p)
        plt.xlabel(c)


OrderedFits = goodFits.sort_values('NSE')
OrderedFits.loc[:,'Rank'] = range(1,OrderedFits.index.size+1)

OrderedFits

Graph=plt.figure(figsize=(18,5))
pos=1
for p in Params:
    ax = Graph.add_subplot(1,6,pos)
    plt.plot(OrderedFits.loc[:,'Rank'],OrderedFits.loc[:,p],'-o')
    plt.xlabel('Rank')
    plt.text(0.05,0.9,p,transform=ax.transAxes,fontsize=14)
    pos+=1

RoundedTopFits = OrderedFits.iloc[:1:].mean(axis=0).round(1)
print('LV = '+ str(RoundedTopFits['MinLN']))
print('LN = ' + str(RoundedTopFits['MinLN']+RoundedTopFits['VS']))
print('SV = ' + str(RoundedTopFits['MinLN']+RoundedTopFits['PPS']))
print('SN = ' + str(RoundedTopFits['MinLN']+RoundedTopFits['VS']+RoundedTopFits['PPS']+RoundedTopFits['SDVS']))

RoundedTopFits

bnds = [(RoundedTopFits['MinLN']-2,RoundedTopFits['MinLN']+2),
          (RoundedTopFits['VS']-2,RoundedTopFits['VS']+2),
          (RoundedTopFits['PPS']-2,RoundedTopFits['PPS']+2),
          (RoundedTopFits['SDVS']-2,RoundedTopFits['SDVS']+2)]
xinit = [RoundedTopFits['MinLN'],RoundedTopFits['VS'],RoundedTopFits['PPS'],RoundedTopFits['SDVS']]
ret = scipy.optimize.minimize(runFLNModelItter,x0 = xinit,bounds=bnds)

#FittingVariables = ['Wheat.Phenology.HeadingDAS','Wheat.Phenology.FloweringDAS']
FittingVariables = ['Wheat.Phenology.FinalLeafNumber','Wheat.Phenology.FlagLeafDAS']
Cultivar='MacKellar'
OptimisationVariables = ['Predicted.'+x for x in FittingVariables]
DataPresent = pd.Series(index = BaseLine.index,dtype=bool)
DataPresent = False
for v in OptimisationVariables:
    DataPresent = (DataPresent | ~np.isnan(pd.to_numeric(BaseLine.loc[:,v])))
SetFilter = (BaseLine.Cultivar==Cultivar) & DataPresent
SimulationSet = BaseLine.loc[SetFilter,'SimulationName'].values
SimSet = makeLongString(SimulationSet)
SimSet

BaseLine.loc[BaseLine.Cultivar=='MacKellar',['Predicted.Wheat.Phenology.FlagLeafDAS','Observed.Wheat.Phenology.FlagLeafDAS']]

Cultivars = BaseLine.loc[BaseLine.Country=='New Zealand'].Cultivar.drop_duplicates().values

DataCounts = pd.DataFrame(index=Cultivars)
FittingVariables = ['Wheat.Phenology.FinalLeafNumber','Wheat.Phenology.FlagLeafDAS']
for c in Cultivars:
    for v in FittingVariables:
        DataCounts.loc[c,v] =  BaseLine.loc[BaseLine.Cultivar==c,'Observed.'+v].count()
DataCounts

BaseLine
