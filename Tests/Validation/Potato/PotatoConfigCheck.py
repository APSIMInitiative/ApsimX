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

import sqlite3
import datetime
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import statsmodels.api as sm
import MathsUtilities as MUte
import matplotlib.patheffects as path_effects
import GraphHelpers as GH
import matplotlib.lines as mlines
# %matplotlib inline

# ### Link to APSIM output Data

con = sqlite3.connect(r'C:\GitHubRepos\ApsimX\Tests\Validation\Potato\Potato.db')

# ### Read the Simulations table that has SimulationID matched to Simulation Name

Simulations = pd.read_sql("Select * from _Simulations",con)
Simulations.set_index('ID',inplace=True)

# ### Read in the Factor table that links factor names and levels to simulation IDs

Factors = pd.read_sql("Select * from _Factors",
                        con)
Factors.set_index('SimulationID',inplace=True)

# ### Read in the Report generated on the Sowing Event

LocList = pd.read_excel(r'C:\GitHubRepos\ApsimX\Tests\Validation\Potato\List of locations.xlsx')
LocList.set_index('metfile',inplace=True)

import re
def extractLocation(lcn):
    lcn = lcn.replace('MET files\\','')
    lcn = re.sub('\d','',lcn)
    lcn = lcn.replace('.met','')
    return lcn
def extractfileName(fn):
    fn = fn.replace('MET files\\','')
    fn = fn.replace('.met','')
    return fn


Obs = pd.read_excel(r'C:\GitHubRepos\ApsimX\Tests\Validation\Potato\Observed.xlsx')

InitialReport = pd.read_sql("Select * from InitialReport",
                        con)
InitialReport.loc[:,'SimulationName'] = [Simulations.loc[InitialReport.loc[x,'SimulationID'],'Name'] for x in InitialReport.index]
InitialReport.drop(['CheckpointID','Zone','Nitrogen','01','1','Field_','_','Water','Radiation','PlantDensity'],axis=1,inplace=True)
InitialReport.set_index('SimulationID',inplace=True)
InitialReport.loc[:,'Location'] = [extractLocation(x) for x in InitialReport.loc[:,'Weather.FileName']]
InitialReport.loc[:,'FileName'] = [extractfileName(InitialReport.loc[x,'Weather.FileName']) for x in InitialReport.index]
InitialReport.loc[:,'Country'] = [LocList.loc[x,'country'] for x in InitialReport.loc[:,'FileName']]
InitialReport.loc[:,'Loc'] = [LocList.loc[x,'loc'] for x in InitialReport.loc[:,'FileName']]
InitialReport.loc[:,'Country Loc'] = [InitialReport.loc[x,'Country'] + ' ' + InitialReport.loc[x,'Loc'] for x in InitialReport.index] 

DailyReport = pd.read_sql("Select * from DailyReport",
                        con)
DailyReport.loc[:,'SimulationName'] = [Simulations.loc[DailyReport.loc[x,'SimulationID'],'Name'] for x in DailyReport.index]
DailyReport.drop(['CheckpointID','Zone','Nitrogen','01','1','Field_','_','Water','Radiation','PlantDensity'],axis=1,inplace=True)
DailyReport.set_index('SimulationID',inplace=True)

DailyPreObs = pd.read_sql("Select * from TimeSeriesData",
                        con)
DailyPreObs.loc[:,'SimulationName'] = [Simulations.loc[DailyPreObs.loc[x,'SimulationID'],'Name'] for x in DailyPreObs.index]
DailyPreObs.set_index('SimulationID',inplace=True)
DailyPreObs.drop_duplicates(inplace=True)

HarvestReport = pd.read_sql("Select * from HarvestReport",
                        con)
HarvestReport.loc[:,'SimulationName'] = [Simulations.loc[HarvestReport.loc[x,'SimulationID'],'Name'] for x in HarvestReport.index]
HarvestReport.drop(['CheckpointID','Zone','Nitrogen','01','1','Field_','_','Water','Radiation','PlantDensity'],axis=1,inplace=True)
HarvestReport.loc[:,'PlantPopn'] = 1/((HarvestReport.loc[:,'RowWidth']/1000)* (HarvestReport.loc[:,'InterRowPlantSpace']/1000))
HarvestReport.loc[:,'StemPopn'] = HarvestReport.loc[:,'StemPerTuber'] *  HarvestReport.loc[:,'PlantPopn']
HarvestReport.loc[:,'Location'] = [extractLocation(x) for x in HarvestReport.loc[:,'Weather.FileName']]
HarvestReport.set_index('SimulationID',inplace=True)
HarvestReport.loc[:,'FileName'] = [extractfileName(HarvestReport.loc[x,'Weather.FileName']) for x in HarvestReport.index]
HarvestReport.loc[:,'Country'] = [LocList.loc[x,'country'] for x in HarvestReport.loc[:,'FileName']]
HarvestReport.loc[:,'Loc'] = [LocList.loc[x,'loc'] for x in HarvestReport.loc[:,'FileName']]
HarvestReport.loc[:,'Country Loc'] = [HarvestReport.loc[x,'Country'] + ' ' + HarvestReport.loc[x,'Loc'] for x in HarvestReport.index] 

