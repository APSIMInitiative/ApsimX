from asyncio.windows_events import NULL
import json
from lib2to3.pytree import convert
from os.path import exists

#main, start point
def main():
    quit = False
    while quit == False:
        quit = interface()
    print("Complete")
    input("Press ENTER to Quit.")

def interface():

    print("This script will convert SoilNitrogen, SoilNitrogen, SoilNitrogenNO3,")
    print("SoilNitrogenNH4 and SoilNitrogenUrea nodes in an apsimx file.")
    print(" ")
    print("These nodes will be replaced by a Nutrient node and Solute nodes.")
    print(" ")
    print("Built for APSIM NextGen Build 7177")
    print(" ")

    inputFile = input("Enter filename with Extension (input.apsimx):")
    outputFile = input("Enter output filename with Extension (output.apsimx):")
    #inputFile = "Grapevine.apsimx"
    #outputFile = "output.apsimx"

    if exists(inputFile) == False:
        input("Invalid Filename")
        return False;

    counts = dict()
    counts['SoilNitrogen'] = 0
    counts['SoilNitrogenNO3'] = 0
    counts['SoilNitrogenNH4'] = 0
    counts['SoilNitrogenUrea'] = 0
    counts['ScriptSoilNitrogen'] = 0
    counts['ReportSoilNitrogen'] = 0

    with open(inputFile, "r") as inf:
        data = json.load(inf)
        counts = countNodes(data, counts)

    inf.close()

    print("Nodes Found:")
    print("SoilNitrogen: " + str(counts['SoilNitrogen']))
    print("SoilNitrogenNO3: " + str(counts['SoilNitrogenNO3']))
    print("SoilNitrogenNH4: " + str(counts['SoilNitrogenNH4']))
    print("SoilNitrogenUrea: " + str(counts['SoilNitrogenUrea']))
    print(" ")
    print("References In Scripts:")
    print("SoilNitrogen: " + str(counts['ScriptSoilNitrogen']))
    print(" ")
    print("References In Reports:")
    print("SoilNitrogen: " + str(counts['ReportSoilNitrogen']))
    print(" ")

    convertYN = ""
    while convertYN != 'y' and convertYN != 'Y' and convertYN != 'n' and convertYN != 'N':
        convertYN = input("Continue with Conversion? (Y/N):")

    if convertYN == 'Y' or convertYN == 'y':
        print("Processing...")
        convertNodes(data)

        with open(outputFile, 'w') as outf:
            output = json.dump(data, outf, indent=2)

        outf.close()
        print(" ")
        print("Open your file in APSIM to generate Nutrient graphs and save the file to finish this conversion")
        print(" ")

    return True

#looks at the node structure and counts how many SoilNitrogen, SoilNitrogenNO3, SoilNitrogenNH4 
#and SoilNitrogenUrea nodes there are.
#Recursive function
def countNodes(node, counts):
    if '$type' in node:
        if node['$type'] == "Models.Soils.SoilNitrogen, Models":
            counts['SoilNitrogen'] = counts['SoilNitrogen'] + 1
        elif node['$type'] == "Models.Soils.SoilNitrogenNO3, Models":
            counts['SoilNitrogenNO3'] = counts['SoilNitrogenNO3'] + 1
        elif node['$type'] == "Models.Soils.SoilNitrogenNH4, Models":
            counts['SoilNitrogenNH4'] = counts['SoilNitrogenNH4'] + 1
        elif node['$type'] == "Models.Soils.SoilNitrogenUrea, Models":
            counts['SoilNitrogenUrea'] = counts['SoilNitrogenUrea'] + 1
        elif node['$type'] == "Models.Manager, Models":
            if 'Code' in node:
                if node['Code'].find('SoilNitrogen ') > -1:
                    counts['ScriptSoilNitrogen'] = counts['ScriptSoilNitrogen'] + 1
        elif node['$type'] == "Models.Report, Models":
            if 'VariableNames' in node:
                if node['VariableNames'] != None:
                    for v in node['VariableNames']:
                        if v.find('SoilNitrogen.') > -1:
                            counts['ReportSoilNitrogen'] = counts['ReportSoilNitrogen'] + 1

    if 'Children' in node:
        for child in node["Children"]:
            counts = countNodes(child, counts)

    return counts

#converts the SoilNitrogen, SoilNitrogenNO3, SoilNitrogenNH4 and SoilNitrogenUrea nodes to
#Nutrient and Solute nodes with same values
#Nutrient Node is left blank to be generated when the file is loaded
#Recursive function
def convertNodes(node):
    if '$type' in node:
        if node['$type'] == "Models.Soils.SoilNitrogen, Models":
            node.clear()
            node['$type'] = "Models.Soils.Nutrients.Nutrient, Models"
            graph = dict()
            graph['$type'] = "APSIM.Shared.Graphing.DirectedGraph, APSIM.Shared"
            graph['Nodes'] = []
            graph['Arcs'] = []
            node['DirectedGraphInfo'] = graph
            node['SurfaceResidueDecomposition'] = NULL
            node['ResourceName'] = "Nutrient"
            node['Name'] = "Nutrient"
            node['Enabled'] = True
            node['ReadOnly'] = False

        elif node['$type'] == "Models.Soils.SoilNitrogenNO3, Models":
            node['$type'] = "Models.Soils.Solute, Models"

        elif node['$type'] == "Models.Soils.SoilNitrogenNH4, Models":
            node['$type'] = "Models.Soils.Solute, Models"

        elif node['$type'] == "Models.Soils.SoilNitrogenUrea, Models":
            node['$type'] = "Models.Soils.Solute, Models"

        elif node['$type'] == "Models.Manager, Models":
            if 'Code' in node:
                if node['Code'].find('SoilNitrogen') > -1:
                       node['Code'] = node['Code'].replace('SoilNitrogen ', 'Nutrient ')

        elif node['$type'] == "Models.Report, Models":
            if 'VariableNames' in node and node['VariableNames'] != None:
                for i in range(len(node['VariableNames'])):
                    v = node['VariableNames'][i]
                    if v.find('SoilNitrogen.FOMN') > -1:
                        node['VariableNames'][i] = v.replace('SoilNitrogen.FOMN', 'Nutrient.FOM.N')
                    elif v.find('SoilNitrogen.NFlow') > -1:
                        node['VariableNames'][i] = v.replace('SoilNitrogen.NFlow', 'Nutrient.NFlow.Value')
                    elif v.find('SoilNitrogen.mineral_n') > -1:
                        node['VariableNames'][i] = v.replace('SoilNitrogen.mineral_n', 'Nutrient.MineralN')
                    elif v.find('SoilNitrogen.Denitrification') > -1:
                        node['VariableNames'][i] = v.replace('SoilNitrogen.Denitrification', 'Nutrient.DenitrifiedN')
                    elif v.find('SoilNitrogen.') > -1:
                        node['VariableNames'][i] = v.replace('SoilNitrogen.', 'Nutrient.')

    if 'Children' in node:
        for child in node["Children"]:
            convertNodes(child)

#Prints the node structure to console with tabbing for depth
#Recursive function
def printNodes(node, count):

    for i in range(count):
        print('\t', end='')
    if '$type' in node:
        print(node["$type"])
    else:
        print("No Type")

    if 'Children' in node:
        for child in node["Children"]:
            printNodes(child, count+1)

#main call to start program
main()