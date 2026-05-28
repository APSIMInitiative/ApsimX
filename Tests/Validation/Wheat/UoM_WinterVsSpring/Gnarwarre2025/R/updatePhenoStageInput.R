#' Update APSIM Stage Inputs with Haun-based Phenology
#'
#' @description
#' Updates base interpolated phenology dates with higher-priority dates derived 
#' from Haun stage observations. 
#' 
#' @details
#' **APSIM Firewall:** APSIM crashes if parameter tables contain empty cells. 
#' This function strictly drops any phenological stage column that contains 
#' one or more missing values across the simulation set, outputting a loud 
#' console warning identifying the offending simulations.
#'
#' @param obsIntPheno Data frame containing base interpolated phenology dates.
#' @param haunPheno Data frame containing Haun-derived target dates.
#'
#' @return A rigorously formatted data frame ready for APSIM parameterization.
#'
#' @importFrom dplyr select mutate across contains left_join any_of if_else coalesce
#' @importFrom lubridate parse_date_time
#' @export
updatePhenoStageInput <- function(obsIntPheno, haunPheno) {
  
  require(dplyr)
  require(lubridate)
  
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
  # 1. TYPE SAFETY & PREPARATION (The Lubridate Fix)
  # ------------------------------------------------------------------
  safe_parse_date <- function(x) {
    if (inherits(x, "Date")) return(x)
    
    # Lubridate prevents the "0006" year bug by strictly evaluating formats
    parsed <- suppressWarnings(
      lubridate::parse_date_time(as.character(x), orders = c("dmy", "ymd", "dmy HMS", "ymd HMS"))
    )
    return(as.Date(parsed))
  }
  
  # Prepare base data (cleanly dropping LeavesInitiating if it exists)
  base_df <- obsIntPheno %>%
    dplyr::select(-dplyr::any_of(col_leaves)) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  # Isolate and explicitly rename Haun columns to avoid bracket issues in join
  haun_sub <- haunPheno %>%
    dplyr::select(SimulationName, dplyr::all_of(c(col_leaves, col_spike))) %>%
    dplyr::mutate(dplyr::across(dplyr::contains("DateToProgress"), safe_parse_date))
  
  # Rename to completely safe, standard strings
  names(haun_sub)[names(haun_sub) == col_leaves] <- "haun_leaves"
  names(haun_sub)[names(haun_sub) == col_spike]  <- "haun_spike"
  
  # ------------------------------------------------------------------
  # 2. JOIN AND OVERWRITE
  # ------------------------------------------------------------------
  updated_df <- base_df %>%
    dplyr::left_join(haun_sub, by = "SimulationName")
  
  # Safe Coalesce Overwrite: Give Haun priority, fallback to base
  if (col_spike %in% names(updated_df)) {
    updated_df[[col_spike]] <- dplyr::coalesce(updated_df$haun_spike, updated_df[[col_spike]])
  } else {
    updated_df[[col_spike]] <- updated_df$haun_spike
  }
  
  # Insert the leaves data directly
  updated_df[[col_leaves]] <- updated_df$haun_leaves
  
  # Clean up and force the final strict order
  updated_df <- updated_df %>%
    dplyr::select(-haun_leaves, -haun_spike) %>%
    dplyr::select(dplyr::any_of(ordered_cols))
  
  # ------------------------------------------------------------------
  # 3. CHRONOLOGICAL VALIDATION
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
    warning_box <- c(
      "",
      "======================================================================",
      "  ⚠️  CHRONOLOGY ERROR IN PHENOLOGY DATES DETECTED ⚠️ ",
      "======================================================================",
      " The following SimulationNames have non-sequential dates after merging",
      " the Haun data (a later stage occurs before an earlier stage):",
      paste("   -", bad_sims),
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Non-chronological phenology dates detected.", call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # 4. APSIM EMPTY CELL FIREWALL (COLUMN PURGE)
  # ------------------------------------------------------------------
  cols_to_drop <- character(0)
  
  for (col in present_stage_cols) {
    missing_rows <- is.na(updated_df[[col]])
    
    if (any(missing_rows)) {
      sims_missing_data <- updated_df$SimulationName[missing_rows]
      
      purge_msg <- c(
        "",
        "======================================================================",
        sprintf("  🚨 DROPPING COLUMN: %s 🚨", col),
        "======================================================================",
        " APSIM crashes if parameter columns contain empty cells.",
        " This entire stage column was DELETED because the following",
        " SimulationName(s) were missing data for this stage:",
        paste("   -", sims_missing_data),
        "======================================================================",
        ""
      )
      message(paste(purge_msg, collapse = "\n"))
      
      cols_to_drop <- c(cols_to_drop, col)
    }
  }
  
  # Execute the drop
  if (length(cols_to_drop) > 0) {
    updated_df <- updated_df %>%
      dplyr::select(-dplyr::any_of(cols_to_drop))
  }
  
  # ------------------------------------------------------------------
  # 5. FINAL APSIM FORMATTING
  # ------------------------------------------------------------------
  final_df <- updated_df %>%
    dplyr::mutate(
      dplyr::across(
        dplyr::contains("DateToProgress"),
        # Any surviving columns are fully populated, so we safely format to text
        ~ format(.x, "%d-%m-%Y")
      )
    )
  
  message("Successfully applied Haun-priority updates and purged columns with empty cells.")
  return(final_df)
}