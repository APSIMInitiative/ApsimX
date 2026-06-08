#' Apply Tailored Corrections to Wide Observation Data
#'
#' @description
#' Applies specific, hard-coded structural corrections to the finalized wide 
#' observation dataframe before exporting to Excel. Currently configured to 
#' rename legacy variables, drop redundant columns, fix percentage formats, 
#' and scale specific measurement units.
#'
#' @details
#' **Safe Execution:** The function checks if the target column exists before 
#' attempting to modify it, preventing pipeline crashes. If a correction is applied, 
#' it generates a highly visible console warning to maintain an audit trail.
#'
#' @param df A data.frame containing the wide-format observation data.
#'
#' @return The corrected data.frame.
#'
#' @importFrom dplyr rename select mutate
#' @export
apply_corrections_Gna24 <- function(df) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  
  correction_logs <- c()
  
  # ------------------------------------------------------------------
  # 1. CORRECTION: Rename Grain Size
  # ------------------------------------------------------------------
  if ("Wheat.Spike.Grain.Size" %in% names(df)) {
    
    df <- df %>%
      dplyr::rename("Wheat.Grain.Size" = "Wheat.Spike.Grain.Size")
    
    correction_logs <- c(
      correction_logs, 
      " -> RENAMED   : 'Wheat.Spike.Grain.Size' to 'Wheat.Grain.Size'"
    )
  }
  
  # ------------------------------------------------------------------
  # 2. CORRECTION: Drop Redundant Yield Column
  # ------------------------------------------------------------------
  # The raw data contains both 'Wheat.Grain.Yield' and 'Yield'. We drop the vague one.
  if ("Yield" %in% names(df)) {
    
    df <- df %>%
      dplyr::select(-Yield)
    
    correction_logs <- c(
      correction_logs, 
      " -> DROPPED   : Redundant 'Yield' column (retained 'Wheat.Grain.Yield')"
    )
  }
  
  # ------------------------------------------------------------------
  # 3. CORRECTION: Nutrient Concentration Fix (Percentage to Fractional)
  # ------------------------------------------------------------------
  # Find all columns containing "NConc" or "WSC"
  target_cols <- grep("NConc|WSC", names(df), value = TRUE)
  
  for (col in target_cols) {
    if (is.numeric(df[[col]])) {
      # If any value is > 1, we assume the lab reported percentages (0-100) instead of fractions (0-1)
      if (any(df[[col]] > 1, na.rm = TRUE)) {
        
        df[[col]] <- df[[col]] / 100
        
        correction_logs <- c(
          correction_logs, 
          sprintf(" -> CONVERTED : '%s' divided by 100 (percentage to fraction)", col)
        )
      }
    }
  }
  
  # ------------------------------------------------------------------
  # 4. CORRECTION: Leaf Height Unit Scaling 
  # ------------------------------------------------------------------
  if ("Wheat.Leaf.Height" %in% names(df)) {
    if (is.numeric(df[["Wheat.Leaf.Height"]])) {
      
      df[["Wheat.Leaf.Height"]] <- df[["Wheat.Leaf.Height"]] * 10
      
      correction_logs <- c(
        correction_logs, 
        " -> CONVERTED : 'Wheat.Leaf.Height' multiplied by 10 (unit scaling)"
      )
    }
  }
  
  # ------------------------------------------------------------------
  # TRIGGER NOTICES
  # ------------------------------------------------------------------
  if (length(correction_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " \u26A0\uFE0F  TAILORED CORRECTIONS APPLIED TO WIDE OBS DATAFRAME \u26A0\uFE0F ",
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