#' Calculate Synthetic APSIM Phenology Progression Dates
#'
#' @description
#' This function takes observed dates for primary wheat phenology stages (Emergence, 
#' Stem Elongation, Flowering) and interpolates synthetic intermediate stages 
#' (Spikelets Differentiating and Heading) based on a specified percentage factor.
#' It formats the output with APSIM-compliant column names ready for simulation injection.
#'
#' @details
#' **Defensive Column Handling:** If raw data is entirely missing a specific stage 
#' (e.g., no "Emerging" dates exist), `tidyr::pivot_wider` will not create that column. 
#' This function explicitly checks for and initializes missing base columns with `NA` 
#' to prevent fatal "size 0" pipeline crashes during the calculation phase.
#' 
#' **Missing Data Warnings:** Instead of failing silently or crashing, the function 
#' actively scans for missing prerequisite dates. If it cannot calculate a synthetic 
#' stage, it outputs a clean `NA` for that stage and triggers a descriptive warning 
#' pinpointing the exact `SimulationName` and the specific missing dependencies.
#'
#' **Safe Date Math:** Base R date subtraction yields `difftime` objects. Multiplying 
#' these by fractions can silently convert the resulting addition into a `POSIXct` 
#' Date-Time object. Because APSIM requires strict Date strings, this function converts 
#' differences to numeric days and rounds them to integers before adding them to base dates.
#'
#' @param df_dateWhenStageWasReached A data.frame containing the observed stages. 
#'   Must include `SimulationName`, `StageName`, and `DateReached`.
#' @param BtwStgPerc Numeric (0-100). The percentage of time between two primary 
#'   stages at which the synthetic stage occurs.
#' @param fill_NAs_with_average Logical. If TRUE, temporarily fills missing primary stage 
#'   dates with the global average date for that stage to allow simulations to run. 
#'   Defaults to FALSE.
#'
#' @return A tibble with `SimulationName` and five APSIM-formatted date columns:
#'   Emerging, SpikeletsDifferentiating, StemElongating, Heading, and Flowering.
#'
#' @importFrom dplyr select filter pull mutate all_of rowwise c_across ungroup
#' @importFrom tidyr pivot_wider
#' @importFrom rlang .data `:=` `!!`
#' @export
#' 
doAPSIMStageInput <- function(df_dateWhenStageWasReached, 
                              BtwStgPerc, 
                              fill_NAs_with_average) {
  
  require(dplyr)
  require(tidyr)
  require(lubridate)
  require(rlang)
  
  frac <- BtwStgPerc / 100
  
  # ------------------------------------------------------------------
  # (i) Attach SimulationName
  # ------------------------------------------------------------------
  df <- df_dateWhenStageWasReached 
  
  # ------------------------------------------------------------------
  # (ii) Spread stages into columns
  # ------------------------------------------------------------------
  df_wide <- df %>%
    dplyr::select(SimulationName, StageName, DateReached) %>%
    tidyr::pivot_wider(
      names_from  = StageName,
      values_from = DateReached
    )
  
  # ------------------------------------------------------------------
  # Column names (explicit and readable)
  # ------------------------------------------------------------------
  col_emerg  <- "[Wheat].Phenology.Emerging.DateToProgress"
  col_stem   <- "[Wheat].Phenology.StemElongating.DateToProgress"
  col_flower <- "[Wheat].Phenology.Flowering.DateToProgress"
  
  col_spikelets <- "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress"
  col_heading   <- "[Wheat].Phenology.Heading.DateToProgress"
  
  # ------------------------------------------------------------------
  # (iii) DEFENSIVE COLUMN CHECKS
  # ------------------------------------------------------------------
  expected_bases <- c(col_emerg, col_stem, col_flower)
  for (col in expected_bases) {
    if (!col %in% names(df_wide)) {
      df_wide[[col]] <- as.Date(NA)
    } else {
      df_wide[[col]] <- as.Date(df_wide[[col]])
    }
  }
  
  # ------------------------------------------------------------------
  # (iii.b) TEMPORARY NA IMPUTATION (If requested)
  # ------------------------------------------------------------------
  if (fill_NAs_with_average) {
    filled_cols <- c()
    
    for (col in expected_bases) {
      if (any(is.na(df_wide[[col]]))) {
        # Calculate the average date, ignoring NAs, and round to nearest whole day
        avg_date <- round(mean(df_wide[[col]], na.rm = TRUE))
        
        # Only fill if the average is a real date (i.e., not ALL values were NA)
        if (!is.na(avg_date)) {
          df_wide[[col]][is.na(df_wide[[col]])] <- avg_date
          filled_cols <- c(filled_cols, col)
        }
      }
    }
    
    # Print the loud warning to the console
    if (length(filled_cols) > 0) {
      warning_box <- c(
        "",
        "======================================================================",
        " ⚠️  CRITICAL WARNING: TEMPORARY AVERAGE DATES INJECTED ⚠️ ",
        "======================================================================",
        " `fill_NAs_with_average` is set to TRUE.",
        " Missing dates for the following stages were replaced with the global average:",
        sprintf(" -> %s", paste(filled_cols, collapse = "\n -> ")),
        "",
        " ACTION REQUIRED:",
        " This is a temporary fix to allow simulations to run.",
        " Please update the raw dataset with actual observed values and set",
        " `fill_NAs_with_average = FALSE` for final analysis.",
        "======================================================================",
        ""
      )
      message(paste(warning_box, collapse = "\n"))
      
      # Also trigger a base R warning so targets logs it in tar_meta()
      warning("TEMPORARY AVERAGE DATES INJECTED. See console output for details.", call. = FALSE)
    }
  }
  
  # ------------------------------------------------------------------
  # (iv) TARGETED MISSING DATA WARNINGS
  # ------------------------------------------------------------------
  # 1. Check Spikelets dependencies
  bad_spikelets <- df_wide %>%
    dplyr::filter(is.na(.data[[col_emerg]]) | is.na(.data[[col_stem]])) %>%
    dplyr::pull(SimulationName)
  
  if (length(bad_spikelets) > 0) {
    warning(sprintf(
      "Cannot calculate 'SpikeletsDifferentiating' for SimulationName(s): %s\n  -> Reason: Missing 'Emerging' or 'StemElongating' dates.",
      paste(bad_spikelets, collapse = ", ")
    ), call. = FALSE)
  }
  
  # 2. Check Heading dependencies
  bad_heading <- df_wide %>%
    dplyr::filter(is.na(.data[[col_stem]]) | is.na(.data[[col_flower]])) %>%
    dplyr::pull(SimulationName)
  
  if (length(bad_heading) > 0) {
    warning(sprintf(
      "Cannot calculate 'Heading' for SimulationName(s): %s\n  -> Reason: Missing 'StemElongating' or 'Flowering' dates.",
      paste(bad_heading, collapse = ", ")
    ), call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # (v) Create synthetic stages (Using Safe Date Math)
  # ------------------------------------------------------------------
  df_wide <- df_wide %>%
    dplyr::mutate(
      !!col_spikelets := .data[[col_emerg]] +
        round(as.numeric(.data[[col_stem]] - .data[[col_emerg]]) * frac),
      
      !!col_heading := .data[[col_stem]] +
        round(as.numeric(.data[[col_flower]] - .data[[col_stem]]) * frac)
    )
  
  # ------------------------------------------------------------------
  # (vi) Enforce correct column order
  # ------------------------------------------------------------------
  df_final <- df_wide %>%
    dplyr::select(
      SimulationName,
      dplyr::all_of(col_emerg),
      dplyr::all_of(col_spikelets),
      dplyr::all_of(col_stem),
      dplyr::all_of(col_heading),
      dplyr::all_of(col_flower)
    )
  
  # ------------------------------------------------------------------
  # (vii) Sanity check: dates must increase chronologically
  # ------------------------------------------------------------------
  date_cols <- names(df_final)[-1]
  
  bad_rows <- df_final %>%
    dplyr::rowwise() %>%
    dplyr::mutate(
      ok = all(diff(as.numeric(dplyr::c_across(dplyr::all_of(date_cols)))) >= 0, na.rm = FALSE)
    ) %>%
    dplyr::ungroup() %>%
    dplyr::filter(!ok | is.na(ok))
  
  if (nrow(bad_rows) > 0) {
    warning(sprintf(
      "Sanity Check Failed: Non-monotonic or missing stage dates for SimulationName(s): %s", 
      paste(bad_rows$SimulationName, collapse = ", ")
    ), call. = FALSE)
  }
  
  return(df_final)
}