#' Apply Tailored Structural Corrections to Grass Validation Observations
#'
#' @description
#' Processes a finalized wide-format observation data frame and applies specific analytical adjustments.
#' In its core implementation, it scans the dataset for all variables containing the token \code{"WSCc"}
#' in their header string and converts their values from percentage points (0-100) to absolute 
#' dry weight fractions (0.0-1.0) as natively required by the APSIM-X PMF organ components.
#'
#' @details
#' **Resilient Vectorization:** This function utilizes dynamic column selection via \code{dplyr::across()} 
#' paired with a regex string match framework. This ensures that whether a dataset contains two WSCc organs 
#' or twenty, they are identified and scaled simultaneously. 
#' 
#' **Type and NA Safety:** It dynamically wraps values in \code{as.numeric()} to safely convert 
#' characters reading as numbers, while carefully preserving structural \code{NA} cells.
#'
#' @param df_obs Data frame. The wide-format, finalized observation data frame (e.g., the output 
#'   of your renaming or harvest-injection layers).
#'
#' @return A processed data frame of the identical dimensions, with all WSCc value ranges 
#'   normalized to fractional scales.
#' @export
#'
#' @examples
#' \dontrun{
#' # Standard pipeline integration step
#' df_final_corrected <- apply_corrections_Grass24(df_final_observed_renamed)
#' }
apply_corrections_Grass24 <- function(df_obs) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_obs) || is.null(df_obs) || nrow(df_obs) == 0) {
    stop("Error [apply_corrections_Grass24]: Incoming data asset 'df_obs' is missing or completely empty.")
  }
  
  # ---- 2. DYNAMICALLY IDENTIFY TARGET COLUMNS ----
  # Scan column names for the "WSCc" pattern (case-insensitive for safety)
  wsc_cols <- grep("WSCc", names(df_obs), ignore.case = TRUE, value = TRUE)
  
  if (length(wsc_cols) == 0) {
    message("Notice [apply_corrections_Grass24]: No columns containing 'WSCc' patterns located. Table returned unchanged.")
    return(df_obs)
  }
  
  # ---- 3. EXECUTE VECTORIZED UNIT CONVERSION PIPELINE ----
  # Using an internal anonymous wrapper function to perform type-safe division
  df_corrected <- df_obs %>%
    dplyr::mutate(dplyr::across(
      .cols = dplyr::all_of(wsc_cols),
      .fns = ~ {
        # Convert to numeric to handle any string-typed inputs from raw sheets safely
        num_val <- as.numeric(.x)
        
        # Divide by 100 to shift from percentage space (e.g. 15.4%) to fraction space (0.154)
        # if_else preserves NA structures flawlessly
        dplyr::if_else(is.na(num_val), NA_real_, num_val / 100)
      }
    ))
  
  # ---- 4. PIPELINE AUDIT REPORTING ----
  message(sprintf("Success [apply_corrections_Grass24]: Normalized %d WSCc concentration variable(s) to dry-weight fractions:\n -> Affected: (%s)", 
                  length(wsc_cols), paste(wsc_cols, collapse = ", ")))
  
  return(df_corrected)
}