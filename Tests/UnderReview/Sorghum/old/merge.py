#!/usr/bin/python
import glob
import os.path as path
import pandas

# Get the indices of the header rows.
def getHeaderRows(fileName):
    with open(fileName, 'r') as file:
        lines = file.readlines()
        ignored = []
        for i in range(0, len(lines)):
            line = lines[i]
            if '=' in line:
                ignored.append(i)
        ignored.append(ignored[len(ignored) - 1] + 2)
        return ignored

def getSimNameLookup(filename):
    try:    
        with open(filename, 'r') as file:
            result = {}
            for line in file.readlines():
                if '=' in line:
                    parts = line.split('=')
                    result[parts[0]] = parts[1].strip()
            return result
    except:
        return {}

def combineResults(directory):
    files = glob.glob(path.join(directory, '*.out'))
    simNames = getSimNameLookup('names.txt')
    data = []
    for file in files:
        ignoredRows = getHeaderRows(file)
        df = pandas.read_csv(file, skiprows = ignoredRows)
        simName = path.splitext(path.basename(file))[0]
        if simName in simNames:
            simName = simNames[simName]
        df['SimulationName'] = simName
        data.append(df)

    # Concatenate all data frames into a single big dataframe
    combined = pandas.concat(data, sort = False)
    combined.to_csv('combined_%s.csv' % directory, header = True, index = False)

combineResults('fixed')
combineResults('dynamic')