#' Calculate Synthetic APSIM Phenology Progression Dates
#'
#' @description
#' This function takes observed dates for major wheat phenology stages (Emergence, 
#' Stem Elongation, Flowering) and interpolates synthetic intermediate stages 
#' (Spikelets Differentiating and Heading) based on a specified percentage factor.
#' It formats the output with APSIM-compliant column names ready for simulation injection.
#'
#' @details
#' **Safe Date Math Implementation:** #' Base R date subtraction yields `difftime` objects. Multiplying these by fractions 
#' can silently convert the resulting addition back into a `POSIXct` Date-Time object 
#' (e.g., "YYYY-MM-DD 12:00:00"). Because APSIM strictly requires Date strings, this 
#' function explicitly converts differences to numeric days and rounds them to the 
#' nearest whole integer before adding them back to the base `Date` object.
#'
#' @param df_dateStageTargetReached A data.frame containing the observed stages. 
#'   Must include `Cultivar`, `StageName`, and `DateReached`.
#' @param df_simNameByCult A data.frame mapping `Cultivar` to `SimulationName`.
#' @param BtwStgPerc Numeric (0-100). The percentage of time between two primary 
#'   stages at which the synthetic stage occurs.
#'
#' @return A tibble with `SimulationName` and five APSIM-formatted date columns:
#'   Emerging, SpikeletsDifferentiating, StemElongating, Heading, and Flowering.
#'   Throws a warning if the calculated dates for any simulation are non-monotonic.
#'
#' @importFrom dplyr left_join select mutate all_of rowwise c_across ungroup filter
#' @importFrom tidyr pivot_wider
#' @importFrom rlang .data `:=`
#' @export
doAPSIMStageInput <- function(df_dateStageTargetReached,
                              df_simNameByCult,
                              BtwStgPerc) {
  
  frac <- BtwStgPerc / 100
  
  # ------------------------------------------------------------------
  # (i) Attach SimulationName
  # ------------------------------------------------------------------
  df <- df_dateStageTargetReached %>%
    dplyr::left_join(df_simNameByCult, by = "Cultivar")
  
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
  # (iii) Create synthetic stages (WITH SAFE DATE MATH)
  # ------------------------------------------------------------------
  df_wide <- df_wide %>%
    dplyr::mutate(
      # Ensure base columns are Date objects (prevents character math errors)
      !!col_emerg  := as.Date(.data[[col_emerg]]),
      !!col_stem   := as.Date(.data[[col_stem]]),
      !!col_flower := as.Date(.data[[col_flower]]),
      
      # Use round(as.numeric(...)) to prevent decimal days / POSIXct conversion
      !!col_spikelets := .data[[col_emerg]] + 
        round(as.numeric(.data[[col_stem]] - .data[[col_emerg]]) * frac),
      
      !!col_heading := .data[[col_stem]] + 
        round(as.numeric(.data[[col_flower]] - .data[[col_stem]]) * frac)
    )
  
  # ------------------------------------------------------------------
  # (iv) Enforce correct column order
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
  # (v) Sanity check: dates must increase chronologically
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
      "Sanity Check Failed: %d SimulationNames have non-monotonic or missing stage dates.", 
      nrow(bad_rows)
    ))
  }
  
  return(df_final)
}