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
from skopt.plots import plot_convergence
import re

from py_expression_eval import Parser
parser = Parser()

import winsound
frequency = 2500  # Set Frequency To 2500 Hertz
duration = 1000  # Set Duration To 1000 ms == 1 second
# %matplotlib inline

pd.set_option('display.max_rows', 100)

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

def replaceModel(Parent,modelPath,New):
    PathElements = modelPath.split('.')
    if PathElements[-1][-1] != "]":
        findModel(Parent,PathElements[:-1])[PathElements[-1]] = New
    else:
        findModel(Parent,PathElements[:-1])[PathElements[-1][0]][int(PathElements[-1][-2])-1] = New

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

    ## Add crop coefficient overwrite into replacements
    for p in range(len(paramValues)):
        replaceModel(Apsimx,
                     paramNames[p],
                     paramValues[p])

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
def Preparefile():
#     !del C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise.db

    
def runModelItter(paramSet,OptimisationVariables,SimulationSet,paramsTried):        
    paramAddresses = [ParamData.loc[x,'Address'] for x in paramSet.index]
    absoluteParamValues = deriveIfRelativeTo(paramSet)
    ApplyParamReplacementSet(absoluteParamValues,paramAddresses,Path+'.apsimx') # write parameter set into model
    start = dt.datetime.now()
    simSet = makeLongString(SimulationSet) # make command string with simulations to run
    subprocess.run(['C:/GitHubRepos/ApsimX/bin/Debug/netcoreapp3.1/Models.exe',
                    Path+'.apsimx',
                   simSet], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=True)  # Run simulations
    endrun = dt.datetime.now()
    runtime = (endrun-start).seconds
    con = sqlite3.connect(r'C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise.db')
    try:
        ObsPred = pd.read_sql("Select * from PredictedObserved",con)  # read observed and predicted data
        con.close()
    except:
        con.close()
        print("Simulations must not have run as no data in PredictedObserved")
    DataSize = pd.DataFrame(VariableWeights.loc[OptimisationVariables,step])  #data frame with weighting for each variable
    DataSize.loc[:,'size'] =  [ObsPred.loc[:,"Observed."+v].dropna().size for v in OptimisationVariables] # add the data size for each variable
    DataSize.loc[:,'sizeBalance'] = [round(DataSize.loc[:,'size'].max()/DataSize.loc[v,'size']) for v in DataSize.index]  # add size wieghting for each variable
    DataSize.loc[:,'weighting'] = DataSize.loc[:,step] * DataSize.loc[:,'sizeBalance'] # Calculate overall weighting for each variable
    ScObsPre = pd.DataFrame(columns = ['ScObs','ScPred','Var','SimulationID'])  # make blank dataframe to put scalled obs pred values in
    indloc = 0
    for var in OptimisationVariables:
        weighting = DataSize.loc[var,'weighting']
        DataPairs = ObsPred.reindex(['Observed.'+var,'Predicted.'+var,'SimulationID'],axis=1).dropna() # slice out data we need for doing stats
        for c in DataPairs.columns:
            DataPairs.loc[:,c] = pd.to_numeric(DataPairs.loc[:,c])  # ensure all values are numeric, not objects
        VarMax = max(DataPairs.loc[:,'Observed.'+var].max(),DataPairs.loc[:,'Predicted.'+var].max())  # maximum for variable
        VarMin = min(DataPairs.loc[:,'Observed.'+var].min(),DataPairs.loc[:,'Predicted.'+var].min())  # minimum for variable
        DataPairs = pd.DataFrame(index = np.repeat(DataPairs.index,weighting),
                                  data = np.repeat(DataPairs.values,weighting ,axis=0),columns = DataPairs.columns)  # Replicate data to give required weighting
        DataPairs.reset_index(inplace=True) # make index unique
        for x in DataPairs.index:
            ScObsPre.loc[indloc,'ScObs'] = CalcScaledValue(DataPairs.loc[x,'Observed.'+var],VarMax,VarMin)  # Scale observed values between VarMin (0) and VarMax (1)
            ScObsPre.loc[indloc,'ScPred'] = CalcScaledValue(DataPairs.loc[x,'Predicted.'+var],VarMax,VarMin) # Scale predicted values between VarMin (0) and VarMax (1)
            ScObsPre.loc[indloc,'Var'] = var  # assign variable name for indexing
            ScObsPre.loc[indloc,'SimulationID'] = DataPairs.loc[x,'SimulationID'] # assign variable name for indexing
            indloc+=1
    RegStats = MUte.MathUtilities.CalcRegressionStats('LN',ScObsPre.loc[:,'ScPred'].values,ScObsPre.loc[:,'ScObs'].values)

    retVal = max(RegStats.NSE,0) *-1
    globals()["itteration"] += 1
    print("i" + str(globals()["itteration"] )+"  "+str(paramsTried) + " run completed " +str(len(SimulationSet)) + ' sims in ' + str(runtime) + ' seconds.  NSE = '+str(RegStats.NSE))
    return retVal

