# APSIM.Workflow

APSIM.Workflow is used to take the validation data, split it by experiment and organise all input files for each of the new split apsim files.
It then creates a workflow.yml file for use with CSIRO Workflo tool which sends all new smaller experiments across to Azure for running.

> [!IMPORTANT]
> This project is not intended for general use and is only intended for use by the APSIM Build system (GitHub Action Workflow)