HarvestPreObs = pd.read_sql("Select * from FinalYieldData",
                        con)
HarvestPreObs.loc[:,'SimulationName'] = [Simulations.loc[HarvestPreObs.loc[x,'SimulationID'],'Name'] for x in HarvestPreObs.index]
HarvestPreObs.set_index('SimulationID',inplace=True)
HarvestPreObs.drop_duplicates(inplace=True)

# +
# Simulations.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\Simulations.csv', index=True)
# Factors.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\Factors.csv', index=True)
# LocList.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\LocList.csv', index=True)
# InitialReport.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\InitialReport.csv', index=True)
# DailyReport.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\DailyReport.csv', index=True)
# HarvestReport.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\HarvestReport.csv', index=True)
# Obs.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\Obs.csv', index=True)
# HarvestPreObs.to_csv(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Paper files\PreObs.csv', index=True)
# -

# ### List of simulation names that are in observations data set but are not triggering a sowing event

NotPlanted = []
for x in Obs.SimulationName.drop_duplicates().values:
    if x not in InitialReport.loc[:,'SimulationName'].drop_duplicates().values:
        NotPlanted.append(x)
NotPlanted

# ## List of Simulation names that are being planted but are not triggering a harvest event

NotHarvested = []
for x in InitialReport.index:
    if x not in HarvestReport.index:
        NotHarvested.append(Simulations.loc[x,'Name'])
NotHarvested

Experiments = HarvestReport.Experiment.drop_duplicates().values
Experiments

Folders = ['New Zealand', 'Australia', 'SubStor','Scotland']

Locations = HarvestReport.loc[:,'Loc'].drop_duplicates().values
Locations.sort()
Countries = HarvestReport.loc[:,'Country'].drop_duplicates().values
Countries.sort()
CountryLocs = HarvestReport.loc[:,'Country Loc'].drop_duplicates().values
CountryLocs.sort()

Locations

Countries

CountryLocs

AllColors = ['black',
 'grey',
 'lightgrey',
 'maroon',
 'indianred',
 'red',
'salmon',
 'darksalmon',
 'lightsalmon',
 'saddlebrown',
 'peru',
 'darkorange',
 'navajowhite',
             'wheat',
 'goldenrod',
 'gold',
 'darkkhaki',
 'olive',
 'yellow',
 'yellowgreen',
 'darkolivegreen',
 'darkseagreen',
 'limegreen',
 'lime',
 'springgreen',
 'mediumspringgreen',
 'lightseagreen',
 'darkslategray',
 'darkcyan',
             'deepskyblue',
 'dodgerblue',
 'slategray',
 'lightsteelblue',
 'royalblue',
 'navy',
 'darkslateblue',
 'mediumpurple',
 'rebeccapurple',
 'darkorchid',
 'violet',
 'purple',
             'mediumvioletred',
 'crimson',
 'pink']
AllColors.sort()

AllColors

Cultivars = HarvestReport.Cultivar.drop_duplicates().values
Cultivars.sort()
Cultivars

CultivarAcronums = {'39707716': '39', 
'Achirana': 'Ac', 
'Agria': 'Ag', 
'Alpha': 'Al', 
'Amarilis': 'Am', 
'Asante': 'As', 
'Atlantic': 'At', 
'Bintje': 'Bi', 
'Coliban': 'Co', 
'Desiree': 'Dt', 
'Dianella': 'De', 
'Fianna': 'Di', 
'Gabriela': 'Fi', 
'Horizon': 'Ga', 
'Ib0005': 'Ho', 
'IlamHardy': 'Ib', 
'JerseyBenny': 'Ih', 
'Jinguan': 'Jb', 
'Kaptah': 'Ji', 
'Karaka': 'Ka', 
'Kexin': 'Kr', 
'Kufri Bahar': 'Kb', 
'Luky': 'Lu', 
'Maria': 'Ma', 
'Maris Piper': 'Mp', 
'Moonlight': 'Ml', 
'Nadine': 'Na', 
'Neishu': 'Ne', 
'Nooksac': 'No', 
'Posmo': 'Po', 
'Red Lasoda': 'Rl', 
'RedRascal': 'Re', 
'Rua': 'Ru', 
'RussetBurbank': 'Rb', 
'RussetRanger': 'Rr', 
'Sava': 'Sa', 
'Sebago': 'Se', 
'Spunta': 'Sp', 
'Tylva': 'Ty', 
'Waycha': 'Wa', 
'Zibaihua': 'Zi'}

# +
CountryProps = pd.DataFrame(index = Countries,columns=['Color'])
pos = 0
for x in CountryProps.index:
    CountryProps.loc[x,'Color'] = AllColors[pos]
    pos += 2
CultivarProps = pd.DataFrame.from_dict(CultivarAcronums,orient='index',columns=['CultAcro'])
pos=0
for x in CultivarProps.index:
    CultivarProps.loc[x,'Color'] = AllColors[pos]
    pos+=1

