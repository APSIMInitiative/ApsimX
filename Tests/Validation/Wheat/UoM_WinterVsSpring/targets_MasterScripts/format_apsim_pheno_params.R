#' Transform and Format Phenology Timelines for APSIM-X Injection (Step 5)
#'
#' @description
#' The final phenology pipeline component. It ingests the unified, quality-checked 
#' long-format phenology data, runs a fail-safe chronological sequence check, translates 
#' numeric stages into explicit APSIM-X bracketed parameter strings, pivots the dataset 
#' wide, and strictly formats dates to locale-safe characters ("dd-MMM-yyyy").
#'
#' @param df_pheno_final Data frame. The unified 3-column output from Step 4.
#'
#' @return A wide data frame strictly formatted for APSIM-X parameter input.
#' @export
format_apsim_pheno_params <- function(df_pheno_final) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_pheno_final) || is.null(df_pheno_final) || nrow(df_pheno_final) == 0) {
    stop("Error [format_apsim_pheno_params]: Unified phenology data frame is missing or empty.")
  }
  
  req_cols <- c("SimulationName", "Clock.Today", "Wheat.Phenology.Stage")
  missing_cols <- setdiff(req_cols, names(df_pheno_final))
  if (length(missing_cols) > 0) {
    stop(paste("Error [format_apsim_pheno_params]: Input does not match standard schema. Missing:", 
               paste(missing_cols, collapse = ", ")))
  }
  
  # ---- 2. ULTIMATE FAIL-SAFE CHRONOLOGY CHECK ----
  # Step 4 should guarantee chronology, but this acts as the final gatekeeper before export.
  chrono_check <- df_pheno_final %>%
    dplyr::arrange(SimulationName, Wheat.Phenology.Stage) %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::filter(dplyr::n() > 1) %>%
    dplyr::summarise(
      Is_Bad = any(as.numeric(diff(Clock.Today)) < 0, na.rm = TRUE),
      .groups = "drop"
    ) %>%
    dplyr::filter(Is_Bad == TRUE)
  
  if (nrow(chrono_check) > 0) {
    bad_sims <- chrono_check$SimulationName
    warning_box <- c(
      "",
      "======================================================================",
      "  ⚠️  CRITICAL: FINAL EXPORT CHRONOLOGY ERROR DETECTED ⚠️ ",
      "======================================================================",
      " The following SimulationNames have non-sequential dates in the final",
      " export buffer (a later stage is dated before an earlier stage):",
      paste("   -", bad_sims),
      "======================================================================",
      " Action Required: Review Step 4 (merge_and_qc_pheno) execution logs.",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Export buffer contains chronological inversions. See console.", call. = FALSE)
  }
  
  # ---- 3. APSIM STRING MAPPING ----
  # Translate numeric codes to precise text identifiers
  df_mapped <- df_pheno_final %>%
    dplyr::mutate(
      Stage_Name = dplyr::case_when(
        Wheat.Phenology.Stage == 3  ~ "Emerging",
        Wheat.Phenology.Stage == 4  ~ "LeavesInitiating",
        Wheat.Phenology.Stage == 5  ~ "SpikeletsDifferentiating",
        Wheat.Phenology.Stage == 6  ~ "StemElongating",
        Wheat.Phenology.Stage == 7  ~ "Heading",
        Wheat.Phenology.Stage == 8  ~ "Flowering",
        Wheat.Phenology.Stage == 10 ~ "GrainFilling",
        TRUE                        ~ NA_character_
      )
    ) %>%
    dplyr::filter(!is.na(Stage_Name)) %>%
    dplyr::mutate(
      # Construct the literal APSIM parameter header strings
      Apsim_Param = paste0("[Wheat].Phenology.", Stage_Name, ".DateToProgress")
    )
  
  # ---- 4. PIVOT WIDE & ORDER COLUMNS ----
  # Define the strict physiological ordering for the final spreadsheet
  ordered_cols <- c(
    "SimulationName",
    "[Wheat].Phenology.Emerging.DateToProgress",
    "[Wheat].Phenology.LeavesInitiating.DateToProgress",
    "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",
    "[Wheat].Phenology.StemElongating.DateToProgress",
    "[Wheat].Phenology.Heading.DateToProgress",
    "[Wheat].Phenology.Flowering.DateToProgress",
    "[Wheat].Phenology.GrainFilling.DateToProgress"
  )
  
  df_wide <- df_mapped %>%
    dplyr::select(SimulationName, Apsim_Param, Clock.Today) %>%
    tidyr::pivot_wider(
      names_from = Apsim_Param,
      values_from = Clock.Today
    )
  
  # Ensure all standard columns exist even if no simulation reached them, then order strictly
  missing_ap_cols <- setdiff(ordered_cols, names(df_wide))
  for (col in missing_ap_cols) {
    df_wide[[col]] <- as.Date(NA)
  }
  
  df_wide <- df_wide %>% dplyr::select(dplyr::all_of(ordered_cols))
  
  # ---- 5. LOCALE-SAFE TEXT STRING FORMATTING (The Fix) ----
  # Hardcode English months to completely bypass OS language settings
  eng_months <- c("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
  
  format_apsim_date <- function(dates) {
    dplyr::if_else(
      is.na(dates), 
      NA_character_, 
      sprintf("%02d-%s-%04d", 
              as.numeric(format(dates, "%d")), 
              eng_months[as.numeric(format(dates, "%m"))], 
              as.numeric(format(dates, "%Y")))
    )
  }
  
  df_export <- df_wide %>%
    dplyr::mutate(
      dplyr::across(-SimulationName, format_apsim_date)
    )
  
  # ---- 6. PIPELINE COMPLETION NOTIFICATION ----
  message(sprintf("Success [format_apsim_pheno_params]: Translated %d simulations into wide APSIM parameter format.", 
                  nrow(df_export)))
  
  return(df_export)
}