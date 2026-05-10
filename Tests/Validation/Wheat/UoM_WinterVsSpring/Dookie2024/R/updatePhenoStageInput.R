#' Update APSIM Stage Inputs with Haun-based Phenology
#'
#' @description
#' Updates base interpolated phenology dates with higher-priority dates derived 
#' from Haun stage observations. Forces a skeleton join to ensure all simulations 
#' survive to the final APSIM parameter file.
#'
#' @param obsIntPheno Data frame containing base interpolated phenology dates.
#' @param haunPheno Data frame containing Haun-derived target dates.
#' @param df_master_sims Data frame containing the complete list of simulations.
#'
#' @return A rigorously formatted data frame ready for APSIM parameterization.
#'
#' @importFrom dplyr select mutate across contains left_join any_of if_else coalesce distinct
#' @export
updatePhenoStageInput <- function(obsIntPheno, haunPheno, df_master_sims) {
  
  require(dplyr)
  
  # ------------------------------------------------------------------
  # 0. DEFINE APSIM TARGET COLUMNS
  # ------------------------------------------------------------------
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
  
  # ------------------------------------------------------------------
  # 1. TYPE SAFETY & PREPARATION
  # ------------------------------------------------------------------
  safe_parse_date <- function(x) {
    if (inherits(x, "Date")) return(x)
    as.Date(x, tryFormats = c("%Y-%m-%d", "%d-%m-%Y"))
  }
  
  # Prepare base data 
  base_df <- obsIntPheno %>%
    dplyr::select(-dplyr::any_of(col_leaves)) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  # Isolate and explicitly rename Haun columns
  haun_sub <- haunPheno %>%
    dplyr::select(SimulationName, dplyr::any_of(c(col_leaves, col_spike))) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  names(haun_sub)[names(haun_sub) == col_leaves] <- "haun_leaves"
  names(haun_sub)[names(haun_sub) == col_spike]  <- "haun_spike"
  
  # ------------------------------------------------------------------
  # 2. JOIN AND OVERWRITE
  # ------------------------------------------------------------------
  updated_df <- base_df %>%
    dplyr::left_join(haun_sub, by = "SimulationName")
  
  if (col_spike %in% names(updated_df) && "haun_spike" %in% names(updated_df)) {
    updated_df[[col_spike]] <- dplyr::coalesce(updated_df$haun_spike, updated_df[[col_spike]])
  } else if ("haun_spike" %in% names(updated_df)) {
    updated_df[[col_spike]] <- updated_df$haun_spike
  }
  
  if ("haun_leaves" %in% names(updated_df)) {
    updated_df[[col_leaves]] <- updated_df$haun_leaves
  }
  
  updated_df <- updated_df %>%
    dplyr::select(-dplyr::any_of(c("haun_leaves", "haun_spike"))) %>%
    dplyr::select(dplyr::any_of(ordered_cols))
  
  # ------------------------------------------------------------------
  # 3. THE SKELETON JOIN (APSIM CRASH FIX)
  # ------------------------------------------------------------------
  # Force every simulation from the master list into the final dataframe.
  # If a simulation had no raw data, this creates a row filled with NAs.
  skeleton <- df_master_sims %>% 
    dplyr::select(SimulationName) %>% 
    dplyr::distinct()
  
  updated_df <- skeleton %>%
    dplyr::left_join(updated_df, by = "SimulationName")
  
  # ------------------------------------------------------------------
  # 4. CHRONOLOGICAL VALIDATION 
  # ------------------------------------------------------------------
  present_stage_cols <- intersect(ordered_cols[-1], names(updated_df))
  bad_sims <- character(0)
  
  if (length(present_stage_cols) >= 2) {
    is_bad <- logical(nrow(updated_df))
    
    for (i in seq_len(nrow(updated_df))) {
      row_dates <- unlist(updated_df[i, present_stage_cols])
      row_dates <- row_dates[!is.na(row_dates)]
      
      if (length(row_dates) < 2) {
        is_bad[i] <- FALSE
      } else {
        is_bad[i] <- !all(as.numeric(diff(row_dates)) >= 0)
      }
    }
    
    bad_sims <- updated_df$SimulationName[is_bad]
  }
  
  if (length(bad_sims) > 0) {
    warning("Non-chronological phenology dates detected in: ", paste(bad_sims, collapse = ", "))
  }
  
  # ------------------------------------------------------------------
  # 5. FINAL APSIM FORMATTING
  # ------------------------------------------------------------------
  final_df <- updated_df %>%
    dplyr::mutate(
      dplyr::across(
        dplyr::contains("DateToProgress"),
        ~ dplyr::if_else(is.na(.x), NA_character_, format(.x, "%d-%m-%Y"))
      )
    )
  
  message("Successfully applied Haun updates and enforced master skeleton join.")
  return(final_df)
}