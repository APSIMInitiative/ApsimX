#' Universal Phenology Integrity Gatekeeper
#'
#' @description
#' Scans the final APSIM phenology parameter matrix for missing dates (NAs).
#' If any missing values are found, it halts the pipeline and prints a detailed
#' report of the offending simulations and missing parameters to prevent APSIM crashes.
#'
#' @param df_pheno The wide dataframe containing SimulationName and DateToProgress columns.
#' @return The original dataframe (if it passes) or throws a fatal error (if it fails).
#' @export
check_pheno_integrity <- function(df_pheno) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package 'tidyr' required.")
  
  # 1. Look for any NAs in the entire dataframe
  has_nas <- any(is.na(df_pheno))
  
  # 2. If no NAs, pass the data through safely
  if (!has_nas) {
    message("✅ SUCCESS: Phenology Input Matrix passed integrity check. No missing dates.")
    return(df_pheno)
  }
  
  # 3. If NAs exist, build a diagnostic report
  missing_report <- df_pheno %>%
    dplyr::filter(dplyr::if_any(dplyr::everything(), is.na)) %>%
    # Pivot long to figure out exactly which columns are missing
    tidyr::pivot_longer(
      cols = -SimulationName, 
      names_to = "Parameter", 
      values_to = "Value"
    ) %>%
    dplyr::filter(is.na(Value)) %>%
    dplyr::select(SimulationName, Parameter)
  
  # 4. Format the error message
  error_msg <- c(
    "",
    "======================================================================",
    " 🚨 FATAL ERROR: MISSING PHENOLOGY DATES DETECTED 🚨 ",
    "======================================================================",
    " APSIM will crash if 'DateToProgress' parameters are left blank.",
    " The following simulations are missing data:",
    ""
  )
  
  # Add the specific broken rows to the message
  for (i in 1:nrow(missing_report)) {
    error_msg <- c(
      error_msg, 
      sprintf(" -> Simulation: '%s' | Missing: '%s'", 
              missing_report$SimulationName[i], missing_report$Parameter[i])
    )
  }
  
  error_msg <- c(
    error_msg,
    "======================================================================",
    " ACTION REQUIRED: Fix the raw data or apply an imputation function.",
    ""
  )
  
  # Print the massive warning and stop the pipeline
  message(paste(error_msg, collapse = "\n"))
  stop("Pipeline Halted: Phenology Matrix Integrity Check Failed.")
}