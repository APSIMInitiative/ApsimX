# Split Wheat Validation Files
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/Wheat.apsimx -s ./Tests/Validation/Wheat/split.json
# Split FAR Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/FAR/FAR.apsimx -s ./Tests/Validation/Wheat/FAR/split.json
# Split Wheat Phenology Validation
dotnet ./bin/Release/net8.0/APSIM.Workflow.dll -d ./Tests/Validation/Wheat/Phenology/Phenology.apsimx -s ./Tests/Validation/Wheat/Phenology/split.json
