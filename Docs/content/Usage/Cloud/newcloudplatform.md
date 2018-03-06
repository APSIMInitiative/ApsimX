---
title: "Interfacing with a new cloud platform"
draft: false
---

This document outlines the general steps needed to add job submission and job viewer functionality from a new cloud platform to APSIM Next Generation.

At the time of writing this document (early 2018), Microsoft Azure is the only cloud platform accessible through APSIM.

## Cloud Selection

Currently there is no way to select a cloud platform from a list of those accessible through APSIM (Azure is currently the only one). If a new platform is added, the user will need some way to select which one to use when submitting or viewing a job. This task is left as an exercise for the enthusiastic developer.

## Views and Presenters

The `CloudJobDisplayView`, `NewCloudJobView`, and `DownloadWindow` classes are cloud-agnostic - they display information provided to them, but do not directly interface with any cloud service. This means that they may be reused to display jobs from any cloud platform, provided that the same information is to be displayed.

Any presenter controlling these views must inherit from a specific interface, which describes a standard set of functionality which the view requires of its presenter. Specific details of these views are given below.

### Job Submission

`CloudJobDisplayView` is the view controlling the job submission. In theory, the user inputs needed to submit a job (e.g. name, number of CPU cores) should be very similar no matter which cloud platform the job is being submitted to, allowing this view to be reused.

The presenter must inherit from `INewCloudJobPresenter`, which defines functionality such as job submission and submission cancelling.

Things to note about the new job presenter:

- The presenter must generate a job ID. Under the Azure implementation this is a `System.Guid`.
- Job submission should occur in a separate thread - if the job has millions of simulations, uploading it may take some time.
- The presenter can show the submission status by modifying the view's `Status` property.

### Job Display

`CloudJobDisplayView` is the view controlling the job viewer UI. The presenter must inherit from `ICloudJobPresenter`. The presenter interacts with the view in several ways, such as updating the progress bars, updating the table of jobs, etc.

`DownloadWindow` is a small window that pops up when the user clicks the download button, allowing them to select a few options such as the output directory, whether or not to download debugging files, etc.
It requires a reference to an `ICloudJobPresenter` object, so that it may call the presenter's `DownloadResults()` method when the user clicks to initiate the download.

Things to note about the job display presenter:

- Loading the list of jobs should occur in a separate thread so that the UI remains responsive.
- To update the job table, the presenter must call the `CloudJobDisplayView.UpdateJobTable()` method, passing in a list of JobDetails objects. 
- To update the progress bars, the presenter can simply assign the progress (a double in the range [0, 1]) to the view's `JobLoadProgress` or `DownloadProgress` properties. 
- When detaching the presenter, the view's `Detach()` method should be called. 
- The presenter does not need to handle any of the view's events - generally, when one of the view's events fires, the view will grab data from the relevant input fields and pass it into one of the presenter's methods to be validated and acted upon.