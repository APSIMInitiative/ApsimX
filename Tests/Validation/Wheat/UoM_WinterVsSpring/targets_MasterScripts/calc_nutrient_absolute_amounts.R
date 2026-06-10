#' Calculate Absolute Nutrient Amounts (Strict Engine with Omni-Diagnostics)
#'
#' @description
#' Calculates absolute mass of tissue nutrients. Enforces strict NA propagation.
#' Missing data is classified (FATAL vs INFO) and exported to a diagnostic CSV.
#' Only FATAL data mismatches trigger the massive console alarm.
#'
#' @details
#' **Pure Individual Pools:** Individual organs are strictly kept as NA if no 
#' observation was made, preventing artificial zero-lines in APSIM graphs.
#' **Smart Aggregation:** AboveGround sums safely bypass missing organs 
#' (e.g., early spikes) using na.rm=TRUE, but strictly enforce NA totals 
#' if any required organ triggered a FATAL data mismatch.
#'
#' @export
calc_nutrient_absolute_amounts <- function(df, 
                                           crop_prefix = "Wheat",
                                           organs = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"),
                                           conc_targets = c("N" = "NConc", "WSC" = "WSCc"),
                                           mass_suffix = "Wt",
                                           ag_name = "Wheat.AboveGround",
                                           divisor = 1,
                                           error_log_path = "Inputs/Missing_Nutrient_Data_Log.csv") {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("readr", quietly = TRUE)) stop("Package 'readr' required.")
  
  if (missing(df) || !is.data.frame(df) || nrow(df) == 0) {
    stop("Error [calc_nutrient_absolute_amounts]: Main observation dataframe is missing or empty.")
  }
  
  df_out <- df
  total_calcs <- 0
  diagnostic_logs <- data.frame() 
  
  cat("\n------------------------------------------------------------\n")
  cat(" \U0001F9EA NUTRIENT AGGREGATION & QC CHECK\n")
  cat("------------------------------------------------------------\n")
  
  # ---- 1. DYNAMIC EXPLICIT CROSS-MULTIPLICATION ----
  for (target_nutrient in names(conc_targets)) {
    raw_conc_suffix <- conc_targets[[target_nutrient]]
    cols_to_sum <- c() 
    
    # Vector to track FATAL mismatched data on a per-row basis for this specific nutrient
    fatal_rows_for_nutrient <- rep(FALSE, nrow(df_out))
    
    for (organ in organs) {
      mass_col <- paste(crop_prefix, organ, mass_suffix, sep = ".")     
      conc_col <- paste(crop_prefix, organ, raw_conc_suffix, sep = ".") 
      out_col  <- paste(crop_prefix, organ, target_nutrient, sep = ".") 
      
      if (!mass_col %in% names(df_out)) df_out[[mass_col]] <- NA_real_
      if (!conc_col %in% names(df_out)) df_out[[conc_col]] <- NA_real_
      
      df_out[[mass_col]] <- suppressWarnings(as.numeric(as.character(df_out[[mass_col]])))
      df_out[[conc_col]] <- suppressWarnings(as.numeric(as.character(df_out[[conc_col]])))
      
      if (all(is.na(df_out[[conc_col]]))) {
        message(sprintf("   [!] Notice: '%s' is 100%% missing/empty in this dataset. Safely bypassing.", conc_col))
      }
      
      # 1A: Smart calculation (Strict NA preservation for individual organs!)
      df_out <- df_out %>%
        dplyr::mutate(
          !!out_col := dplyr::case_when(
            # Exception: Explicit 0 mass means 0 physical nutrients
            !is.na(.data[[mass_col]]) & .data[[mass_col]] == 0 ~ 0,
            
            # Standard calculation: Both exist and mass > 0
            !is.na(.data[[mass_col]]) & !is.na(.data[[conc_col]]) ~ (.data[[mass_col]] * .data[[conc_col]]) / divisor,
            
            # EVERYTHING ELSE defaults to NA (Preserves pure empty data, no fake zeros!)
            TRUE ~ NA_real_
          )
        )
      
      # 1B: OMNI-TRACKER DIAGNOSTIC LOGGING
      is_fatal <- !is.na(df_out[[mass_col]]) & df_out[[mass_col]] > 0 & is.na(df_out[[conc_col]])
      fatal_rows_for_nutrient <- fatal_rows_for_nutrient | is_fatal # Update the master failure mask
      
      issue_df <- df_out %>%
        dplyr::select(dplyr::any_of(c("SimulationName", "Clock.Today"))) %>%
        dplyr::mutate(
          Organ = organ,
          Nutrient = target_nutrient,
          Mass_Value = df_out[[mass_col]],
          Conc_Value = df_out[[conc_col]],
          Issue = dplyr::case_when(
            is_fatal ~ "FATAL: Has Mass > 0, but Conc is missing",
            is.na(Mass_Value) & !is.na(Conc_Value) ~ "FATAL: Has Conc, but Mass is missing",
            is.na(Mass_Value) & is.na(Conc_Value) ~ "INFO: Both Mass and Conc are NA (Organ absent/lost)",
            TRUE ~ "OK"
          )
        ) %>%
        dplyr::filter(Issue != "OK")
      
      if (nrow(issue_df) > 0) {
        diagnostic_logs <- dplyr::bind_rows(diagnostic_logs, issue_df)
      }
      
      cols_to_sum <- c(cols_to_sum, out_col)
      total_calcs <- total_calcs + 1
    }
    
    # ---- 2. STRICT AGGREGATE SUMMATION ----
    ag_col_out <- paste(ag_name, target_nutrient, sep = ".")
    
    df_out <- df_out %>%
      dplyr::mutate(
        !!ag_col_out := dplyr::case_when(
          # 2A: If ANY organ triggered a FATAL mismatch on this row, the total MUST fail.
          fatal_rows_for_nutrient ~ NA_real_,
          
          # 2B: If ALL individual organs are perfectly NA, the total must be NA (no false zeros).
          rowSums(!is.na(dplyr::select(., dplyr::all_of(cols_to_sum)))) == 0 ~ NA_real_,
          
          # 2C: Safely sum whatever organs do exist! NAs from biologically absent organs are ignored.
          TRUE ~ rowSums(dplyr::select(., dplyr::all_of(cols_to_sum)), na.rm = TRUE)
        )
      )
  }
  
  # ---- 3. EXPLICIT WARNING LOG OUTPUT ----
  if (nrow(diagnostic_logs) > 0) {
    
    out_dir <- dirname(error_log_path)
    if (!dir.exists(out_dir)) dir.create(out_dir, recursive = TRUE)
    
    readr::write_csv(diagnostic_logs, file = error_log_path)
    
    fatal_errors <- diagnostic_logs %>% dplyr::filter(grepl("FATAL", Issue))
    
    if (nrow(fatal_errors) > 0) {
      big_alert_box <- c(
        "\n",
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        " \U0001F6A8 ACTION REQUIRED: FATAL LAB DATA MISMATCH DETECTED \U0001F6A8",
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        sprintf(" %d instances found where physical organs had Mass but no Conc", nrow(fatal_errors)),
        " (or vice versa), corrupting the aggregate totals.",
        "",
        " A detailed diagnostic log has been exported.",
        " You MUST review the following file to see exactly what to fix:",
        sprintf(" -> %s", error_log_path),
        "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        "\n"
      )
      message(paste(big_alert_box, collapse = "\n"))
    } else {
      message(sprintf("💾 Success: Derived %d pools. (Notice: %d missing organ events safely bypassed and logged to CSV)", total_calcs, nrow(diagnostic_logs)))
    }
    
  } else {
    message(sprintf("💾 Success: Derived %d explicit organ pools and cleanly aggregated to '%s'.", total_calcs, ag_name))
  }
  
  return(df_out)
}