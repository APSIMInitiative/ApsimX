#' Master Merge and Quality Control for Phenology Timelines (Step 4)
#'
#' @description
#' A unified Step 4 pipeline component that ingests the three phenology tracking streams 
#' (Raw, Interpolated, and Haun-derived). It enforces a strict truth hierarchy to resolve 
#' duplicate stage predictions and runs a chronological validation engine to ensure the 
#' crop timeline only moves forward.
#'
#' @details
#' **Truth Hierarchy:** #' 1st Priority: `df_raw` (Direct field observations)
#' 2nd Priority: `df_haun` (Morphological derivations)
#' 3rd Priority: `df_int` (Proportional mathematical interpolations)
#'
#' **Chronological Resolution Engine:** If a timeline inversion is detected (e.g., Stage 5 
#' is dated earlier than Stage 4), the engine compares the hierarchy of the two offending 
#' rows. The row with the lower trusted priority is dropped to restore chronological order.
#'
#' @param df_raw Data frame. Standard 3-column output from Step 1 (Raw Ingestion).
#' @param df_haun Data frame. Standard 3-column output from Step 3 (Haun Derivation).
#' @param df_int Data frame. Standard 3-column output from Step 2 (Linear Interpolation).
#'
#' @return A tidy, quality-checked data frame matching the interface standard, strictly 
#'   ordered by `SimulationName` and `Wheat.Phenology.Stage`.
#' @export
merge_and_qc_pheno <- function(df_raw, df_haun, df_int) {
  
  # ---- 1. SAFE DATA INGESTION & HIERARCHY TAGGING ----
  # Safely attach tracking metadata only if the data frame contains rows
  prep_df <- function(df, src_name, priority_lvl) {
    if (!is.null(df) && nrow(df) > 0) {
      df %>% dplyr::mutate(Source = src_name, Priority = priority_lvl)
    } else {
      NULL
    }
  }
  
  df_1 <- prep_df(df_raw,  "Raw",  1)
  df_2 <- prep_df(df_haun, "Haun", 2)
  df_3 <- prep_df(df_int,  "Int",  3)
  
  # Combine all available streams into a single vertical master log
  df_all <- dplyr::bind_rows(df_1, df_2, df_3)
  
  if (nrow(df_all) == 0) {
    warning("Warning [merge_and_qc_pheno]: All input data frames are empty. Returning empty schema.")
    return(data.frame(SimulationName=character(), Clock.Today=as.Date(character()), Wheat.Phenology.Stage=numeric()))
  }
  
  # ---- 2. WARNING 1: HIERARCHY CONFLICT RESOLUTION ----
  # Detect identical stages for the same simulation that have contrasting dates
  conflicts <- df_all %>%
    dplyr::group_by(SimulationName, Wheat.Phenology.Stage) %>%
    dplyr::filter(dplyr::n() > 1, dplyr::n_distinct(Clock.Today) > 1) %>%
    dplyr::summarise(
      Date_Clash = paste(Clock.Today, collapse = " vs "),
      Source_Clash = paste(Source, collapse = " vs "),
      .groups = "drop"
    )
  
  if (nrow(conflicts) > 0) {
    message("\n[!] NOTICE: Contrasting Dates Detected for Identical Stages")
    message("    Applying Truth Hierarchy (Raw > Haun > Int) to resolve. Affected simulations:")
    print(as.data.frame(conflicts))
  }
  
  # Apply the Truth Hierarchy: Keep only the row with the lowest Priority score (1 is best)
  df_filtered <- df_all %>%
    dplyr::group_by(SimulationName, Wheat.Phenology.Stage) %>%
    dplyr::arrange(Priority, .by_group = TRUE) %>%
    dplyr::slice(1) %>%
    dplyr::ungroup()
  
  # ---- 3. WARNING 2: CHRONOLOGICAL VALIDATION ENGINE ----
  # Custom iterative function to hunt down and fix backward-moving timelines per simulation
  fix_chronology <- function(df_sim) {
    dropped_logs <- c()
    df_sim <- df_sim %>% dplyr::arrange(Wheat.Phenology.Stage)
    
    keep_checking <- TRUE
    while(keep_checking) {
      if (nrow(df_sim) <= 1) break
      
      # Check for negative date differences between sequential stages
      date_diffs <- as.numeric(df_sim$Clock.Today[-1]) - as.numeric(df_sim$Clock.Today[-nrow(df_sim)])
      inversions <- which(date_diffs < 0)
      
      if (length(inversions) == 0) {
        keep_checking <- FALSE # Timeline is perfectly chronological
      } else {
        # Isolate the first conflicting pair
        idx <- inversions[1] 
        row_A <- df_sim[idx, ]     # The earlier stage
        row_B <- df_sim[idx + 1, ] # The later stage
        
        # Compare priorities to decide which one to destroy
        # (Lower priority score = stronger truth)
        if (row_A$Priority <= row_B$Priority) {
          drop_idx <- idx + 1 # Row A is stronger, drop Row B
        } else {
          drop_idx <- idx     # Row B is stronger, drop Row A
        }
        
        # Construct the Big Warning string
        drop_row <- df_sim[drop_idx, ]
        kept_row <- df_sim[if(drop_idx == idx) idx+1 else idx, ]
        
        msg <- sprintf("Simulation '%s': Stage %s (%s, %s) occurred BEFORE Stage %s (%s, %s). Hierarchy logic dropped Stage %s (%s).",
                       df_sim$SimulationName[1],
                       row_B$Wheat.Phenology.Stage, row_B$Source, row_B$Clock.Today,
                       row_A$Wheat.Phenology.Stage, row_A$Source, row_A$Clock.Today,
                       drop_row$Wheat.Phenology.Stage, drop_row$Source)
        
        dropped_logs <- c(dropped_logs, msg)
        
        # Remove the offending row and restart the while loop check
        df_sim <- df_sim[-drop_idx, ]
      }
    }
    
    attr(df_sim, "chrono_logs") <- dropped_logs
    return(df_sim)
  }
  
  # Split dataset by simulation, apply the fix to each, and recombine
  sim_list <- split(df_filtered, df_filtered$SimulationName)
  fixed_list <- lapply(sim_list, fix_chronology)
  
  # Extract the logs for the Big Warning
  all_chrono_logs <- unlist(lapply(fixed_list, function(x) attr(x, "chrono_logs")))
  
  if (length(all_chrono_logs) > 0) {
    message("\n=========================================================================================")
    message(" [!!!] CRITICAL WARNING: CHRONOLOGICAL TIMELINE INVERSIONS DETECTED & CORRECTED")
    message("=========================================================================================")
    for (log_msg in all_chrono_logs) {
      message(" -> ", log_msg)
    }
    message("=========================================================================================\n")
  }
  
  # ---- 4. CLEANUP AND FINAL INTERFACE ALIGNMENT ----
  df_final <- dplyr::bind_rows(fixed_list) %>%
    dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::arrange(SimulationName, Wheat.Phenology.Stage)
  
  message(sprintf("Success [merge_and_qc_pheno]: Master timeline assembled. Retained %d strictly chronological stage events.", 
                  nrow(df_final)))
  
  return(df_final)
}