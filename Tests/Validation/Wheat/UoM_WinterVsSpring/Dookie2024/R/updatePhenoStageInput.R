#' Update APSIM Stage Inputs with Haun-based Phenology
#'
#' @description
#' Updates base interpolated phenology dates with higher-priority dates derived 
#' from Haun stage observations. Forces a skeleton join to ensure all simulations 
#' survive to the final APSIM parameter file.
#'
#' @details
#' Written to be strictly `targets`-safe. Gracefully handles missing Haun data 
#' and automatically bridges 'Cultivar' to 'SimulationName' if needed. Includes 
#' a severe warning for any dropped simulations.
#'
#' @param obsIntPheno Data frame containing base interpolated phenology dates.
#' @param haunPheno Data frame containing Haun-derived target dates.
#' @param df_master_sims Data frame containing the complete list of simulations.
#'
#' @return A rigorously formatted data frame ready for APSIM parameterization.
#'
#' @export
updatePhenoStageInput <- function(obsIntPheno, haunPheno, df_master_sims) {
  
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
  # 1. PREPARE BASE DATA
  # ------------------------------------------------------------------
  safe_parse_date <- function(x) {
    if (inherits(x, "Date")) return(x)
    as.Date(x, tryFormats = c("%Y-%m-%d", "%d-%m-%Y"))
  }
  
  updated_df <- obsIntPheno
  if (col_leaves %in% names(updated_df)) {
    updated_df[[col_leaves]] <- NULL
  }
  
  base_date_cols <- grep("DateToProgress", names(updated_df), value = TRUE)
  for (col in base_date_cols) {
    updated_df[[col]] <- safe_parse_date(updated_df[[col]])
  }
  
  # ------------------------------------------------------------------
  # 2. HAUN OVERWRITE (SAFE GUARDED & BRIDGED)
  # ------------------------------------------------------------------
  if (!is.null(haunPheno) && !("SimulationName" %in% names(haunPheno)) && "Cultivar" %in% names(haunPheno)) {
    if (is.data.frame(df_master_sims) && "Cultivar" %in% names(df_master_sims) && "SimulationName" %in% names(df_master_sims)) {
      sim_map <- unique(df_master_sims[, c("Cultivar", "SimulationName")])
      haunPheno <- merge(haunPheno, sim_map, by = "Cultivar", all.x = TRUE)
    }
  }
  
  if (!is.null(haunPheno) && "SimulationName" %in% names(haunPheno) && nrow(haunPheno) > 0) {
    haun_cols_to_keep <- intersect(names(haunPheno), c("SimulationName", col_leaves, col_spike))
    haun_sub <- haunPheno[, haun_cols_to_keep, drop = FALSE]
    
    haun_date_cols <- grep("DateToProgress", names(haun_sub), value = TRUE)
    for (col in haun_date_cols) {
      haun_sub[[col]] <- safe_parse_date(haun_sub[[col]])
    }
    
    if (col_leaves %in% names(haun_sub)) names(haun_sub)[names(haun_sub) == col_leaves] <- "haun_leaves"
    if (col_spike %in% names(haun_sub)) names(haun_sub)[names(haun_sub) == col_spike]  <- "haun_spike"
    
    updated_df <- merge(updated_df, haun_sub, by = "SimulationName", all.x = TRUE)
    
    if (col_spike %in% names(updated_df) && "haun_spike" %in% names(updated_df)) {
      idx <- !is.na(updated_df$haun_spike)
      updated_df[[col_spike]][idx] <- updated_df$haun_spike[idx]
    } else if ("haun_spike" %in% names(updated_df)) {
      updated_df[[col_spike]] <- updated_df$haun_spike
    }
    
    if ("haun_leaves" %in% names(updated_df)) {
      updated_df[[col_leaves]] <- updated_df$haun_leaves
    }
    
    updated_df$haun_leaves <- NULL
    updated_df$haun_spike <- NULL
  }
  
  # ------------------------------------------------------------------
  # 3. THE SKELETON JOIN (APSIM CRASH FIX) + MONSTROUS WARNING
  # ------------------------------------------------------------------
  if (is.data.frame(df_master_sims) && "SimulationName" %in% names(df_master_sims)) {
    skeleton <- data.frame(SimulationName = unique(df_master_sims$SimulationName), stringsAsFactors = FALSE)
  } else {
    skeleton <- data.frame(SimulationName = unique(as.character(df_master_sims)), stringsAsFactors = FALSE)
  }
  
  # 🔥 THE MONSTROUS WARNING 🔥
  missing_sims <- setdiff(skeleton$SimulationName, updated_df$SimulationName)
  
  if (length(missing_sims) > 0) {
    warning(
      "\n====================================================================\n",
      "🚨 MONSTROUS WARNING: MISSING SIMULATIONS DETECTED! 🚨\n",
      "====================================================================\n",
      "The following ", length(missing_sims), " SimulationName(s) dropped out of the pipeline\n",
      "and are being artificially rescued by the Skeleton Join (filled with NAs):\n\n",
      paste(paste0("   -> ", missing_sims), collapse = "\n"),
      "\n\n====================================================================\n"
    )
  }
  
  # Execute the skeleton join to force them back in
  updated_df <- merge(skeleton, updated_df, by = "SimulationName", all.x = TRUE)
  
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
  date_cols_to_format <- grep("DateToProgress", names(updated_df), value = TRUE)
  
  for (col in date_cols_to_format) {
    fmt <- format(updated_df[[col]], "%d-%m-%Y")
    fmt[is.na(updated_df[[col]])] <- NA_character_
    updated_df[[col]] <- fmt
  }
  
  reordered_cols <- intersect(ordered_cols, names(updated_df))
  final_df <- updated_df[, reordered_cols, drop = FALSE]
  
  return(final_df)
}