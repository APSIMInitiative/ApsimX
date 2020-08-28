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


# -

# ## Standard Obs vs Pred graph for Harvest

Graph = plt.figure(figsize=(15,20))
AddObsPredGraph(PlotVariables,HarvestPreObs,'Country',Countries)
Graph.patch.set_facecolor('white')

# ## Standard Obs vs Pre graph for time series data

Graph = plt.figure(figsize=(15,40))
AddObsPredGraph(PlotDailyVariables,DailyPreObs,'Country',Countries)
Graph.patch.set_facecolor('white')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.jpeg',dpi=600,bbox_inches='tight')

HarvestResiduals = HarvestPreObs.loc[:,[X for X in HarvestPreObs.columns if 'Pred-Obs' in X]].copy()


def GroupResidulesGraph(Variables,ResidulesTable,GroupList,GroupVar):
    panpos = 1
    rows = len(Variables)
    for Var in Variables:
        ax = Graph.add_subplot(rows,2,panpos)
        startx = 0
        tickPoss = []
        tickLabs = []
        colpos = 1
        upper = ResidulesTable.loc[:,'Pred-Obs.' + Var].max() * 1.1
        lower = ResidulesTable.loc[:,'Pred-Obs.' + Var].min() * 1.1
        MeanResidual = ResidulesTable.loc[:,'Pred-Obs.' + Var].mean()
        TextPos = 'Lower'
        for group in GroupList:
            ColDF = globals()[GroupVar+'Props']
            col = ColDF.loc[group,'Color']
            SimIDs = HarvestReport.loc[HarvestReport.loc[:,GroupVar]==group,:].index.values
            xvals = range(startx,startx+len(SimIDs))
            plt.plot(xvals,ResidulesTable.reindex(SimIDs,axis=0).loc[:,'Pred-Obs.' + Var],
                     '-o',color=col)
            #tickPoss.append(startx+len(SimIDs)/2)
            tickLabs.append(group)
            if TextPos=='Lower':
                plt.text(startx+len(SimIDs)/2,lower,group,fontsize=20,
                         color=col,rotation=-45,verticalalignment='bottom',horizontalalignment='center')
                TextPos = 'Upper'
            else:
                plt.text(startx+len(SimIDs)/2,upper,group,fontsize=20,
                         color=col,rotation=-45,verticalalignment='top',horizontalalignment='center')
                TextPos = 'Lower'
            startx += len(SimIDs)
            if colpos == 30:
                colpos = 0
            colpos+=1
        ax.xaxis.set_major_locator(plt.FixedLocator(tickPoss))
        ax.set_xticklabels(tickLabs)
        ax.yaxis.set_tick_params(labelsize=17)
        plt.tick_params(rotation=0)
        plt.plot([0,startx],[MeanResidual,MeanResidual],'-',lw=3,color='k')
        plt.plot([0,startx],[0,0],'--',lw=3,color='k')
        plt.ylim(lower,upper)
        plt.title(Var,fontsize=25)
        plt.ylabel('Pred-Obs ' + ' ('  + VariablePars.loc[Var,'Units'] + ')',fontsize=23)
        #plt.ylabel('Pred-Obs',fontsize=23)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.set_tick_params(labelsize=21)
        ax.xaxis.set_tick_params(labelsize=21)
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True)
        plt.tick_params(axis='y', which='both', left=True,right=False, labelbottom=True)        
        plt.subplots_adjust(wspace=0.15, hspace=0.5)
        panpos+=1


# ## Graph residuals for key variables grouped by cultivar

Graph = plt.figure(figsize=(30,60))
GroupResidulesGraph(PlotVariables,HarvestResiduals,Cultivars,'Cultivar')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.jpeg',dpi=600,bbox_inches='tight')

# ## Graph residuals for key variables grouped by location

Graph = plt.figure(figsize=(30,60))
GroupResidulesGraph(PlotVariables,HarvestResiduals,Countries,'Country')
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.jpeg',dpi=600,bbox_inches='tight')

# ## Make graphs of residuals vs simulation configuration variables

