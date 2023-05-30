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

import winsound
frequency = 2500  # Set Frequency To 2500 Hertz
duration = 1000  # Set Duration To 1000 ms == 1 second
# %matplotlib inline

# +
WheatFilePath = "C:/GitHubRepos/ApsimX/Tests/Validation/Wheat/Wheat.apsimx"

BaseLine = pd.read_pickle('./BaselineHarvestObsPred.pkl')

DailyBaseLine = pd.read_pickle('./BaselineDailyObsPred.pkl')

BZOptimised = pd.read_pickle('./CAMP_optimisedBZHarvestObsPred.pkl')

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
    with open(filePath,'r') as WheatApsimxJSON:
        WheatApsimx = json.load(WheatApsimxJSON)
        WheatApsimxJSON.close()
    ## Remove old prameterSet manager in replacements
    removeModel(WheatApsimx,'Replacements.SetCropParameters')

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

    AppendModeltoModelofType(WheatApsimx,'Models.Core.Replacements, Models',SetCropParams)

    with open(filePath,'w') as WheatApsimxJSON:
        json.dump(WheatApsimx,WheatApsimxJSON,indent=2)
        
def makeLongString(SimulationSet):
    longString =  '/SimulationNameRegexPattern:"'
    longString =  longString + '(' + SimulationSet[0]  + ')|' # Add first on on twice as apsim doesn't run the first in the list
    for sim in SimulationSet[:]:
        longString = longString + '(' + sim + ')|'
    longString = longString + '(' + SimulationSet[-1] + ')|' ## Add Last on on twice as apsim doesnt run the last in the list
    longString = longString + '(' + SimulationSet[-1] + ')"'
    return longString

def CalcScaledValue(Value,RMax,RMin):
    return (Value - RMin)/(RMax-RMin)
# +
def Preparefile(filePath,freq):
    ## revert .apximx file to master
#     !del C:\GitHubRepos\ApsimX\Tests\Validation\Wheat\Wheat.db
    command= "git --git-dir=C:/GitHubRepos/ApsimX/.git --work-tree=C:/GitHubRepos/ApsimX checkout " + filePath 
    comm=shlex.split(command) # This will convert the command into list format
    subprocess.run(comm, shell=True) 
    ## Add blank manager into each simulation
    with open(filePath,'r') as WheatApsimxJSON:
        WheatApsimx = json.load(WheatApsimxJSON)
    WheatApsimxJSON.close()
    if freq == 'Harvest':
        #Stop Daily reporting
        StopReporting(WheatApsimx,'Replacements.DailyReport')
        removeModel(WheatApsimx,'DataStore.DailyObsPred')
    else:
        if freq != 'Daily':
            print('Only works with Daily or Harvest frequencies')
    AppendModeltoModelofTypeAndDeleteOldIfPresent(WheatApsimx,'Models.Core.Zone, Models',BlankManager)
    with open(filePath,'w') as WheatApsimxJSON:
        json.dump(WheatApsimx,WheatApsimxJSON,indent=2)
    
