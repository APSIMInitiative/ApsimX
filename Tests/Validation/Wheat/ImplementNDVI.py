# ---
# jupyter:
#   jupytext:
#     formats: ipynb,py:light
#     text_representation:
#       extension: .py
#       format_name: light
#       format_version: '1.5'
#       jupytext_version: 1.15.0
#   kernelspec:
#     display_name: Python 3 (ipykernel)
#     language: python
#     name: python3
# ---

import datetime
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import xml.etree.ElementTree as ET
import xmltodict, json
import ast
import numbers
import shlex # package to construct the git command to subprocess format
import subprocess 
import os
# %matplotlib inline

MasterFile = 'C:\GitHubRepos\ApsimX\Tests\Validation\Wheat\Wheat.apsimx'


# +
def findModel(Parent,modelPath):
    PathElements = modelPath.split('.')
    return findModelFromElements(Parent,PathElements)

def findModelFromElements(Parent,PathElements):
    for pe in PathElements:
        Parent = findNextChild(Parent,pe)
    return Parent

def findNextChild(Parent,ChildName):
    if len(Parent['Children']) >0:
        for child in range(len(Parent['Children'])):
            if Parent['Children'][child]['Name'] == ChildName:
                return Parent['Children'][child]
    else:
        return Parent[ChildName]

def swapModels(Parent,modelName,modelType):
    pos=0
    for c in Parent['Children']:
        if (c['Name'] == modelName) and (c['$type'] == modelType):
            params = {c["Parameters"][0]['Key']:float(c["Parameters"][0]['Value']),
                     c["Parameters"][1]['Key']:float(c["Parameters"][1]['Value']),
                     c["Parameters"][2]['Key']:float(c["Parameters"][2]['Value']),
                     c["Parameters"][3]['Key']:float(c["Parameters"][3]['Value'])}
            NewModel = {
                          "$type": "Models.Sensor.Spectral, Models",
                          "DrySoilNDVI": params["DrySoilNDVI"],
                          "WetSoilNDVI": params["WetSoilNDVI"],
                          "GreenCropNDVI": params["GreenCropNDVI"],
                          "DeadCropNDVI": params["DeadCropNDVI"],
                          "NDVI": 0.0,
                          "Name": "Spectral",
                          "ResourceName": None,
                          "Children": [],
                          "Enabled": True,
                          "ReadOnly": False
                        }
            Parent['Children'].append(NewModel)
            del Parent['Children'][pos]            
        swapModels(c,modelName,modelType)
        pos+=1


# +
## Read wheat test file into json object
with open(MasterFile,'r') as MasterJSON:
    Master = json.load(MasterJSON)
    MasterJSON.close()

swapModels(Master,"NDVIModel","Models.Manager, Models")

with open(MasterFile,'w') as WheatTestsJSON:
    json.dump(Master ,WheatTestsJSON,indent=2)
# -

replacements = {'[NDVIModel].Script.NDVI':'[

# +
#replacements = pd.read_excel(VariableRenamesFile,index_col=0,sheet_name = 'SimpleLeafRenames').to_dict()['SimpleLeaf']
with open(ImplementedFile, 'r') as file: 
    data = file.read() 
    for v in replacements.keys():
        data = data.replace(v, replacements[v])
        w = v.replace('Wheat','[Wheat]')
        rw = replacements[v].replace('Wheat','[Wheat]')
        data = data.replace(w, rw)
        
# Opening our text file in write only 
# mode to write the replaced content 
with open(ImplementedFile, 'w') as file: 
  
    # Writing the replaced data in our 
    # text file 
    file.write(data) 

# +
VariableRenames = pd.read_excel(VariableRenamesFile,index_col=0, sheet_name='SimpleLeafRenames').to_dict()['SimpleLeaf']
MaxLeafSizeRenames = pd.read_excel(VariableRenamesFile,index_col=0, sheet_name='MaxLeafSizeRenames').to_dict()['SimpleLeaf']

from pathlib import Path
fileLoc = 'C:\GitHubRepos\ApsimX\Tests\Validation\Wheat\data'
Allcols = []
pathlist = Path(fileLoc).glob('**/*.xlsx')
for path in pathlist:
    # because path is object not string
    obsDat = pd.read_excel(path, engine='openpyxl',sheet_name='Observed')
    newCols = []
    replace = False
    for c in obsDat.columns:
        if c in VariableRenames.keys():
            newCols.append(c.replace(c,VariableRenames[c]))
            replace = True
            if c == "Wheat.Leaf.Tips":
                print(str(path) + " tips")
        else:
            newCols.append(c)
    if replace == True:
        obsDat.columns = newCols
        with pd.ExcelWriter(path, engine='openpyxl', mode='a',if_sheet_exists='replace') as writer: 
            workbook = writer.book
            obsDat.to_excel(writer,index=False,sheet_name='Observed')
    
    try:
        obsDat = pd.read_excel(path, engine='openpyxl',sheet_name='MaxLeafSize')
        newCols = []
        replace = False
        for c in obsDat.columns:
            if c in MaxLeafSizeRenames.keys():
                newCols.append(c.replace(c,MaxLeafSizeRenames[c]))
                replace = True
            else:
                newCols.append(c)
        if replace == True:
            obsDat.columns = newCols
            with pd.ExcelWriter(path, engine='openpyxl', mode='a',if_sheet_exists='replace') as writer: 
                workbook = writer.book
                obsDat.to_excel(writer,index=False,sheet_name='MaxLeafSize')
    except:
        do = "Nothing"