def ResidulesVsVariable(ResidulesTable,Variable,GroupList,GroupVar,XVariables):
    pos = 1
    for vsVariable in XVariables:
        ax = Graph.add_subplot(7,2,pos)
        for group in GroupList:
            ColDF = globals()[GroupVar+'Props']
            col = ColDF.loc[group,'Color']
            SimIDs = HarvestReport.loc[HarvestReport.loc[:,GroupVar]==group,:].index.values
            yvals = ResidulesTable.reindex(SimIDs,axis=0).loc[:,'Pred-Obs.Potato.'+Variable]
            xvals = HarvestReport.reindex(SimIDs,axis=0).loc[:,vsVariable]
            plt.plot(xvals,yvals,'o',color=col,label=group)
            upper = HarvestReport.loc[:,vsVariable].max() * 1.1
            lower = HarvestReport.loc[:,vsVariable].min() * 0.8
            MeanResidual = ResidulesTable.loc[:,'Pred-Obs.Potato.' + Variable].mean()
            plt.plot([lower,upper],[MeanResidual,MeanResidual],'-',lw=3,color='k')
            plt.plot([lower,upper],[0,0],'--',lw=3,color='k')
            #plt.title('Pred-Obs ' + Variable + ' vs ' + vsVariable)
            plt.ylabel('Pred-Obs ($g\,m^{-2}$)',fontsize=17)
            #plt.ylabel('Pred-Obs ' + Variable)
            plt.xlabel(vsVariable,fontsize=17)
            plt.subplots_adjust(wspace=0.35, hspace=0.35)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.set_tick_params(labelsize=15)
        ax.xaxis.set_tick_params(labelsize=15)
        plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True)
        plt.tick_params(axis='y', which='both', left=True,right=False, labelbottom=True)
        if pos == 1:
            GH.AddLegend(LegLoc = (-0.1,1.1),labelsize=16,Title='',NCol=5,MScale=2)
        pos+=1


Graph = plt.figure(figsize=(15,25))
XVariables = ['Weather.Latitude','Weather.Longitude','Weather.Tav','Weather.Amp',
'Weather.CO2','CumRain','PlantingDepth','PlantPopn','IrrigApplied','NFertApplied']
ResidulesVsVariable(HarvestResiduals,'Tuber.Live.Wt',Cultivars,'Cultivar',XVariables)
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.jpeg',dpi=600,bbox_inches='tight')

Graph = plt.figure(figsize=(15,25))
XVariables = ['Weather.Latitude','Weather.Longitude','Weather.Tav','Weather.Amp',
'Weather.CO2','CumRain','PlantingDepth','PlantPopn','IrrigApplied','NFertApplied']
ResidulesVsVariable(HarvestResiduals,'Tuber.Live.Wt',Countries,'Country',XVariables)
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Appendix\res.jpg',dpi=600,bbox_inches='tight')

# +
def simsWithTimeCoarseData(Var):
    SimsWithTimeCoarseData = []
    for sim in DailyPreObs.index.drop_duplicates():
        try:
            obsNo = len(DailyPreObs.loc[sim,'Pred-Obs.Potato.'+Var].dropna().values)
            if obsNo>1:
                SimsWithTimeCoarseData.append(sim)
        except:
            do = 'Nothing'
    return SimsWithTimeCoarseData