ColProps = pd.DataFrame(index=HarvestReport.index)
ColProps.loc[:,'Cultivar'] = CultivarProps.loc[[HarvestReport.loc[x,'Cultivar'] for x in ColProps.index],'Color'].values
ColProps.loc[:,'Country'] = CountryProps.loc[[HarvestReport.loc[x,'Country'] for x in ColProps.index],'Color'].values
ColProps

# +
['AboveGround N', 'AboveGround Wt',
       'TotalLive Wt', 'TotalLive N',
       'AppearedCohortNo', 'CoverGreen',
       'CoverTotal', 'LAI', 'Leaf Live N',
       'Leaf Live Wt', 'Leaf Live NConc',
       'Leaf SpecificArea', 'Stem Live N',
       'Stem Live NConc', 'Stem Live Wt',
       'Tuber DM%',
       'Tuber Live N', 'Tuber Live NConc',
       'Tuber Live Wt', 'Tuber LiveFWt',
       'SW(1)',
       'SW(2)', 'SW(3)',
       'SW(4)', 'SW(5)',
       'SW(6)', 'TotalSWC', 'TotalSoilN', 'TotalNO3',
       'TotalNH4', 'SW(7)']

['$g/m^2$', '$g/m^2$',
       '$g/m^2$', '$g/m^2$',
       '$No$', '$0-1$',
       '$0-1$', '$LAI$', '$g/m^2$',
       '$g/m^2$', '$g/g$',
       'mm2/g', '$g/m^2$',
       '$g/g$', '$g/m^2$',
       '$g/g$',
       '$g/m^2$', '$g/g$',
       '$g/m^2$', '$g/m^2$',
       'mm/mm',
       'mm/mm', 'mm/mm',
       'mm/mm', 'mm/mm',
       'mm/mm', 'mm', '$kg/ha$', '$kg/ha$',
       '$kg/ha$', 'mm/mm']

# +
AllVars = HarvestPreObs.loc[:,['Observed' in x for x in HarvestPreObs.columns ]].columns.values
for i in range(len(AllVars)):
    AllVars[i] = AllVars[i].replace('Observed.','')
PlotVariables = AllVars[['CheckpointID' not in x and 'Clock' not in x for x in AllVars]]

AllDailyVars = DailyPreObs.loc[:,['Observed' in x for x in DailyPreObs.columns ]].columns.values
for i in range(len(AllDailyVars)):
    AllDailyVars[i] = AllDailyVars[i].replace('Observed.','')
PlotDailyVariables = AllDailyVars[['CheckpointID' not in x and 'Clock' not in x and 'Script' not in x for x in AllDailyVars]]

#Names = ['Tuber Dry Weight','Tuber Fresh Weight', 'Tuber N','DMC','Total Wt', 'Total N']
VariablePars = pd.DataFrame(index=PlotDailyVariables)
VariablePars.loc[:,'Units'] = ['$g/m^2$', '$g/m^2$',
       '$g/m^2$', '$g/m^2$',
       '$No$', '$0-1$',
       '$0-1$', '$LAI$', '$g/m^2$',
       '$g/m^2$', '$g/g$',
       'mm2/g', '$g/m^2$',
       '$g/g$', '$g/m^2$',
       '$g/m^2$', '$g/g$',
       '$g/m^2$', '$g/m^2$',
       'mm/mm',
       'mm/mm', 'mm/mm',
       'mm/mm', 'mm/mm',
       'mm/mm', 'mm', '$kg/ha$', '$kg/ha$',
       '$kg/ha$', 'mm/mm'
        ]
VariablePars.loc[:,'Names'] = ['Above-ground N', 'Above-ground Wt',
       'Total live Wt', 'Total live N',
       'Appeared cohorts', 'Green cover',
       'Total cover', 'Leaf area index', 'Live leaf N',
       'Live leaf Wt', 'Live leaf NConc',
       'Specific Leaf Area ', 'Live stem N',
       'Live stem NConc', 'Live stem Wt',
       'Live Tuber N', 'Live Tuber NConc',
       'Live tuber Wt', 'Live Tuber Fresh Wt',
       'SW(1)',
       'SW(2)', 'SW(3)',
       'SW(4)', 'SW(5)',
       'SW(6)', 'Total SWC', 'Total Soil N', 'Total NO3',
       'Total NH4', 'SW(7)']
       
VariablePars

# +
InitialVars = ['Weather.Latitude','Weather.Longitude',
       'RowWidth','StemPerTuber',
       'InterRowPlantSpace','PlantPopn',
       'IrrigApplied','NFertApplied',
       'Weather.CO2', 'PlantingDepth']
InitialPars = pd.DataFrame(index=InitialVars)
InitialPars.loc[:,'Units'] = ['$^o$','$^o$',
        'mm', 'count',
        'mm','/m',
        'mm','kg/ha',
        'ppm','mm']

InitialPars.loc[:,'Names'] =['Latitude','Longitude',
       'Row width','Stems per tuber',
       'Inter-row plant space','Plant population',
       'Irrigation applied','N fertiliser applied',
       '$CO_2$ concentration', 'Planting depth']
