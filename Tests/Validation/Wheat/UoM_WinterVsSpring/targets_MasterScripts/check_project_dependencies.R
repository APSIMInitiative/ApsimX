#' Check for required APSIM external file dependencies (Universal Master)
#'
#' @description
#' Iterates through a vector of project names, constructs the expected file paths,
#' and verifies their existence. Halts execution with a clean, formatted list 
#' if any required inputs are missing.
#'
#' @param projects Character vector. One or more project names (e.g., c("Dookie2024", "Wagga2024")).
#' @param dir_met Character. Base directory path for .met files.
#' @param dir_inputs Character. Base directory path for parameter inputs.
#' @param dir_obs Character. Base directory path for observed data files.
#'
#' @return Logical TRUE (invisibly) if all files exist.
#' @export
check_project_dependencies <- function(projects, dir_met, dir_inputs, dir_obs) {
  
  all_missing_files <- c()
  
  for (proj in projects) {
    # Construct the 4 expected file paths for this specific project
    files_to_check <- c(
      file.path(dir_met, paste0(proj, ".met")),
      file.path(dir_inputs, paste0(proj, "_HaunStagesInput.csv")),
      file.path(dir_inputs, paste0(proj, "_PhenoDatesInput.csv")),
      file.path(dir_obs, paste0(proj, "_Observed.xlsx"))
    )
    
    missing <- files_to_check[!file.exists(files_to_check)]
    all_missing_files <- c(all_missing_files, missing)
  }
  
  if (length(all_missing_files) > 0) {
    error_box <- c(
      "",
      "======================================================================",
      " \u26A0\uFE0F PRE-FLIGHT CHECK FAILED: MISSING DEPENDENCIES \u26A0\uFE0F",
      "======================================================================",
      " The pipeline cannot start because the following required files",
      " were not found on disk:",
      "",
      paste("   ->", all_missing_files),
      "======================================================================",
      ""
    )
    stop(paste(error_box, collapse = "\n"), call. = FALSE)
  }
  
  message("\u2705 Pre-flight check passed: All required dependencies found.")
  return(invisible(TRUE))
}