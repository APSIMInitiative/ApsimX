#' Calculate Synthetic APSIM Phenology Progression Dates
#'
#' @description
#' A universal workflow function that transforms observed dates for primary wheat phenology 
#' stages (Emergence, Stem Elongation, Flowering) into a continuous timeline by interpolating 
#' synthetic intermediate stages (Spikelets Differentiating and Heading) based on a sliding percentage.
#'
#' @details
#' **Defensive Structural Engineering:** If an experimental run completely omits a primary stage 
#' from its observations (e.g., fast spring emergence skipping sampling gates), this function 
#' explicitly intercepts the wide data frame and appends the missing column filled with \code{NA}. 
#' This layout normalization blocks downstream variable subsetting crashes.
#' 
#' **Safe Date Arithmetic:** To maintain string integrity and prevent implicit date timezone conversions 
#' to \code{POSIXct} formats during multiplication, date gaps are processed explicitly as numeric day intervals, 
#' rounded to whole integer components, and appended back to base anchor calendar dates.
#'
#' @param df_dateStageTargetReached A data frame containing observed growth stages. 
#'   Must include columns: \code{SimulationName}, \code{StageName}, and \code{DateReached}.
#' @param BtwStgPerc Numeric (0-100). The percentage of temporal distance between adjacent primary 
#'   stages where the intermediate synthetic milestone occurs.
#' @param fill_NAs_with_average Logical. If TRUE, missing base observation parameters are imputed 
#'   using the calculated run-wide arithmetic mean calendar date for that stage. Defaults to FALSE.
#'
#' @return A tibble structured in chronological sequence containing: 
#'   \code{SimulationName}, \code{[Wheat].Phenology.Emerging.DateToProgress}, 
#'   \code{[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress}, 
#'   \code{[Wheat].Phenology.StemElongating.DateToProgress}, 
#'   \code{[Wheat].Phenology.Heading.DateToProgress}, and 
#'   \code{[Wheat].Phenology.Flowering.DateToProgress}.
#' @export
#'
#' @examples
#' \dontrun{
#' df_stages <- doAPSIMStageInput(
#'   df_dateStageTargetReached = df_dateStageTarget, 
#'   BtwStgPerc = 50, 
#'   fill_NAs_with_average = FALSE
#' )
#' }
process_pheno_stages <- function(df_dateStageTargetReached, BtwStgPerc, fill_NAs_with_average = FALSE) {
  
  # ---- 1. SETUP PARAMETERS & STRINGS ----
  frac <- BtwStgPerc / 100
  
  col_emerg     <- "[Wheat].Phenology.Emerging.DateToProgress"
  col_stem      <- "[Wheat].Phenology.StemElongating.DateToProgress"
  col_flower    <- "[Wheat].Phenology.Flowering.DateToProgress"
  col_spikelets <- "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress"
  col_heading   <- "[Wheat].Phenology.Heading.DateToProgress"
  
  expected_bases <- c(col_emerg, col_stem, col_flower)
  
  # ---- 2. LONG TO WIDE PIVOT PIPELINE ----
  df_wide <- df_dateStageTargetReached %>%
    dplyr::select(SimulationName, StageName, DateReached) %>%
    tidyr::pivot_wider(
      names_from  = StageName,
      values_from = DateReached
    )
  
  # ---- 3. DEFENSIVE INTEGRITY CHECK ----
  # Force all prerequisite columns to exist as Date structures, regardless of raw tracking gaps
  for (col in expected_bases) {
    if (!col %in% names(df_wide)) {
      df_wide[[col]] <- as.Date(NA)
    } else {
      df_wide[[col]] <- as.Date(df_wide[[col]])
    }
  }
  
  # ---- 4. CONDITIONAL MEAN IMIPUTATION GATE ----
  if (fill_NAs_with_average) {
    filled_cols <- c()
    
    for (col in expected_bases) {
      if (any(is.na(df_wide[[col]]))) {
        avg_date <- round(mean(df_wide[[col]], na.rm = TRUE))
        
        if (!is.na(avg_date)) {
          df_wide[[col]][is.na(df_wide[[col]])] <- as.Date(avg_date, origin = "1970-01-01")
          filled_cols <- c(filled_cols, col)
        }
      }
    }
    
    if (length(filled_cols) > 0) {
      warning_box <- c(
        "",
        "======================================================================",
        "  CRITICAL WARNING: TEMPORARY AVERAGE DATES INJECTED  ",
        "======================================================================",
        " 'fill_NAs_with_average' is currently active.",
        " Missing values for these variables were replaced with the global mean:",
        sprintf(" -> %s", paste(filled_cols, collapse = "\n -> ")),
        "",
        " Please clean the raw source validation sheet for absolute analysis.",
        "======================================================================",
        ""
      )
      message(paste(warning_box, collapse = "\n"))
      warning("APSIM Stage input contains imputed placeholder records.", call. = FALSE)
    }
  }
  
  # ---- 5. PIPELINE VALIDATION NOTIFICATIONS ----
  bad_spikelets <- df_wide %>%
    dplyr::filter(is.na(.data[[col_emerg]]) | is.na(.data[[col_stem]])) %>%
    dplyr::pull(SimulationName)
  
  if (length(bad_spikelets) > 0) {
    warning(sprintf("Cannot calculate 'SpikeletsDifferentiating' for: %s (Missing Emerging/StemElongating bounds)", 
                    paste(bad_spikelets, collapse = ", ")), call. = FALSE)
  }
  
  bad_heading <- df_wide %>%
    dplyr::filter(is.na(.data[[col_stem]]) | is.na(.data[[col_flower]])) %>%
    dplyr::pull(SimulationName)
  
  if (length(bad_heading) > 0) {
    warning(sprintf("Cannot calculate 'Heading' for: %s (Missing StemElongating/Flowering bounds)", 
                    paste(bad_heading, collapse = ", ")), call. = FALSE)
  }
  
  # ---- 6. INTERPOLATE SYNTHETIC MILESTONES ----
  df_wide <- df_wide %>%
    dplyr::mutate(
      !!col_spikelets := .data[[col_emerg]] + 
        round(as.numeric(difftime(.data[[col_stem]], .data[[col_emerg]], units = "days")) * frac),
      
      !!col_heading   := .data[[col_stem]] + 
        round(as.numeric(difftime(.data[[col_flower]], .data[[col_stem]], units = "days")) * frac)
    )
  
  # ---- 7. CHRONOLOGICAL ENFORCEMENT & LAYOUT CLEANUP ----
  df_final <- df_wide %>%
    dplyr::select(
      SimulationName,
      dplyr::all_of(col_emerg),
      dplyr::all_of(col_spikelets),
      dplyr::all_of(col_stem),
      dplyr::all_of(col_heading),
      dplyr::all_of(col_flower)
    )
  
  # ---- 8. MONOTONIC SEQUENCE SANITY CHECK ----
  date_cols <- names(df_final)[-1]
  
  bad_rows <- df_final %>%
    dplyr::rowwise() %>%
    dplyr::mutate(
      ok = all(diff(as.numeric(dplyr::c_across(dplyr::all_of(date_cols)))) >= 0, na.rm = FALSE)
    ) %>%
    dplyr::ungroup() %>%
    dplyr::filter(!ok | is.na(ok))
  
  if (nrow(bad_rows) > 0) {
    warning(sprintf("APSIM Phenology Sanity Check Failed: Non-monotonic or missing timelines tracked in: %s", 
                    paste(bad_rows$SimulationName, collapse = ", ")), call. = FALSE)
  }
  
  return(df_final)
}