InitialPars
# -

ffilter = HarvestReport.loc[:,'Cultivar'] == 'Agria'
HarvestReport.loc[ffilter,['Weather.Latitude','Weather.Longitude',
       'RowWidth','StemPerTuber',
       'InterRowPlantSpace','PlantPopn',
       'IrrigApplied','NFertApplied',
       'Weather.CO2', 'PlantingDepth']]


# +
def SortedPlot(Var,GroupVar,GroupList,ax):
    Sorted = HarvestReport.sort_values(GroupVar)
    Sorted.loc[:,'Linear'] = range(Sorted.index.size)
    tickPos = []
    pastx = -10
    ymin = Sorted.loc[:,Var].min()
    ymax = Sorted.loc[:,Var].max()
    DataRange = ymax-ymin
    Offset = DataRange * 0.01
    handles = []
    for g in GroupList:
        ColDF = globals()[GroupVar+'Props']
        col = ColDF.loc[g,'Color']
        ffilter = Sorted.loc[:,GroupVar] == g
        plt.plot(Sorted.loc[ffilter,'Linear'],Sorted.loc[ffilter,Var],
                 'o',color=col,ms=15)
        xloc = Sorted.loc[ffilter,'Linear'].mean()#.iloc[0],pastx+5)
        pastx = xloc
        yloc = Sorted.loc[ffilter,Var].max() + Offset# + random.randint(-40,40)
        #plt.text(xloc-5,yloc,g,fontsize=16,color=col,rotation=0,
        #             verticalalignment='bottom',horizontalalignment='center')
        handles.append(mlines.Line2D([],[],marker='o',color = col,label=g,ls='None'))
    plt.ylabel(InitialPars.loc[Var,'Names'] + ' (' + InitialPars.loc[Var,'Units']+')',fontsize=24)
    plt.ylim(ymin,ymax*1.1)
    ax.spines['right'].set_visible(False)
    ax.spines['top'].set_visible(False)
    plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
    plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=23)
    plt.title(Var,fontsize=30)
    plt.subplots_adjust(wspace=0.3, hspace=0.2)
    return handles

def makeConfigPlots(PlotVars,GroupVar,GroupList,legloc,legcols):
    no = len(GroupVar)
    rows = np.ceil(no/2) + 1
    pos=1
    for v in Vars:
        ax = Graph.add_subplot(rows,2,pos)
        handles = SortedPlot(v,GroupVar,GroupList,ax)
        pos+=1  
    plt.legend(handles=handles,loc=legloc,ncol=legcols,fontsize=16,markerscale=2.5)


# -

# ## The following series of Graphs present a series of variables that reflect the configuration of each simulation so these can be sense checked against the rest of the validation set

HarvestReport.columns

Graph = plt.figure(figsize=(18,35))
Vars = ['Weather.Latitude','Weather.Longitude',
       'RowWidth','StemPerTuber',
       'InterRowPlantSpace','PlantPopn',
       'IrrigApplied','NFertApplied',
       'Weather.CO2', 'PlantingDepth']
makeConfigPlots(Vars,'Country',Countries,(-1.5,-0.3),7)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.tif',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(18,35))
Vars = ['Weather.Latitude','Weather.Longitude','Weather.CO2']
makeConfigPlots(Vars,'Cultivar',Cultivars,(1.2,0),3)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.tif',dpi=1200,bbox_inches='tight')

Graph = plt.figure(figsize=(18,35))
Vars = ['Weather.Latitude','Weather.Longitude',
       'RowWidth','StemPerTuber',
       'InterRowPlantSpace','PlantPopn',
       'IrrigApplied','NFertApplied',
       'Weather.CO2', 'PlantingDepth']
makeConfigPlots(Vars,'Cultivar',Cultivars,(-1.5,-0.6),7)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\conf2.tif',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(18,35))
Vars = ['RowWidth','InterRowPlantSpace','PlantPopn',
       'IrrigApplied','NFertApplied','PlantingDepth']
makeConfigPlots(Vars,'Cultivar',Cultivars,(-1.5,-0.6),7)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.tif',dpi=1200,bbox_inches='tight')

