#' Check for required APSIM external file dependencies
#'
#' Iterates through a vector of project names, constructs the expected file paths
#' based on standard locations, and verifies their existence. Halts execution
#' and lists all missing files if any are not found.
#'
#' @param projects Character vector. One or more project names (e.g., c("Dookie2024", "WaggaWagga2024")).
#' @param dir_met Character. Base directory path for .met files.
#' @param dir_inputs Character. Base directory path for parameter inputs.
#' @param dir_obs Character. Base directory path for observed data files.
#' @return Logical TRUE (invisibly) if all files exist. Throws an error otherwise.
#' @export
check_project_dependencies <- function(projects, dir_met, dir_inputs, dir_obs) {
  
  # Initialize an empty vector to collect any missing file paths
  all_missing_files <- c()
  
  for (proj in projects) {
    # Construct the 4 expected file paths for this specific project
    files_to_check <- c(
      file.path(dir_met, paste0(proj, ".met")),
      file.path(dir_inputs, paste0(proj, "_HaunStagesInput.csv")),
      file.path(dir_inputs, paste0(proj, "_PhenoDatesInput.csv")),
      file.path(dir_obs, paste0(proj, "_Observed.xlsx"))
    )
    
    # Identify which of these constructed paths do not actually exist
    missing <- files_to_check[!file.exists(files_to_check)]
    
    # Append to our master tracking list
    all_missing_files <- c(all_missing_files, missing)
  }
  
  # If anything is missing, stop the script and print a clean list of what to fix
  if (length(all_missing_files) > 0) {
    stop(
      sprintf(
        "Pre-flight check failed! Missing %d required files:\n  - %s\n\nPlease locate these files before running the pipeline.", 
        length(all_missing_files),
        paste(all_missing_files, collapse = "\n  - ")
      ),
      call. = FALSE
    )
  }
  
  message("Pre-flight check passed: All dependencies found for all projects.")
  return(invisible(TRUE))
}