def ResidulesVsTtSow(Variable,GroupLabel,colourLabel):
    SimsWithTimeDataForVar = simsWithTimeCoarseData(Variable)
    WithTimeCoarseData = HarvestReport.reindex(SimsWithTimeDataForVar,axis=0).loc[:,GroupLabel].dropna().drop_duplicates().values
    Grouping = WithTimeCoarseData
    Grouping.sort()
    cols = np.ceil(len(Grouping)/5)
    pos=1
    upper = DailyPreObs.loc[:,'Pred-Obs.Potato.'+Variable].max() * 1.1
    lower = DailyPreObs.loc[:,'Pred-Obs.Potato.'+Variable].min() * 0.8
    MeanResidual = DailyPreObs.loc[:,'Pred-Obs.Potato.'+Variable].mean()
    DailyObsPredSims = DailyPreObs.index.drop_duplicates()
    for group in Grouping:
        ax = Graph.add_subplot(cols,5,pos)
        simsInGroup = HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index
        GroupSimsWithTimeCoarseData = list(set(HarvestReport.loc[HarvestReport.loc[:,GroupLabel]==group].index) & set(SimsWithTimeDataForVar))
        for sim in GroupSimsWithTimeCoarseData:
            col = ColProps.loc[sim,colourLabel]
            residuals = DailyPreObs.loc[sim,'Pred-Obs.Potato.'+Variable]
            DatesWithObs = DailyReport.loc[:,'Clock.Today'].isin(DailyPreObs.loc[sim,'Clock.Today'])
            TtSow = DailyReport.loc[DatesWithObs,:].loc[sim,'Potato.Phenology.AccumulatedEmergedTT'].values
            try:
                plt.plot(TtSow,residuals,'o-',color=col,label=group)
            except:
                print(DailyReport.loc[sim,'SimulationName'].drop_duplicates())
        plt.text(0.03,1.0,group,transform=ax.transAxes,fontsize=12)
        plt.ylim(lower,upper)
        plt.plot([0,2300],[MeanResidual,MeanResidual],'-',lw=3,color='k')
        plt.plot([0,2300],[0,0],'--',lw=3,color='k')
        plt.subplots_adjust(wspace=0.05, hspace=0.2)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        if pos in range(1,200,5):
            plt.tick_params(axis='y', which='both', left=True,right=False, labelleft=True,labelsize=10)
            plt.ylabel('Pred-Obs ($g\,m^{-2}$)',fontsize=14)
            ax.yaxis.set_tick_params(labelsize=13)
            #plt.ylabel('Pred-Obs'+Variable,fontsize=12)
        else:
            plt.tick_params(axis='y', which='both', left=False,right=False, labelleft=False,labelsize=10)
        if pos in list(range(len(Grouping)-4,len(Grouping)+1)):
            plt.tick_params(axis='x', which='both', bottom=True,top=False, labelbottom=True,labelsize=10)
            plt.xlabel('TtAccumSow',fontsize=14)
            ax.xaxis.set_tick_params(labelsize=13)
        else:
            plt.tick_params(axis='x', which='both', bottom=False,top=False, labelbottom=False,labelsize=10)
        pos+=1
    plt.tight_layout


# -

# ## Make Graphs of tuber live Wt residuals for each location plotted against thermal time since sowing

Graph = plt.figure(figsize=(15,10))
PlotVar = 'Tuber.Live.Wt'
SortVar = 'Country'
ColorVar = 'Cultivar'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Appendix\res.png',dpi=600,bbox_inches='tight')

# ## Make Graphs of tuber live Wt residuals for each Cultivar plotted against thermal time since sowing

Graph = plt.figure(figsize=(15,20))
PlotVar = 'Tuber.Live.Wt'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Manuscript\Figures\FIG.jpeg',dpi=600,bbox_inches='tight')

# ## Make Graphs of LAI residuals for each cultivar plotted against thermal time since sowing

Graph = plt.figure(figsize=(15,10))
PlotVar = 'Leaf.LAI'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)

Graph = plt.figure(figsize=(15,10))
PlotVar = 'Leaf.Live.NConc'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)

Graph = plt.figure(figsize=(15,20))
PlotVar = 'Leaf.Live.Wt'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)

Graph = plt.figure(figsize=(15,20))
PlotVar = 'Stem.Live.Wt'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)

Graph = plt.figure(figsize=(15,20))
PlotVar = 'Stem.Live.Wt'
SortVar = 'Cultivar'
ColorVar = 'Country'
ResidulesVsTtSow(PlotVar,SortVar,ColorVar)

Graph = plt.figure(figsize=(15,10))
ResidulesVsTtSow('Leaf.LAI','Cultivar','Country')

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
#Graph.savefig(r'C:\Users\cflhxb\Dropbox\APSIMPotato\Paper docs\Paper 1\Figures\cultivar_vs_loc.tif',dpi=1200,bbox_inches='tight')