def plotDepthVars(VarName,depthNorm):
    Thicks = InitialReport.loc[:,[X for X in InitialReport.columns if 'Thickness' in X]]
    Depths = Thicks.cumsum(axis=1)
    Vars = InitialReport.loc[:,[X for X in InitialReport.columns if VarName in X]]
    LayerDB = pd.DataFrame()
    for x in Depths.index:
        SimLayerDB = pd.DataFrame(index = pd.MultiIndex.from_product([[x],['LayerBounds','Varval']]))
        LayerBounds = [0.0]
        depthNormFact = 1 
        if depthNorm == True:
            depthNormFact = Thicks.loc[x,:][0]
        Varvals = [Vars.loc[x,:][0]/depthNormFact]
        for l in range(13):
            try:
                if depthNorm == True:
                    depthNormFact = Thicks.loc[x,:][l]
                LayerBounds.append(float(Depths.loc[x,:][l])*-1)
                Varvals.append(Vars.loc[x,:][l]/depthNormFact)
            except:
                do = 'Nothing'
        for p in range(len(LayerBounds)):
            SimLayerDB.loc[(x,'LayerBounds'),p] = LayerBounds[p]
            SimLayerDB.loc[(x,'Varval'),p] = Varvals[p]
        LayerDB = pd.concat([LayerDB,SimLayerDB])
        LayerDB.index = LayerDB.index.swaplevel()
        LayerBounds = LayerDB.stack().loc['LayerBounds']
        VarVals = LayerDB.stack().loc['Varval']
        LayerDB = pd.concat([LayerDB,SimLayerDB])
    MaxDep = LayerDB.loc[(slice(None),'LayerBounds'),:].min(axis=1).min()
    MinVar = LayerDB.loc[(slice(None),'Varval'),:].min(axis=1).min()
    MaxVar = LayerDB.loc[(slice(None),'Varval'),:].max(axis=1).max()    
    Graph = plt.figure(figsize=(10,20))
    cols = np.ceil(len(Locations)/5)
    pos=1
    for lcn in Locations:
        ax = Graph.add_subplot(cols,5,pos)
        #for x in Depths.index:
        #    plcn = InitialReport.loc[x,'Location']
        plt.plot(VarVals,LayerBounds,'o',color = 'lightgrey')
        plt.text(0.03,1.0,lcn,transform=ax.transAxes,fontsize=12)
        LocalVars = HarvestReport.loc[HarvestReport.loc[:,'Loc']==lcn,:].index
        for l in LocalVars:
            plt.plot(LayerDB.loc[(l,'Varval'),:],LayerDB.loc[(l,'LayerBounds'),:],'-o',color = 'red')
        plt.ylim(MaxDep*1.05,0)
        plt.xlim(MinVar * 0.8,MaxVar * 1.05)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        if pos in range(1,200,5):
            plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
            plt.ylabel('Depth (cm)',fontsize=12)
        else:
            plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
        if pos in list(range(len(Locations)-4,len(Locations)+1)):
            plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
            plt.xlabel(VarName,fontsize=12)
            #plt.xlabel(VarName+' ($kg\,ha^{-1}$)',fontsize=12)
        else:
            plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
        pos += 1
    plt.tight_layout
    Graph.patch.set_facecolor('white')


# ## The flowing series of graphs show CropSoil configuration settings for each simulation so they can be sense checked against the rest of the validation set

plotDepthVars('DUL',False)

plotDepthVars('XF',False)

plt.figure(figsize=(17,40))
plotDepthVars('FOM',False)
#plt.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Appendix\tif.jpg',dpi=1200,bbox_inches='tight')

plotDepthVars('LL',False)

plotDepthVars('KL',False)

plotDepthVars('NH4N',False)

plotDepthVars('NO3N',False)

plotDepthVars('SoilCNRatio',False)


# +
def MakeLabel(RegStats):
    #text = RegStats.Name + '  n = ' + str(RegStats.n)
    text = '\ny = ' + '%.2f'%RegStats.Intercept + '(se ' + '%.2f'%RegStats.SEintercept + ') + ' + '%.2f'%RegStats.Slope + '(se ' + '%.2f'%RegStats.SEslope + ') x' 
    text += '\n$r^2$ =' + '%.2f'%RegStats.R2 + ' RMSE = ' + '%.2f'%RegStats.RMSE +' NSE = ' + '%.2f'%RegStats.NSE
    text += '\nME = ' + '%.2f'%RegStats.ME + ' MAE = ' + '%.2f'%RegStats.MAE
    text += '\nn = ' + str(RegStats.n)
    return text

