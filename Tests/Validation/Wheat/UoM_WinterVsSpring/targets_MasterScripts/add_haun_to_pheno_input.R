#' Update APSIM Stage Inputs with Haun-based Phenology Matrix
#'
#' @description
#' Merges base interpolated crop phenology progress tables with high-priority dates 
#' explicitly derived from observed plant Haun stage values. Automatically handles the 
#' injection of early micro-stages like Double Ridge (\code{LeavesInitiating}) and over-writes 
#' placeholder estimates for Terminal Spikelet (\code{SpikeletsDifferentiating}) development.
#'
#' @details
#' **Dynamic Structural Engineering:** Bracketed naming definitions used natively by APSIM-X 
#' (e.g., \code{[Wheat].Phenology.StageName.DateToProgress}) present significant tidy-eval subsetting issues. 
#' This routine standardizes column structures using safe temporal aliases, resolves prioritized data 
#' using vector coalescing, and runs a comprehensive matrix sequence checker to ensure growth milestones 
#' increment monotonically forward in time.
#'
#' @param obsIntPheno Data frame. Base estimated/interpolated phenology date matrices.
#' @param haunPheno Data frame. Primary physiological data frames computed from raw Haun field records.
#'
#' @return A processed, chronologically verified data frame containing formatted character date strings 
#'   aligned with native APSIM-X parameterization constraints.
#' @export
#'
#' @examples
#' \dontrun{
#' df_final_pheno <- add_haunBased_pheno(
#'   obsIntPheno = df_apsimStageInput, 
#'   haunPheno = df_haun_pheno_dates
#' )
#' }
add_haun_to_pheno_input <- function(obsIntPheno, haunPheno) {
  
  # ---- 0. DEFINE NATIVE APSIM TARGETS ----
  col_emerge <- "[Wheat].Phenology.Emerging.DateToProgress"
  col_leaves <- "[Wheat].Phenology.LeavesInitiating.DateToProgress"
  col_spike  <- "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress"
  col_stem   <- "[Wheat].Phenology.StemElongating.DateToProgress"
  col_head   <- "[Wheat].Phenology.Heading.DateToProgress"
  col_flower <- "[Wheat].Phenology.Flowering.DateToProgress"
  col_grain  <- "[Wheat].Phenology.GrainFilling.DateToProgress"
  
  ordered_cols <- c(
    "SimulationName",
    col_emerge, col_leaves, col_spike, col_stem, col_head, col_flower, col_grain
  )
  
  # ---- 1. TYPE SAFETY PARSING ----
  safe_parse_date <- function(x) {
    if (inherits(x, "Date")) return(x)
    return(as.Date(x, tryFormats = c("%Y-%m-%d", "%d-%m-%Y", "%Y/%m/%d")))
  }
  
  # Prepare base datasets safely
  base_df <- obsIntPheno %>%
    dplyr::select(-dplyr::any_of(col_leaves)) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  # Isolate and explicitly alias matching Haun tracks to guard structural keys
  haun_sub <- haunPheno %>%
    dplyr::select(SimulationName, dplyr::any_of(c(col_leaves, col_spike))) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  # Map columns to completely safe tracking labels
  names(haun_sub)[names(haun_sub) == col_leaves] <- "haun_leaves"
  names(haun_sub)[names(haun_sub) == col_spike]  <- "haun_spike"
  
  # ---- 2. PRIORITY HASH JOIN & OVERWRITE ----
  updated_df <- base_df %>%
    dplyr::left_join(haun_sub, by = "SimulationName")
  
  # Prioritize Haun records over base calculations using an array-safe coalesce
  if (col_spike %in% names(updated_df)) {
    updated_df[[col_spike]] <- dplyr::coalesce(updated_df$haun_spike, updated_df[[col_spike]])
  } else {
    updated_df[[col_spike]] <- updated_df$haun_spike
  }
  
  updated_df[[col_leaves]] <- updated_df$haun_leaves
  
  # Drop alias keys and re-index structure by target column matrix
  updated_df <- updated_df %>%
    dplyr::select(-dplyr::any_of(c("haun_leaves", "haun_spike"))) %>%
    dplyr::select(dplyr::any_of(ordered_cols))
  
  # ---- 3. ROBUST CHRONOLOGICAL VALIDATION ----
  present_stage_cols <- intersect(ordered_cols[-1], names(updated_df))
  bad_sims <- character(0)
  
  if (length(present_stage_cols) >= 2) {
    # Convert dates to a clean numeric matrix (rows = simulations, cols = stages)
    # This completely eliminates tibble unlist object formatting distortions!
    date_matrix <- updated_df %>%
      dplyr::select(dplyr::all_of(present_stage_cols)) %>%
      dplyr::mutate(dplyr::across(everything(), as.numeric)) %>%
      as.matrix()
    
    is_bad <- logical(nrow(updated_df))
    
    for (i in seq_len(nrow(updated_df))) {
      row_vals <- date_matrix[i, ]
      row_vals <- row_vals[!is.na(row_vals)]
      
      if (length(row_vals) >= 2) {
        # Mark simulation bad if any subsequent growth stage occurs backward in calendar time
        if (!all(diff(row_vals) >= 0)) {
          is_bad[i] <- TRUE
        }
      }
    }
    bad_sims <- updated_df$SimulationName[is_bad]
  }
  
  if (length(bad_sims) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      "  CRITICAL PIPELINE ERROR: PHENOLOGY TIMELINE TWISTED  ",
      "======================================================================",
      " Non-chronological date order recorded after merging Haun physiological values.",
      " An earlier developmental milestone occurs after a subsequent milestone in:",
      sprintf("  -> %s", paste(bad_sims, collapse = "\n  -> ")),
      "\n Please review raw leaf counts or check the 'BtwStgPerc' logic coordinates.",
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Chronology checks failed across compiled Haun data.", call. = FALSE)
  }
  
  # ---- 4. UNIFORM APSIM STRING OUTPUT FORMATTING ----
  final_df <- updated_df %>%
    dplyr::mutate(
      dplyr::across(
        dplyr::contains("DateToProgress"),
        ~ dplyr::if_else(is.na(.x), NA_character_, format(.x, "%d-%m-%Y"))
      )
    )
  
  message("Success [add_haunBased_pheno]: Synchronized Haun dates into master schedule tables.")
  return(final_df)
}