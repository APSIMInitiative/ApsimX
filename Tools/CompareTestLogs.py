from decimal import Decimal
from os.path import exists
 
SEARCH_TEXT = " has finished. Elapsed time was "

class sim:
    name = ""
    time = 0

def getSimName(line):
    if ')' in line:
        return line[:line.find(')')+1]
    else:
        return line[:line.find(SEARCH_TEXT)]
    
def getSimTime(line):
    end_cut_off = line[0:line.rfind(' ')]
    num_str = end_cut_off[end_cut_off.rfind(' ')+1:]
    return Decimal(num_str)
    
def makeSimsList(lines):
    sims = []
    for line in lines:
        if SEARCH_TEXT in line:
            s = sim()
            s.name = getSimName(line)
            s.time = getSimTime(line)
            if len(s.name) == 0:
                print(line)
            sims.append(s)
    return sims

def main():
    print("This script will compare two jenkin logs and output the difference")
    print("in time for each simulation that was run.")
    print(" ")
    print("Built for APSIM NextGen Build 7256")
    print(" ")

    logFile1 = input("Enter filename with extension for first log file (log1.txt):")
    logFile2 = input("Enter filename with extension for second log file (log1.txt):")
    
    if exists(logFile1) == False:
        print("Invalid Filename " + logFile1)
        return False;
        
    if exists(logFile2) == False:
        print("Invalid Filename " + logFile2)
        return False;

    with open(logFile1,'r') as file:
        sims1 = makeSimsList(file.readlines())
        
    with open(logFile2,'r') as file:
        sims2 = makeSimsList(file.readlines())
    #this is not efficent at all, but it works
    output = ""
    for i in range(0, len(sims2)):
        s2 = sims2[i]
        found = False
        for j in range(0, len(sims1)):
            s1 = sims1[j]
            if s1.name == s2.name:
                output += s1.name + ","
                output += str(s1.time) + ","
                output += str(s2.time) + ","
                output += str(s2.time - s1.time) + "\n"
                found = True
        if not found:
            print(s2.name + " was not found in first log file")
        
    with open('output.csv','w') as file:
        file.write(output)
        
#program start        
main()