def AddObsPredGraph(Variables,DataTable,GroupVar,GroupList):
    Pos = 1
    for Var in Variables:
        NaNFilter = np.isnan(DataTable.loc[:,'Predicted.'+Var]) | np.isnan(DataTable.loc[:,'Observed.'+Var])
        IaNFilter = ~NaNFilter
        Obs = DataTable.loc[:,'Observed.'+Var].loc[IaNFilter].values
        Pred = DataTable.loc[:,'Predicted.'+Var].loc[IaNFilter].values
        no = len(Variables)
        rows = np.ceil(no/3)
        ax = Graph.add_subplot(rows,3,Pos)
        ColPos = 0
        MarPos = 0
        for g in GroupList:
            ColDF = globals()[GroupVar+'Props']
            col = ColDF.loc[g,'Color']
            SimIDs = HarvestReport.loc[HarvestReport.loc[:,GroupVar] == g].index.values
            ExpObs = DataTable.loc[SimIDs,'Observed.'+Var]
            ExpPred = DataTable.loc[SimIDs,'Predicted.'+Var]
            plt.plot(ExpObs,ExpPred,'o',color = col,label=g)
            ColPos +=1
            if ColPos == 30:
                ColPos = 1
                MarPos +=1
        uplim = max(Obs.max(),Pred.max())*1.1
        lowlim = min(Obs.min(),Pred.min())*0.95
        #plt.text(0.02,0.94,VariablePars.loc[Var,'Names'],transform=ax.transAxes,fontsize=20)
        plt.ylim(0,uplim)
        plt.xlim(0,uplim)
        plt.plot([lowlim,uplim*.95],[lowlim,uplim*.95],'-',color='k')
        RegStats = MUte.MathUtilities.CalcRegressionStats(Var,Pred,Obs)
        LabelText = MakeLabel(RegStats)
        #plt.text(uplim*0.05,uplim*.78,LabelText)
        #Fit linear regression to current series and store slope and intercept in dataframe
        ModFit = sm.regression.linear_model.OLS(Pred,  # Y variable
                                            sm.add_constant(Obs), # X variable
                                            missing='drop',                                     # ignor and data where one value is missing
                                            hasconst=False) 
        RegFit = ModFit.fit();  # fit models parameters
        Slope = RegFit.params[1] 
        Intercept = RegFit.params[0]
        Xs = [lowlim,uplim*.95]
        Ys = [Intercept + Xs[0]*Slope,Intercept + Xs[1]*Slope]
        plt.plot(Xs,Ys,'--',color='r')
        plt.ylabel('Pred ' + VariablePars.loc[Var,'Names'] + ' ('  + VariablePars.loc[Var,'Units'] + ')',fontsize=12)
        plt.xlabel('Obs ' + VariablePars.loc[Var,'Names'] + ' ('  + VariablePars.loc[Var,'Units'] + ')',fontsize=12)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.set_tick_params(labelsize=15)
        ax.xaxis.set_tick_params(labelsize=15)
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True)
        plt.tick_params(axis='y', which='both', left=True,right=False, labelbottom=True)
        plt.subplots_adjust(wspace=0.45, hspace=0.4)
        if Pos == 1:
            GH.AddLegend(LegLoc = (0,1.1),labelsize=16,Title='',NCol=5,MScale=2)
        Pos +=1


# +
CultByCountry = HarvestReport.reindex(['Country','Cultivar'],axis=1).drop_duplicates()
for CbC in CultByCountry.index:
    CultCountryFilter = (HarvestReport.loc[:,'Country'] == CultByCountry.loc[CbC,'Country']) & \
                        (HarvestReport.loc[:,'Cultivar'] == CultByCountry.loc[CbC,'Cultivar'])
    CultByCountry.loc[CbC,'Count'] = HarvestReport.loc[CultCountryFilter,:].index.size

Graph = plt.figure(figsize=(12,10))
MaxSize = CultByCountry.loc[:,'Count'].max() * 1.5
for CbC in CultByCountry.index:
    col = ColProps.loc[CbC,'Cultivar']
    size = CultByCountry.loc[CbC,'Count']
    Alpha = (MaxSize-size)/MaxSize
    plt.plot(CultByCountry.loc[CbC,'Country'],CultByCountry.loc[CbC,'Cultivar'],'o',color="0.5",ms=size,alpha=Alpha)
plt.xticks(rotation=90,fontsize=14)
plt.yticks(fontsize=12)
CountryCounts = CultByCountry.groupby('Country').sum().sort_values('Count',ascending=False)
CultivarCounts = CultByCountry.groupby('Cultivar').sum().sort_values('Count',ascending=False)
for c in CountryCounts.index:
    plt.text(c,-1.5,int(CountryCounts.loc[c,'Count']),verticalalignment='bottom',horizontalalignment='center',fontsize=12)
for c in CultivarCounts.index:
    plt.text(-1.5,c,int(CultivarCounts.loc[c,'Count']),verticalalignment='center',horizontalalignment='left',fontsize=12)
plt.xlim(-2,20)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\cultivar_vs_loc.tif',dpi=1200,bbox_inches='tight')
# -

# ## Calculated long term mean temperature patterns from Tav, Amp and Lattitude

# +
#day of year of nthrn summer solstice
nth_solst = 173.0
#delay from solstice to warmest day (days)
temp_delay = 27.0
#warmest day of year in nth hemisphere
nth_hot = nth_solst + temp_delay
#day of year of sthrn summer solstice
sth_solst = nth_solst + 365.25 / 2.0
#warmest day of year of sth hemisphere
sth_hot = sth_solst + temp_delay
my_pi = 3.14159
ang = (2.0 * my_pi) / 365.25

def calcAlx(latitude,doy):
    date = datetime.datetime(2000,1,1) + datetime.timedelta(doy)
    if (latitude >= 0):
        alx = ang * (date - datetime.timedelta(nth_hot)).timetuple().tm_yday#_today.AddDays(-(int)nth_hot).DayOfYear;
    else:
        alx = ang * (date - datetime.timedelta(sth_hot)).timetuple().tm_yday#_today.AddDays(-(int)sth_hot).DayOfYear;
    if ((alx < 0.0) | (alx > 6.31)):
        print("Value for alx is out of range")
        raise 
    else:
        return alx
    
