#' Check for required APSIM external file dependencies (Universal Engine)
#'
#' @description
#' Iterates through a vector of project names, constructs the expected file paths, 
#' and verifies their physical existence on the disk. Halts execution with a clean, 
#' formatted error box if any required inputs are missing, preventing cryptic downstream crashes.
#'
#' @details
#' **Dynamic Weather Naming:** The \code{met_name} parameter is optional. If left as \code{NULL}, 
#' the function defaults to expecting a weather file named exactly \code{"[ProjectName].met"}. 
#' If your weather file has a unique name (e.g., from SILO), pass it explicitly.
#'
#' @param projects Character vector. One or more project names (e.g., \code{c("Dookie2024", "GrassPatch2024")}).
#' @param dir_met Character. Base directory path for .met files.
#' @param dir_inputs Character. Base directory path for parameter inputs.
#' @param dir_obs Character. Base directory path for observed data files.
#' @param met_name Character. Optional explicit name for the weather file (e.g., \code{"Grass Patch_-33.25.met"}). Defaults to NULL.
#'
#' @return Logical TRUE (invisibly) if all files exist. Throws a formatted error otherwise.
#' @export
check_project_dependencies <- function(projects, dir_met, dir_inputs, dir_obs, met_name = NULL) {
  
  # ---- 1. FAIL-FAST CONFIGURATION CHECKS ----
  if (is.null(dir_met) || is.null(dir_inputs) || is.null(dir_obs)) {
    stop("Configuration Error [check_project_dependencies]: One or more directory paths evaluate to NULL. Check your config list!", call. = FALSE)
  }
  
  all_missing_files <- c()
  
  # ---- 2. PATH CONSTRUCTION & VERIFICATION ----
  for (proj in projects) {
    
    # Resolve the expected weather file name dynamically
    expected_met <- if (!is.null(met_name)) met_name else paste0(proj, ".met")
    
    # Construct the 4 standard expected file paths for this specific project
    files_to_check <- c(
      file.path(dir_met, expected_met),
      file.path(dir_inputs, paste0(proj, "_HaunStagesInput.csv")),
      file.path(dir_inputs, paste0(proj, "_PhenoDatesInput.csv")),
      file.path(dir_obs, paste0(proj, "_Observed.xlsx"))
    )
    
    # Identify which of these constructed paths do not actually exist on disk
    missing <- files_to_check[!file.exists(files_to_check)]
    all_missing_files <- c(all_missing_files, missing)
  }
  
  # ---- 3. NOTIFICATION & ERROR FORMATTING ----
  if (length(all_missing_files) > 0) {
    error_box <- c(
      "",
      "======================================================================",
      " ⚠️ PRE-FLIGHT CHECK FAILED: MISSING DEPENDENCIES ⚠️",
      "======================================================================",
      " The pipeline cannot start because the following required files",
      " were not found on disk:",
      "",
      paste("   ->", all_missing_files),
      "======================================================================",
      " Action Required: Ensure these files are placed in their respective folders.",
      ""
    )
    stop(paste(error_box, collapse = "\n"), call. = FALSE)
  }
  
  message("✅ Pre-flight check passed: All required APSIM dependencies found.")
  return(invisible(TRUE))
}