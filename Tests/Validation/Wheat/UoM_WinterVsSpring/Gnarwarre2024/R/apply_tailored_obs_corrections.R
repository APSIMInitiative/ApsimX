#' Apply Tailored Corrections to Wide Observation Data
#'
#' @description
#' Applies specific, hard-coded structural corrections to the finalized wide 
#' observation dataframe before exporting to Excel. Currently configured to 
#' rename legacy variable names to match updated APSIM-X parameter paths.
#'
#' @details
#' **Safe Execution:** The function checks if the target column exists before 
#' attempting to modify it, preventing pipeline crashes if the raw data 
#' structure changes upstream. If a correction is applied, it generates a 
#' highly visible console warning to maintain an audit trail.
#'
#' @param df A data.frame containing the wide-format observation data.
#'
#' @return The corrected data.frame.
#'
#' @importFrom dplyr rename
#' @export
apply_tailored_obs_corrections <- function(df) {
  
  require(dplyr)
  
  correction_logs <- c()
  
  # ------------------------------------------------------------------
  # 1. CORRECTION: Rename Grain Size
  # ------------------------------------------------------------------
  if ("Wheat.Spike.Grain.Size" %in% names(df)) {
    
    df <- df %>%
      dplyr::rename("Wheat.Grain.Size" = "Wheat.Spike.Grain.Size")
    
    # Log the exact string requested
    correction_logs <- c(
      correction_logs, 
      " -> 'Wheat.Spike.Grain.Size' column renamed to 'Wheat.Grain.Size' in Observed.xls"
    )
  }
  
  # Note: You can easily add more tailored corrections here in the future
  # using the exact same if (...) { ... } pattern.
  
  # ------------------------------------------------------------------
  # 2. TRIGGER NOTICES
  # ------------------------------------------------------------------
  if (length(correction_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️  TAILORED CORRECTIONS APPLIED TO WIDE OBS DATAFRAME ⚠️ ",
      "======================================================================",
      correction_logs,
      "======================================================================",
      ""
    )
    
    # Print the large visual box to the console
    message(paste(warning_box, collapse = "\n"))
    
    # Trigger a native R warning so targets flags it in tar_meta()
    warning("Tailored structural corrections were applied. See console for details.", call. = FALSE)
  }
  
  return(df)
}