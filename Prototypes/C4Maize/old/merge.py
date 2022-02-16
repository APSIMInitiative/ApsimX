#!/usr/bin/python
import glob
import os.path as path
import pandas
import re

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

# Change date formats to yyyy-MM-dd
def fixDate(date):
    return re.sub(r'^(\d+)/(\d+)/(\d+)', r'\3-\2-\1', date)

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

def main():
    files = glob.glob('raw/*.out')
    data = []
    simNames = getSimNameLookup('names.txt')
    for file in files:
        ignoredRows = getHeaderRows(file)
        df = pandas.read_csv(file, skiprows = ignoredRows)
        df['Date'] = df['Date'].apply(fixDate)
        simName = path.splitext(path.basename(file))[0]
        simName = simName.replace('-', '_')
        if simName in simNames:
            simName = simNames[simName]
        df['SimulationName'] = simName
        data.append(df)

    # Concatenate all data frames into a single big dataframe
    combined = pandas.concat(data, sort = False)
    combined.to_csv('combined.csv', header = True, index = False)

main()