def runModelItter(paramNames,paramValues,FittingVariables,Cultivar,DataTable,freq):        
    ApplyParamReplacementSet(paramValues,paramNames,'C:/GitHubRepos/ApsimX/Tests/Validation/Wheat/Wheat.apsimx')
    OptimisationVariables = ['Observed.'+x for x in FittingVariables]
    DataPresent = pd.Series(index = DataTable.index,dtype=bool)
    DataPresent = False
    for v in OptimisationVariables:
        DataPresent = (DataPresent | ~np.isnan(pd.to_numeric(DataTable.loc[:,v])))
    SetFilter = (DataTable.Cultivar==Cultivar) & DataPresent
    SimulationSet = DataTable.loc[SetFilter,'SimulationName'].values
    SimSet = makeLongString(SimulationSet)
    #print(SimSet)
    start = dt.datetime.now()
    subprocess.run(['C:/GitHubRepos/ApsimX/bin/Debug/netcoreapp3.1/Models.exe',
                    'C:/GitHubRepos/ApsimX/Tests/Validation/Wheat/Wheat.apsimx',
                    SimSet], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    endrun = dt.datetime.now()
    runtime = (endrun-start).seconds
    con = sqlite3.connect(r'C:\GitHubRepos\ApsimX\Tests\Validation\Wheat\Wheat.db')
    if freq == 'Harvest':
        ObsPred = pd.read_sql("Select * from HarvestObsPred",con)
    else:
        if freq == 'Daily':
            ObsPred = pd.read_sql("Select * from DailyObsPred",con)
        else:
            print('Only works with Daily or Harvest DataTables')
    con.close()
    ScObsPre = pd.DataFrame(columns = ['ScObs','ScPred','Var','SimulationID'])
    indloc = 0
    for var in FittingVariables:
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
    print(str(paramValues) + " run completed " +str(RegStats.n) + ' sims in ' + str(runtime) + ' seconds.  NSE = '+str(RegStats.NSE))
    return retVal


# -

BZFits = pd.read_excel('C:/GitHubRepos/npi/Simulation/ModelFitting/FinalNPIFitting.xlsx',sheet_name='LNParams',index_col=0,skiprows=3)
paramNames = ['[Phenology].CAMP.FLNparams.LV', 
              '[Phenology].CAMP.FLNparams.LN', 
              '[Phenology].CAMP.FLNparams.SV', 
              '[Phenology].CAMP.FLNparams.SN',
              '[Phenology].HeadEmergenceLongDayBase.FixedValue',
              '[Phenology].HeadEmergencePpSensitivity.FixedValue']
Cultivars = BZFits.index.values
FittingVariables = ['Wheat.Phenology.FinalLeafNumber','Wheat.Phenology.FlagLeafDAS','Wheat.Phenology.HeadingDAS',
                                'Wheat.Phenology.FloweringDAS']     
GNFits = pd.DataFrame(index = Cultivars,columns=paramNames)

BZFits

# +
c='Gregory'
Preparefile(WheatFilePath,'Harvest')
def runDevModelItter(Devparams):
    LV = Devparams[0]/10
    LN = Devparams[0]/10 + Devparams[1]/10
    SV = Devparams[0]/10 + Devparams[2]/10
    SN = Devparams[0]/10 + Devparams[1]/10 + Devparams[2]/10 + Devparams[3]/10
    paramValues = [LV,LN,SV,SN,Devparams[4],Devparams[5]/10]
    boundsPass = True
    if (LN > 30):
        boundsPass = False
    if (SV > 25):
        boundsPass = False
    if (SN > 35):
        boundsPass = False
    if (SN < LV):
        boundsPass = False
    if boundsPass == False:
        print (str(paramValues) + " gave out of bounds parameters ")
        retVal = 1
    else:
        retVal = runModelItter(paramNames,paramValues,FittingVariables,c,BaseLine,'Harvest')
    return retVal

x0 = [int(x) for x in BZFits.loc[c,['MinLN','VS','PPS','SDVS','LDB','HPPR']].values]
bounds = [(50,100),
          (0,120),
          (0,80),
          (-80,80),
          (50,500),
          (0,80)]

runDevModelItter([70, 30, 15, 10, 150, 0.0])

RandomCalls = 25
OptimizerCalls = 20
TotalCalls = RandomCalls + OptimizerCalls
#try:
checkpoint_saver = CheckpointSaver("./"+c+"testFits_checkpoint.pkl", compress=9)
CheckPoint = load("./"+c+"Fits_checkpoint.pkl")
x0 = CheckPoint.x_iters
y0 = CheckPoint.func_vals
#if (-CheckPoint.fun < 0.75):
#    try:
ret = gp_minimize(runDevModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
              initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0,y0=y0)
        # ParamCombs = pd.DataFrame(ret.x_iters,columns = paramNames)
        # ParamCombs.loc[:,'NSE'] = ret.func_vals
        # Graph = plt.figure(figsize=(10,3))
        # pos=1
        # for p in paramNames:
        #     ax = Graph.add_subplot(6,1,pos)
        #     plt.plot(ParamCombs.loc[:,p],-ParamCombs.loc[:,'NSE'],'o',color='k')
        #     bestFit = ParamCombs.loc[:,'NSE'].idxmin()
        #     plt.plot(ParamCombs.loc[bestFit,p],-ParamCombs.loc[bestFit,'NSE'],'o',color='cyan',ms=8,mec='k',mew=2)
        #     GNFits.loc[c,p] = ParamCombs.loc[bestFit,p]
        #     GNFits.loc[c,'GN_NSE'] = ParamCombs.loc[bestFit,'NSE']
        #     GNFits.loc[c,'BZ_NSE'] = ret.func_vals[0]
        #     pos+=1
        #     plt.ylabel('NSE')
        #     plt.xlabel(p)
        #     plt.ylim(0,1)
#         except:
#             print(c+' failed')
#     else:
#         print("fits nse already greater than 0.75")
# except:
#     print(c+' has no checkpoint')
# -

PoorFits = ['Axe', 'Batavia','Beaufort',
       'Calingiri', 'Crusader', 'Csirow003', 'Csirow005', 'Csirow011',
       'Csirow018', 'Csirow023', 'Csirow027', 'Csirow087', 'Cunningham',
       'Cutlass', 'Eaglehawk', 'Egret', 'Ellison', 'Forrest',
       'Gregory', 'Kellalac', 'Mace', 'Magenta',
       'Manning', 'Merinda', 'Rongotea', 'Scepter', 'Scout', 'Spitfire',
       'Suneca', 'Sunlamb', 'Sunstate', 'Suntop', 'Trojan', 'Wills']
for c in PoorFits:
    print(c)
    Preparefile(WheatFilePath,'Harvest')

    def runDevModelItter(Devparams):
        LV = Devparams[0]/10
        LN = Devparams[0]/10 + Devparams[1]/10
        SV = Devparams[0]/10 + Devparams[2]/10
        SN = Devparams[0]/10 + Devparams[1]/10 + Devparams[2]/10 + Devparams[3]/10
        paramValues = [LV,LN,SV,SN,Devparams[4],Devparams[5]/10]
        boundsPass = True
        if (LN > 30):
            boundsPass = False
        if (SV > 25):
            boundsPass = False
        if (SN > 35):
            boundsPass = False
        if (SN < LV):
            boundsPass = False
        if boundsPass == False:
            print (str(paramValues) + " gave out of bounds parameters ")
            retVal = 1
        else:
            retVal = runModelItter(paramNames,paramValues,FittingVariables,c,BaseLine,'Harvest')
        return retVal

    x0 = [int(x) for x in BZFits.loc[c,['MinLN','VS','PPS','SDVS','LDB','HPPR']].values]
    bounds = [(50,100),
              (0,120),
              (0,80),
              (-80,80),
              (50,500),
              (0,80)]

    RandomCalls = 25
    OptimizerCalls = 20
    TotalCalls = RandomCalls + OptimizerCalls
    try:
        checkpoint_saver = CheckpointSaver("./"+c+"Fits_checkpoint.pkl", compress=9)
        #CheckPoint = load("./"+c+"Fits_checkpoint.pkl")
        #x0 = CheckPoint.x_iters
        #y0 = CheckPoint.func_vals
        #if (-CheckPoint.fun < 0.75):
        ret = gp_minimize(runDevModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,
                          initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)
        # ParamCombs = pd.DataFrame(ret.x_iters,columns = paramNames)
        # ParamCombs.loc[:,'NSE'] = ret.func_vals
        # Graph = plt.figure(figsize=(10,3))
        # pos=1
        # for p in paramNames:
        #     ax = Graph.add_subplot(6,1,pos)
        #     plt.plot(ParamCombs.loc[:,p],-ParamCombs.loc[:,'NSE'],'o',color='k')
        #     bestFit = ParamCombs.loc[:,'NSE'].idxmin()
        #     plt.plot(ParamCombs.loc[bestFit,p],-ParamCombs.loc[bestFit,'NSE'],'o',color='cyan',ms=8,mec='k',mew=2)
        #     GNFits.loc[c,p] = ParamCombs.loc[bestFit,p]
        #     GNFits.loc[c,'GN_NSE'] = ParamCombs.loc[bestFit,'NSE']
        #     GNFits.loc[c,'BZ_NSE'] = ret.func_vals[0]
        #     pos+=1
        #     plt.ylabel('NSE')
        #     plt.xlabel(p)
        #     plt.ylim(0,1)
    except:
        print(c+' failed')
        # else:
        #     print("fits nse already greater than 0.75")
    # except:
    #     print(c+' has no checkpoint')
# +
Preparefile(WheatFilePath,'Harvest')

def runGrainModelItter(Grainparams):
    paramNames = ['[Grain].NumberFunction.GrainNumber.GrainsPerGramOfStem.FixedValue',
                  '[Grain].MaximumPotentialGrainSize.FixedValue']  
    FittingVariables = ['Wheat.Grain.Number','Wheat.Grain.Size','Wheat.Grain.Wt']
    Cultivar = 'Wakanui'
    return runModelItter(paramNames,Grainparams,FittingVariables,Cultivar,DailyBaseLine,'Daily')

Cultivar = 'Wakanui'
bounds = [(10,50),
          (0.01,0.07)]
RandomCalls = 16
OptimizerCalls = 14
TotalCalls = RandomCalls + OptimizerCalls
checkpoint_saver = CheckpointSaver("./"+Cultivar+"Grain_checkpoint.pkl", compress=9)
# CheckPoint = load("./"+Cultivar+"checkpoint.pkl")
# x0 = CheckPoint.x_iters
# y0 = CheckPoint.func_vals

ret = gp_minimize(runGrainModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,initial_point_generator='sobol',callback=[checkpoint_saver])#,x0=x0,y0=y0)

winsound.Beep(frequency, duration)
# +
Preparefile(WheatFilePath,'Daily')

def runLeafModelItter(Leafparams):
    paramNames = ['[Phenology].Phyllochron.BasePhyllochron.FixedValue',
                  '[Phenology].PhyllochronPpSensitivity.FixedValue']  
    FittingVariables = ['Wheat.Phenology.HaunStage','Wheat.Structure.LeafTipsAppeared','Wheat.Leaf.AppearedCohortNo']
    Cultivar = 'Hartog'
    return runModelItter(paramNames,Leafparams,FittingVariables,Cultivar,DailyBaseLine,'Daily')

Cultivar = 'Hartog'
bounds = [(70,120),
          (0.2,0.7)]
RandomCalls = 16
OptimizerCalls = 14
TotalCalls = RandomCalls + OptimizerCalls
checkpoint_saver = CheckpointSaver("./"+Cultivar+"Grain_checkpoint.pkl", compress=9)
# CheckPoint = load("./"+Cultivar+"checkpoint.pkl")
# x0 = CheckPoint.x_iters
# y0 = CheckPoint.func_vals

ret = gp_minimize(runLeafModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,initial_point_generator='sobol',callback=[checkpoint_saver])#,x0=x0,y0=y0)

winsound.Beep(frequency, duration)
# +
Preparefile(WheatFilePath,'Harvest')

def runAnthModelItter(FLNparams):
    LV = FLNparams[0]/10
    LN = FLNparams[0]/10 + FLNparams[1]/10
    SV = FLNparams[0]/10 + FLNparams[2]/10
    SN = FLNparams[0]/10 + FLNparams[1]/10 + FLNparams[2]/10 + FLNparams[3]/10
    paramValues = [LV,LN,SV,SN,FLNparams[4],FLNparams[5]/10,6,60]
    boundsPass = True
    if (LN > 30):
        boundsPass = False
    if (SV > 25):
        boundsPass = False
    if (SN > 35):
        boundsPass = False
    if (SN < LV):
        boundsPass = False
    if boundsPass == False:
        print (str(paramValues) + " gave out of bounds parameters ")
        retVal = 1
    else:
        paramNames = ['[Phenology].CAMP.FLNparams.LV', 
              '[Phenology].CAMP.FLNparams.LN', 
              '[Phenology].CAMP.FLNparams.SV', 
              '[Phenology].CAMP.FLNparams.SN',
              '[Phenology].HeadEmergenceLongDayBase.FixedValue',
              '[Phenology].HeadEmergencePpSensitivity.FixedValue',
              '[Phenology].CAMP.EnvData.VrnTreatTemp',
              '[Phenology].CAMP.EnvData.VrnTreatDuration']  
        FittingVariables = ['Wheat.Phenology.FinalLeafNumber','Wheat.Phenology.FlagLeafDAS','Wheat.Phenology.HeadingDAS',
                            'Wheat.Phenology.FloweringDAS']
        Cultivar = 'Hartog'
        retVal = runModelItter(paramNames,paramValues,FittingVariables,Cultivar)
    return retVal

Cultivar = 'Hartog'
     #/10,/10,/10,/10,/16/10
#x0 = [80,25,45815,230,5]
bounds = [(50,100),
          (15,50),
          (0,60),
          (-50,50),
          (50,400),
          (0,80)]
#x0 = [55,0,42,60,83,3]
# bounds = [(5.00,15.00),
#           (0.00,20.00),
#           (0.00,10.00),
#           (-10.00,10.00)]

RandomCalls = 36
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls
checkpoint_saver = CheckpointSaver("./"+Cultivar+"checkpoint.pkl", compress=9)
# CheckPoint = load("./"+Cultivar+"checkpoint.pkl")
# x0 = CheckPoint.x_iters
# y0 = CheckPoint.func_vals

ret = gp_minimize(runAnthModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,initial_point_generator='sobol',callback=[checkpoint_saver])#,x0=x0,y0=y0)

winsound.Beep(frequency, duration)
# +
Preparefile(WheatFilePath)

def runFLNModelItter(FLNparams):
    LV = FLNparams[0]/10
    LN = FLNparams[0]/10 + FLNparams[1]/10
    SV = FLNparams[0]/10 + FLNparams[2]/10
    SN = FLNparams[0]/10 + FLNparams[1]/10 + FLNparams[2]/10 + FLNparams[3]/10
    paramValues = [LV,LN,SV,SN,6,60]
    boundsPass = True
    if (LN > 30):
        boundsPass = False
    if (SV > 25):
        boundsPass = False
    if (SN > 35):
        boundsPass = False
    if boundsPass == False:
        print (str(paramValues) + " gave out of bounds parameters ")
        retVal = 100
    else:
        paramNames = ['[Phenology].CAMP.FLNparams.LV', 
              '[Phenology].CAMP.FLNparams.LN', 
              '[Phenology].CAMP.FLNparams.SV', 
              '[Phenology].CAMP.FLNparams.SN',
              '[Phenology].CAMP.EnvData.VrnTreatTemp',
              '[Phenology].CAMP.EnvData.VrnTreatDuration']  
        FittingVariables = ['Wheat.Phenology.FinalLeafNumber','Wheat.Phenology.FlagLeafDAS']
        Cultivar = 'MacKellar'
        retVal = runModelItter(paramNames,paramValues,FittingVariables,Cultivar)
    return retVal

#x0 = [70,0,50,0]
bounds = [(x0[0]20,x0[0]+30),
          (x0[1],x0[1]+80),
          (x0[2]-50,x0[2]+50),
          (x0[3]-60,x0[3]+60)]

# bounds = [(5.00,15.00),
#           (0.00,20.00),
#           (0.00,10.00),
#           (-10.00,10.00)]

RandomCalls = 4*3
OptimizerCalls = 30
TotalCalls = RandomCalls + OptimizerCalls

checkpoint_saver = CheckpointSaver("./FLNcheckpoint.pkl", compress=9)
# from skopt import load
# CheckPoint = load('./FLNcheckpoint.pkl')
# x0 = ret.x_iters
# y0 = ret.func_vals

ret = gp_minimize(runFLNModelItter, bounds, n_calls=TotalCalls,n_initial_points=RandomCalls,initial_point_generator='sobol',callback=[checkpoint_saver],x0=x0)#,y0=y0)

winsound.Beep(frequency, duration)

# +
Preparefile(WheatFilePath)

def runHeadModelItter(Headparams)
    AllParams = [11,15.5,19.1,28] + Headparams + [6,60]
    paramNames = ['[Phenology].CAMP.FLNparams.LV', 
              '[Phenology].CAMP.FLNparams.LN', 
              '[Phenology].CAMP.FLNparams.SV', 
              '[Phenology].CAMP.FLNparams.SN',
              '[Phenology].HeadEmergenceLongDayBase.FixedValue',
              '[Phenology].HeadEmergencePpSensitivity.FixedValue',
              '[Phenology].CAMP.EnvData.VrnTreatTemp',
              '[Phenology].CAMP.EnvData.VrnTreatDuration']  
    FittingVariables = ['Wheat.Phenology.HeadingDAS','Wheat.Phenology.FloweringDAS']
    Cultivar = 'Wakanui'
    retVal = runModelItter(paramNames,AllParams,FittingVariables,Cultivar)
    return retVal

x0 = [90, 7.920653097461647]
bounds = [(50,200),
          (6.0,12.0)]

checkpoint_saver = CheckpointSaver("./HDcheckpoint.pkl", compress=9)
#from skopt import load
#CheckPoint = load('./HDcheckpoint.pkl')

RandomCalls = 2*3
OptimizerCalls = 10
TotalCalls = RandomCalls + OptimizerCalls

ret = gp_minimize(runHeadModelItter, bounds, n_calls=TotalCalls, random_state=0, callback=[checkpoint_saver], x0=x0)#,n_initial_points=RandomCalls,initial_point_generator='sobol',x0=x0)#,y0=y0)

winsound.Beep(frequency, duration)

# + tags=[]
ret = load("./Gregorycheckpoint.pkl")
# -

ret#.x_iters[0]

ParamCombs.loc[63,:]

Params =  ['[Phenology].CAMP.FLNparams.LV', 
              '[Phenology].CAMP.FLNparams.LN', 
              '[Phenology].CAMP.FLNparams.SV', 
              '[Phenology].CAMP.FLNparams.SN',
              '[Phenology].HeadEmergenceLongDayBase.FixedValue',
              '[Phenology].HeadEmergencePpSensitivity.FixedValue']  
ShortParams = pd.Series(index=Params,data=['MinLN','VS','PPS','SDVS','LDB','HPPS'])
Fits = pd.DataFrame(index = Cultivars, columns = ['NSE'])
Graph = plt.figure(figsize=(10,100))
pos=1
for c in Cultivars:
    try:
        ret = load("./"+c+"Fits_checkpoint.pkl")
        Fits.loc[c,'NSE'] = -ret.fun
        ParamCombs = pd.DataFrame(ret.x_iters,columns = Params)
        ParamCombs.loc[:,'NSE'] = ret.func_vals
        for p in Params:
            ax = Graph.add_subplot(60,6,pos)
            plt.plot(ParamCombs.loc[:,p],-ParamCombs.loc[:,'NSE'],'o',color='k')
            bestFit = ParamCombs.loc[:,'NSE'].idxmin()
            plt.plot(ParamCombs.loc[bestFit,p],-ParamCombs.loc[bestFit,'NSE'],'o',color='cyan',ms=8,mec='k',mew=2)
            pos+=1
            if p == '[Phenology].CAMP.FLNparams.LV':
                plt.ylabel(c)
            else:
                ax.axes.yaxis.set_visible(False)
            plt.xlabel(ShortParams[p])
            plt.ylim(0,1)
    except:
        print("no fits for " + c)

len(Fits.dropna())

(Fits.NSE > 0.75).sum()

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