def runFittingItter(fittingParams):
    paramSetForItter = currentParamVals.copy() #Start off with full current param set
    fittingParamsDF = pd.Series(index = paramsToOptimise,data=fittingParams)
    for p in fittingParamsDF.index:
        paramSetForItter[p] = fittingParamsDF[p] #replace parameters being fitted with current itteration values
    return runModelItter(paramSetForItter,OptimisationVariables,SimulationSet,fittingParams)

def deriveIfRelativeTo(paramSet):
    derived = paramSet.copy()
    for p in paramSet.index:
          if RelativeTo[p] != 'nan': #for paramteters that reference another
            members = RelativeTo[p].split()
            if len(members) == 1:
                derived[p] = paramSet[members[0]] #update with current itterations value
            else:
                ref = paramSet.loc[members[0]]
                opp = members[1]
                expression = 'ref'+opp+'num'
                num = paramSet[p]
                derived[p] = parser.parse(expression).evaluate({'ref':ref,'num':num})
    return derived.values.tolist()

def runModelFullset(paramSet):      
    paramAddresses = [ParamData.loc[x,'Address'] for x in paramSet.index]
    absoluteParamValues = deriveIfRelativeTo(paramSet)
    ApplyParamReplacementSet(absoluteParamValues,paramAddresses,Path+'.apsimx')
    start = dt.datetime.now()
    subprocess.run(['C:/GitHubRepos/ApsimX/bin/Debug/netcoreapp3.1/Models.exe',
                    Path+'.apsimx'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    endrun = dt.datetime.now()
    runtime = (endrun-start).seconds
    print("all sims ran in " +str(runtime)+ " seconds")


# -

ParamData = pd.read_excel('OptimiseConfig.xlsx',sheet_name='ParamData',engine="openpyxl",index_col='Param')
SimSet = pd.read_excel('OptimiseConfig.xlsx',sheet_name='SimSet',engine="openpyxl")
VariableWeights = pd.read_excel('OptimiseConfig.xlsx',sheet_name='VariableWeights',engine="openpyxl",index_col='Variable')
OptimisationSteps = SimSet.columns.values.tolist()
paramsToOptimise = []
itteration = 0
best = 0

OptimisationSteps

bestParamVals = pd.Series(index = ParamData.index,data=ParamData.loc[:,'BestValue'])
bestParamVals

bounds = pd.Series(index= ParamData.index,
                   data = [(ParamData.loc[x,'Min_feasible'],ParamData.loc[x,'Max_feasible']) for x in ParamData.index])
bounds

RelativeTo = pd.Series(index = ParamData.index,data=ParamData.loc[:,'RelativeTo'],dtype=str)
RelativeTo

AbsoluteBestParams =  pd.Series(index = ParamData.index,data=deriveIfRelativeTo(bestParamVals))
AbsoluteBestParams

# +
# step = 'Potential canopy'
# OptimisationVariables = VariableWeights.loc[:,step].dropna().index.tolist()
# con = sqlite3.connect(r'C:\GitHubRepos\ApsimX\Prototypes\SimplifiedOrganArbitrator\FodderBeetOptimise.db')
# ObsPred = pd.read_sql("Select * from PredictedObserved",con)
# con.close()
# DataSize = pd.DataFrame(VariableWeights.loc[OptimisationVariables,step])
# DataSize.loc[:,'size'] =  [ObsPred.loc[:,"Observed."+v].dropna().size for v in OptimisationVariables]
# DataSize.loc[:,'sizeBalance'] = [round(DataSize.loc[:,'size'].max()/DataSize.loc[v,'size']) for v in DataSize.index]
# DataSize.loc[:,'weighting'] = DataSize.loc[:,step] * DataSize.loc[:,'sizeBalance']
# ScObsPre = pd.DataFrame(columns = ['ScObs','ScPred','Var','SimulationID'])
# indloc = 0
# for var in OptimisationVariables:
#     weighting = DataSize.loc[var,'weighting']
#     DataPairs = ObsPred.reindex(['Observed.'+var,'Predicted.'+var,'SimulationID'],axis=1).dropna()
#     DataPairs = pd.DataFrame(index = np.repeat(DataPairs.index,weighting),
#                               data = np.repeat(DataPairs.values,weighting ,axis=0),columns = DataPairs.columns)
#     DataPairs.reset_index(inplace=True)
#     for c in DataPairs.columns:
#         DataPairs.loc[:,c] = pd.to_numeric(DataPairs.loc[:,c])
#     VarMax = max(DataPairs.loc[:,'Observed.'+var].max(),DataPairs.loc[:,'Predicted.'+var].max())
#     VarMin = min(DataPairs.loc[:,'Observed.'+var].min(),DataPairs.loc[:,'Predicted.'+var].min())
#     for x in DataPairs.index:
#         ScObsPre.loc[indloc,'ScObs'] = CalcScaledValue(DataPairs.loc[x,'Observed.'+var],VarMax,VarMin)
#         ScObsPre.loc[indloc,'ScPred'] = CalcScaledValue(DataPairs.loc[x,'Predicted.'+var],VarMax,VarMin)
#         ScObsPre.loc[indloc,'Var'] = var
#         ScObsPre.loc[indloc,'SimulationID'] = DataPairs.loc[x,'SimulationID']
#         indloc+=1
#     varDat = ScObsPre.Var==var
#     vmarker = VariableWeights.loc[var,'marker']
#     vcolor = VariableWeights.loc[var,'color']
#     plt.plot(ScObsPre.loc[varDat,'ScObs'],ScObsPre.loc[varDat,'ScPred'],vmarker,color=vcolor,label=var[11:])
# RegStats = MUte.MathUtilities.CalcRegressionStats('LN',ScObsPre.loc[:,'ScPred'].values,ScObsPre.loc[:,'ScObs'].values)
# plt.legend(loc=(.05,1.05))
# plt.ylabel('sc Predicted')
# plt.xlabel('sc Observed')

# retVal = max(RegStats.NSE,0) *-1
# -

OptimisationSteps

SimulationSet

for step in OptimisationSteps[1:]:
    itteration = 0
    globals()["best"] = 0
    print(step + " Optimistion step")
    paramsToOptimise = ParamData.loc[ParamData.loc[:,step] == 'fit',step].index.values.tolist()
    print("fitting these parameters")
    print(paramsToOptimise)
    OptimisationVariables = VariableWeights.loc[:,step].dropna().index.values.tolist()
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
    
    Preparefile()

    RandomCalls = min(len(paramsToOptimise) * 10,50)
    print(str(RandomCalls)+" Random calls")
    OptimizerCalls = 25
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

runModelFullset(bestParamVals) #run simulations with current best fit params

# + tags=[]
for step in OptimisationSteps:
    ret = load("./"+step+"checkpoint.pkl")
    
    graph = plt.figure(figsize=(10,10))
    plot_convergence(ret);
    plt.ylim(-1,0)
    plt.title(step)

    paramsToOptimise = ParamData.loc[ParamData.loc[:,step] == 'fit',step].index.values.tolist()
    Params = pd.DataFrame(data = ret.x_iters,columns=paramsToOptimise)
    Params.loc[:,"fits"] = ret.func_vals
    Params.sort_values('fits',inplace=True)
    graph = plt.figure(figsize=(10,20))
    pos = 1
    for var in paramsToOptimise:
        ax = graph.add_subplot(6,3,pos)
        plt.plot(Params.loc[:,var],-1*Params.loc[:,'fits'],'o',color='lightgrey')
        plt.plot(Params.loc[:,var].iloc[1:4],-1*Params.loc[:,'fits'].iloc[1:4],'o',color='r')
        plt.plot(Params.loc[:,var].iloc[4:7],-1*Params.loc[:,'fits'].iloc[4:7],'o',color='g')
        plt.plot(Params.loc[:,var].iloc[7:10],-1*Params.loc[:,'fits'].iloc[7:10],'o',color='b')
        plt.plot(ret.x[pos-1],-ret.fun,'o',color='gold')
        plt.title(var)
        pos+=1

    graph = plt.figure(figsize=(20,20))
    done = 0 
    for xvar in paramsToOptimise:
        n = len(paramsToOptimise)
        pos = (done * n) + done + 1
        for yvar in paramsToOptimise[done:]:
            ax = graph.add_subplot(n,n,pos)
            num10 = int(Params.index.size * 0.15)
            top10 = Params.iloc[:num10,:]
            if xvar != yvar:
                plt.plot(top10.loc[:,xvar],top10.loc[:,yvar],'o')
            else:
                plt.text(0.05,0.5,xvar,transform=ax.transAxes)
            pos+=1
        done+=1
