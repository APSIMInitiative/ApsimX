# Split Wheat Validation Files
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/Wheat.apsimx -s ./Tests/Validation/Wheat/split.json
# Split FAR Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/FAR/FAR.apsimx -s ./Tests/Validation/Wheat/FAR/split.json
# Split Wheat Phenology Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/Phenology/Phenology.apsimx -s ./Tests/Validation/Wheat/Phenology/split.json
# Split Eucalyptus Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Eucalyptus/Eucalyptus.apsimx -s ./Tests/Validation/Eucalyptus/split.json
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d "./Tests/Validation/Eucalyptus/EKB.250603b/EKB 250603b.apsimx" -s ./Tests/Validation/Eucalyptus/EKB.250603b/split.json
# Split Barley Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Barley/Barley.apsimx -s ./Tests/Validation/Barley/split.json
# Split Pinus Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Pinus/Pinus.apsimx -s ./Tests/Validation/Pinus/split.json