def estTemp(doy,latitude,Tav,Amp):
    alx = calcAlx(latitude,doy)
    return Tav + (Amp / 2.0) * np.cos(alx)

LongTermMeanTemp = pd.DataFrame(index=range(1,366),columns = Locations)

for l in LongTermMeanTemp.columns:
    for d in LongTermMeanTemp.index:
        params = InitialReport.loc[InitialReport.Loc == l,
                                   ['Weather.Latitude','Weather.Tav','Weather.Amp']].drop_duplicates()
        LongTermMeanTemp.loc[d,l] = estTemp(d,
                                            params.iloc[0,0],
                                            params.iloc[0,1],
                                            params.iloc[0,2])


# -

def WeatherVsDAS(Variable,GroupLabel,colourLabel):
    Grouping = HarvestReport.loc[:,[GroupLabel,colourLabel]].dropna().drop_duplicates().values
    Grouping = pd.DataFrame(Grouping).sort_values(by=[1,0])[0].values
    cols = np.ceil(len(Grouping)/3)
    pos=1
    upper = DailyReport.loc[:,'Weather.'+Variable].max() * 1.1
    lower = DailyReport.loc[:,'Weather.'+Variable].min() * 0.8
    for group in Grouping:
        ax = Graph.add_subplot(cols,4,pos)
        GroupSims = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index.sort_values()
        for sim in GroupSims:
            col = ColProps.loc[sim,colourLabel]
            DAS = DailyReport.loc[sim,'Clock.Today.DayOfYear']
            NonZeros = DAS>0
            DAS = DAS.loc[NonZeros]
            absolutes = DailyReport.loc[sim,'Weather.'+Variable].loc[NonZeros]
            try:
                plt.plot(DAS,absolutes,'o',color=col,label=group)
                if Variable == 'Radn':
                    qmax = DailyReport.loc[sim,'Weather.Qmax'].loc[NonZeros]
                    plt.plot(DAS,qmax,'o',color='gold',label='qmax')
                if Variable == 'MaxT':
                    MinT = DailyReport.loc[sim,'Weather.MinT'].loc[NonZeros]
                    plt.plot(DAS,MinT,'o',color=col,label=group,alpha=0.5,mfc='white')
                    plt.plot(LongTermMeanTemp.index,LongTermMeanTemp.loc[:,group],'-',color='gold')
                
            except:
                print(DailyReport.loc[sim,'SimulationName'].drop_duplicates())
        plt.text(0.03,1.0,group + ', ' + HarvestReport.loc[sim,'Country'] + \
                 ' (' +  str(HarvestReport.loc[sim,'Weather.Latitude'].round(decimals=1)) + ' deg)',
                 transform=ax.transAxes,fontsize=12)
        plt.ylim(lower,upper)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        plt.xlim(0,366)
        if pos in range(1,200,4):
            plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
            plt.ylabel(Variable,fontsize=12)
        else:
            plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
        if pos in list(range(len(Grouping)-2,len(Grouping)+1)):
            plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
            plt.xlabel('DoY',fontsize=12)
        else:
            plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
        pos+=1
    plt.tight_layout


Graph = plt.figure(figsize=(17,40))
WeatherVsDAS('Radn','Loc','Country')
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\Fig. 4.tif',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(17,40))
WeatherVsDAS('MaxT','Loc','Country')
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\Fig..tif',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(15,40))
WeatherVsDAS('Wind','Loc','Country')
Graph.patch.set_facecolor('white')

Graph = plt.figure(figsize=(15,40))
WeatherVsDAS('VPD','Loc','Country')
Graph.patch.set_facecolor('white')

Graph = plt.figure(figsize=(15,40))
WeatherVsDAS('Rain','Loc','Country')
Graph.patch.set_facecolor('white')

DailyReport.loc[:,'RadResid'] = (DailyReport.loc[:,'Weather.Qmax'] - DailyReport.loc[:,'Weather.Radn'])/DailyReport.loc[:,'Weather.Qmax']
DailyReport.loc[:,'RadRel'] =  DailyReport.loc[:,'Weather.Radn']/DailyReport.loc[:,'Weather.Qmax'] 

Graph = plt.figure(figsize=(15,40))
GroupLabel='Loc'
colourLabel= 'Country'
Grouping = HarvestReport.loc[:,[GroupLabel,colourLabel]].dropna().drop_duplicates().values
Grouping = pd.DataFrame(Grouping).sort_values(by=[1,0])[0].values
cols = np.ceil(len(Grouping)/3)
pos=1
upper = DailyReport.loc[:,'RadResid'].max() * 1.1
lower = DailyReport.loc[:,'RadResid'].min() * 0.8
for group in Grouping:
    ax = Graph.add_subplot(cols,3,pos)
    simsInGroup = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    GroupSims = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    for sim in GroupSims:
        col = ColProps.loc[sim,colourLabel]
        Rain = DailyReport.loc[sim,'Weather.Rain']
        ResRad = DailyReport.loc[sim,'RadResid']
        try:
            plt.plot(Rain,ResRad,'o',color=col,label=group)
        except:
            print(DailyReport.loc[sim,'SimulationName'].drop_duplicates())
    plt.text(0.03,1.0,group + ', ' + HarvestReport.loc[sim,colourLabel] + \
             ' (' +  str(HarvestReport.loc[sim,'Weather.Latitude']) + ' deg)',
             transform=ax.transAxes,fontsize=12)
    plt.ylim(lower,upper)
    ax.spines['right'].set_visible(False)
    ax.spines['top'].set_visible(False)
    if pos in range(1,200,3):
        plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
        plt.ylabel('Residual Radn',fontsize=12)
    else:
        plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
    if pos in list(range(len(Grouping)-4,len(Grouping)+1)):
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
        plt.xlabel('DAS',fontsize=12)
    else:
        plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
    pos+=1
