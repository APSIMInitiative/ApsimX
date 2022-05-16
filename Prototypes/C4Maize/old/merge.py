#!/usr/bin/python
from glob import glob
import os.path as path
import pandas
from re import sub

def isHeaderLine(line):
    return '=' in line or '(mm' in line

# Get the indices of the header rows.
def getHeaderRows(fileName):
    with open(fileName, 'r') as file:
        lines = file.readlines()
        ignored = []
        for i in range(0, len(lines)):
            line = lines[i]
            if isHeaderLine(line):
                ignored.append(i)
        ignored.append(ignored[len(ignored) - 1] + 2)
        return ignored

# Change date formats to yyyy-MM-dd
def fixDate(date):
    return sub(r'^(\d+)/(\d+)/(\d+)', r'\3-\2-\1', date)

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
    files = glob('raw/*.out')
    data = []
    simNames = getSimNameLookup('names.txt')
    for file in files:
        ignoredRows = getHeaderRows(file)
        df = pandas.read_csv(file, skiprows = ignoredRows, sep = '\s+')
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