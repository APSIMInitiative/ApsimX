# APSIM.Workflow

APSIM.Workflow can split large validation files into smaller experiments. The
validation GitHub workflow builds a Docker image containing the validation
files and uses APSIM.Workflow to submit an Azure Batch task for each file.

> [!IMPORTANT]
> This project is not intended for general use and is only intended for use by the APSIM Build system (GitHub Action Workflow)

## Reading validation databases from a later Batch run

Each validation job records `output_container_uri`, `pull_request_number` and
`commit_sha` in its Azure Batch job metadata. The container URI has no SAS or
other credentials. Use the job ID and metadata to identify the correct run,
including when a pull request has multiple runs; the job ID and container URI
are also logged after submission. The metadata remains available while the
Batch job exists, and the databases while the container is retained.

The upload SAS grants create and write access only; it is not a read credential.
For a future reader, use `Azure.CreateDatabaseReadUri` to generate a separate
container-scoped SAS with read and list permissions. Construct the
`BlobContainerClient` using the output container URI and a storage account key;
the caller must have access to that key. Generate the SAS when submitting the
reader job and choose an expiry long enough for its queue and execution time.
Do not log the SAS or put it in job metadata.

The reader can use the SAS URI to list and read blobs under `databases/`, or
pass it to `ResourceFile.FromStorageContainerUrl` with `blobPrefix: "databases/"`
to download them as Batch task inputs.
Wait for the validation tasks and their output uploads to complete, and check
task failures before consuming the files. A completed GitHub submission alone
does not mean the databases are ready. The future reader does not exist yet and
must call this helper and pass the SAS to its tasks when it is implemented.
