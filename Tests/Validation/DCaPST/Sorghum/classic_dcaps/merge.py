#!/usr/bin/python
import logging
import glob
import os
import os.path as path
import pandas
import re

class MergeTool():
    def __init__(self, mapping_file):
        self.mapping_file = mapping_file

        if not path.exists(self.mapping_file):
            raise Exception(f"Cannot find mapping file: {self.mapping_file}")
    
    # Get the indices of the header rows.
    def getHeaderRows(self, fileName):
        with open(fileName, 'r') as file:
            lines = file.readlines()
            ignored = []
            for i in range(0, len(lines)):
                line = lines[i]
                if '=' in line:
                    ignored.append(i)
            ignored.append(ignored[len(ignored) - 1] + 2)
            return ignored

    def getSimNameLookup(self, filename):
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

    def combineResults(self, directory):
        files = glob.glob(path.join(directory, '*.out'))
        simNames = self.getSimNameLookup(self.mapping_file)
        data = []
        date_rx = r'^(\d{2})/(\d{2})/(\d{4})'
        date_repl = r'\3-\2-\1'
        for file in files:

            if not self.shouldProcessFile(file, simNames):
                logging.info(f"Ignoring file: {file} because no mapping exists in: {self.mapping_file}")
                continue

            logging.debug(f"Processing file: {file}")

            ignoredRows = self.getHeaderRows(file)
            df = pandas.read_csv(file, skiprows = ignoredRows)
            simName = path.splitext(path.basename(file))[0]
            if simName in simNames: simName = simNames[simName]
            
            if not 'SimulationName' in df: 
                df['SimulationName'] = simName
            else:
                raise Exception(f"Could not find simulation name column in {file}")    
            
            if 'date' in df:
                df['date'] = df['date'].apply(lambda x: re.sub(date_rx, date_repl, x))
            elif 'Date' in df:
                df['Date'] = df['Date'].apply(lambda x: re.sub(date_rx, date_repl, x))
            else:
                raise Exception(f"Could not find date column in {file}")
                
            data.append(df)

        # Concatenate all data frames into a single big dataframe
        combined = pandas.concat(data, sort = False)
        combined.to_csv('combined_%s.csv' % directory, header = True, index = False)

    def shouldProcessFile(self, file, simNames):
        file_sim_name = os.path.basename(file)
        file_sim_name_without_extension = os.path.splitext(file_sim_name)[0]
        return simNames.__contains__(file_sim_name_without_extension)

try:
    logging.basicConfig(format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', level=logging.INFO)

    simulations_directory_name = 'simulations'
    merge_tool = MergeTool('names.txt')
    merge_tool.combineResults(simulations_directory_name)
except:
    logging.exception(f"Exception raised when running: {os.path.basename(__file__)}")