plt.tight_layout
Graph.patch.set_facecolor('white')


def RainDay(rain):
    if rain == 0.0:
        return 0.0
    else:
        return 1.0


DailyReport.loc[:,'RainDay'] = [RainDay(DailyReport.iloc[x,:].loc['Weather.Rain']) for x in range(DailyReport.index.size)]

Graph = plt.figure(figsize=(15,20))
GroupLabel='Loc'
colourLabel= 'Country'
Grouping = HarvestReport.loc[:,[GroupLabel,colourLabel]].dropna().drop_duplicates().values
Grouping = pd.DataFrame(Grouping).sort_values(by=[1,0])[0].values
cols = np.ceil(len(Grouping)/5)
pos=1
upper = DailyReport.loc[:,'RadResid'].max() * 1.1
lower = DailyReport.loc[:,'RadResid'].min() * 0.8
for group in Grouping:
    ax = Graph.add_subplot(cols,5,pos)
    simsInGroup = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    GroupSims = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    for sim in GroupSims:
        col = ColProps.loc[sim,colourLabel]
        RainDay = DailyReport.loc[sim,'RainDay']
        ResRad = DailyReport.loc[sim,'RadResid']
        try:
            plt.plot(RainDay.cumsum(),ResRad.cumsum(),'o',color=col,label=group)
        except:
            print(DailyReport.loc[sim,'SimulationName'].drop_duplicates())
    plt.text(0.03,1.0,group + ', ' + HarvestReport.loc[sim,colourLabel],
             transform=ax.transAxes,fontsize=12)
    plt.xlim(0,200)
    plt.ylim(0,200)
    ax.spines['right'].set_visible(False)
    ax.spines['top'].set_visible(False)
    if pos in range(1,200,5):
        plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
        plt.ylabel('AccRRadnLoss',fontsize=12)
    else:
        plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
    if pos in list(range(len(Grouping)-4,len(Grouping)+1)):
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
        plt.xlabel('Accum rain days',fontsize=12)
    else:
        plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
    plt.plot([0,200],[0,200],'--',color='k')
    pos+=1
plt.tight_layout
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\Fig. 5.tif',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(15,20))
GroupLabel='Loc'
colourLabel= 'Country'
Grouping = HarvestReport.loc[:,[GroupLabel,colourLabel]].dropna().drop_duplicates().values
Grouping = pd.DataFrame(Grouping).sort_values(by=[1,0])[0].values
cols = np.ceil(len(Grouping)/5)
pos=1
upper = DailyReport.loc[:,'RadResid'].max() * 1.1
lower = DailyReport.loc[:,'RadResid'].min() * 0.8
for group in Grouping:
    ax = Graph.add_subplot(cols,5,pos)
    simsInGroup = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    GroupSims = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
    RainDay=[]
    NoRainDay=[]
    col=''
    for sim in GroupSims:
        col = ColProps.loc[sim,colourLabel]
        for w in DailyReport.loc[sim,'RadRel'].loc[DailyReport.loc[sim,'RainDay']==1].values:
            RainDay.append(w)
        for d in DailyReport.loc[sim,'RadRel'].loc[DailyReport.loc[sim,'RainDay']==0].values:
            NoRainDay.append(d)
    try:
        plt.boxplot([RainDay,NoRainDay])
        plt.plot([1,2],[np.mean(RainDay),np.mean(NoRainDay)])
    except:
        print(DailyReport.loc[sim,'SimulationName'].drop_duplicates())
    plt.text(0.03,1.0,group + ', ' + HarvestReport.loc[sim,colourLabel],
             transform=ax.transAxes,fontsize=12)
    ax.spines['right'].set_visible(False)
    ax.spines['top'].set_visible(False)
    plt.ylim(0,1.5)
    if pos in range(1,200,5):
        plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
        plt.ylabel('relative Radn',fontsize=12)
    else:
        plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
    if pos in list(range(len(Grouping)-4,len(Grouping)+1)):
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
        plt.xticks([1, 2], ['Wet', 'Dry'])
    else:
        plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
    pos+=1
plt.tight_layout
Graph.patch.set_facecolor('white')

# This graph plots the relative radiation (1 meaning recorded radiation was the same as clear sky radiation) for dry (rain == 0) and wet (rain > 0) days
