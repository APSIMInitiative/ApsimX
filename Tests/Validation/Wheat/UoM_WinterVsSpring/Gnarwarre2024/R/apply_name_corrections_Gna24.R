#' Universal Variable Name Remapper for Observation Pipelines
#'
#' @description
#' A highly resilient validation function that standardizes variable headers inside 
#' finalized observation data frames. It strictly requires an external translation 
#' mapping dictionary CSV to execute safe column swaps.
#'
#' @details
#' **Pipeline Safety Engineering:** Instead of hardcoded assignments, this routine maps 
#' variables dynamically using Tidyverse selectors. It updates present match groups and 
#' leaves unmapped background variables completely untouched. If the required CSV is 
#' missing or malformed, the function intentionally halts the pipeline to prevent 
#' silent data corruption.
#'
#' @param df_obs Data frame. The long or wide finalized observation dataset requiring name remapping.
#' @param mapping_csv_path String. Path to a layout mapping configuration CSV containing 
#'   \code{RawDataName} and \code{ObservedFileName} headers.
#'
#' @return A processed data frame with uniform, APSIM-compliant variable column names.
#' @export
#'
#' @examples
#' \dontrun{
#' # Dynamic CSV Execution (Strictly Required)
#' df_clean_names <- apply_name_corrections_Gna24(
#'   df_obs = df_final_observed, 
#'   mapping_csv_path = "Gnarwarre2024/Gnarwarre2024_obs_var_new_names.csv"
#' )
#' }
apply_name_corrections_Gna24 <- function(df_obs, mapping_csv_path = NULL) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("stringr", quietly = TRUE)) stop("Package 'stringr' required.")
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_obs) || is.null(df_obs) || nrow(df_obs) == 0) {
    stop("Error [apply_name_corrections_Gna24]: Incoming data frame asset 'df_obs' is missing or empty.")
  }
  
  # Define the instructional error message for missing or invalid CSVs
  err_instruction <- paste(
    "This pipeline strictly requires a mapping CSV to translate lab headers to APSIM standard headers.",
    "The CSV MUST contain exactly these two column headers:",
    "  1. RawDataName      (The original header from the lab)",
    "  2. ObservedFileName (The APSIM-X standard header)",
    "",
    "Example CSV Structure:",
    "RawDataName             , ObservedFileName",
    "Wheat.Leaf.Dead.WSC     , Wheat.Leaf.Dead.WSCc",
    "Wheat.Spike.NConc       , Wheat.Spike.NConc",
    "Wheat.Stem.Wt           , Wheat.Stem.Wt",
    sep = "\n"
  )
  
  if (is.null(mapping_csv_path) || !file.exists(mapping_csv_path)) {
    file_name <- ifelse(is.null(mapping_csv_path), "NULL", basename(mapping_csv_path))
    folder_name <- ifelse(is.null(mapping_csv_path), "Unknown", dirname(mapping_csv_path))
    
    stop(sprintf("\nCRITICAL ERROR: The mapping CSV file '%s' was not found in the folder '%s'.\n%s", 
                 file_name, folder_name, err_instruction), call. = FALSE)
  }
  
  # ---- 2. ESTABLISH THE TRANSLATION MATRIX MAP ----
  df_map_cfg <- tryCatch({
    read.csv(mapping_csv_path, stringsAsFactors = FALSE, header = TRUE)
  }, error = function(e) {
    stop(sprintf("\nCRITICAL ERROR: Failed to read the CSV file '%s'. Reason: %s", basename(mapping_csv_path), e$message), call. = FALSE)
  })
  
  names(df_map_cfg) <- stringr::str_trim(names(df_map_cfg))
  
  if (!all(c("RawDataName", "ObservedFileName") %in% names(df_map_cfg))) {
    stop(sprintf("\nCRITICAL ERROR: The mapping CSV '%s' is missing required columns.\n%s", 
                 basename(mapping_csv_path), err_instruction), call. = FALSE)
  }
  
  # Build standard named vector: c("NewName" = "OldName")
  lookup_map <- structure(
    stringr::str_trim(df_map_cfg$RawDataName), 
    names = stringr::str_trim(df_map_cfg$ObservedFileName)
  )
  message(sprintf("Success [apply_name_corrections_Gna24]: Loaded translation matrix from '%s'.", basename(mapping_csv_path)))
  
  # ---- 3. EXECUTE SAFE DYNAMIC ROW/COLUMN REMAPPING ----
  # Intercept and isolate only the translation elements that are actively present inside this data frame slice
  active_renames <- lookup_map[lookup_map %in% names(df_obs)]
  
  if (length(active_renames) == 0) {
    message("Warning [apply_name_corrections_Gna24]: No matching raw headers located inside input table. Columns left as-is.")
    return(df_obs)
  }
  
  # In dplyr::rename(), the syntax is: rename(new_name = old_name)
  # Passing our named vector using !!! (triple-bang) evaluates the whole mapping array concurrently
  df_remapped <- df_obs %>%
    dplyr::rename(!!!active_renames)
  
  # ---- 4. PIPELINE COMPLETION AUDIT LOGGING ----
  # Extract the old names (values) and new names (names) from the active map
  rename_list <- paste("   [OLD]", unname(active_renames), "--> [NEW]", names(active_renames))
  
  log_box <- c(
    "",
    "----------------------------------------------------------------------",
    " \U0001F504 PIPELINE ACTION: VARIABLE NAMES REMAPPED \U0001F504",
    "----------------------------------------------------------------------",
    sprintf(" Successfully standardized %d columns:", length(active_renames)),
    "",
    rename_list,
    "----------------------------------------------------------------------",
    ""
  )
  
  # Print the detailed UI box to the console
  message(paste(log_box, collapse = "\n"))
  
  # Throw the formal R warning so it is officially flagged in the targets log
  warning(sprintf("Remapped %d variable names to APSIM standards. See console for mapping details.", length(active_renames)), call. = FALSE)
  
  return(df_remapped)
}