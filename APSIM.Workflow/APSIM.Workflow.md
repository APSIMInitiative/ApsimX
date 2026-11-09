# APSIM.Workflow

APSIM.Workflow can split large validation files into smaller experiments. The
validation GitHub workflow builds a Docker image containing the validation
files and uses APSIM.Workflow to submit an Azure Batch task for each file.

> [!IMPORTANT]
> This project is not intended for general use and is only intended for use by the APSIM Build system (GitHub Action Workflow)
