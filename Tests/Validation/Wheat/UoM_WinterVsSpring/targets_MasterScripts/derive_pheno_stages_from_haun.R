#' Derive Interpolated Phenology Dates from Haun Stage Observations (Step 3 - Universal Engine)
#'
#' @description
#' A unified Step 3 pipeline component that extracts main-stem leaf emergence records, 
#' calculates the Final Leaf Number (FLN), and applies a monotonic linear approximation to map 
#' specific physiological milestones—Terminal Spikelet (FLN - 3) and Double Ridge (max(2, FLN - 6))—
#' back into true calendar dates.
#'
#' @details
#' **Console Diagnostic Trace:** This function computes wide internal metrics (FLN, Max Leaf limits) 
#' and prints them directly to the console for math-tracing visibility, before melting only the 
#' valid APSIM stages (4 and 5) into the strict 3-column pipeline interface.
#'
#' @param df_input Data frame or List. Can be either a flat, wide observation data frame (Grass) 
#'   or a compiled nested tibble list (Wagga).
#' @param max_leaf_limit Numeric. Fractional threshold coefficient used to pinpoint the date 
#'   of maximum leaf development (default = 0.95).
#' @param input_type Character. Core ingestion strategy selector. Options are \code{"auto"} (detects structure), 
#'   \code{"list_dfs"} (forces nested parsing), or \code{"df_wide"} (forces flat parsing).
#'
#' @return A validated tidy data frame matching the intermediate interface standard containing 
#'   strictly the calculated morphologically derived stages 4 and 5.
#' @export
derive_pheno_stages_from_haun <- function(df_input, max_leaf_limit = 0.95, input_type = "auto") {
  
  # ---- 1. POLYMORPHIC LAYOUT INGESTION SWITCH ----
  if (missing(df_input) || is.null(df_input)) {
    stop("Error [derive_pheno_dates_from_haun]: Input data container argument is missing or null.")
  }
  
  resolved_type <- input_type
  if (resolved_type == "auto") {
    if (all(c("df_name", "data") %in% names(df_input))) {
      resolved_type <- "list_dfs"
    } else {
      resolved_type <- "df_wide"
    }
  }
  
  if (resolved_type == "list_dfs") {
    haun_idx <- grep("haun", df_input$df_name, ignore.case = TRUE)
    if (length(haun_idx) == 0) {
      stop("Error [derive_pheno_dates_from_haun]: No operational dataset with 'haun' found inside nested list structures.")
    }
    df_working <- df_input$data[[haun_idx[1]]]
  } else if (resolved_type == "df_wide") {
    df_working <- df_input
  } else {
    stop(sprintf("Error [derive_pheno_dates_from_haun]: Invalid input_type selection '%s'. Use 'auto', 'list_dfs', or 'df_wide'.", input_type))
  }
  
  # ---- 2. DYNAMIC FIELD ANCHOR VALIDATION ----
  if (!"SimulationName" %in% names(df_working)) {
    stop("Error [derive_pheno_dates_from_haun]: Core tracker column 'SimulationName' is missing from the working dataset.")
  }
  
  date_col <- intersect(c("Clock.Today", "Date"), names(df_working))
  if (length(date_col) == 0) {
    stop("Error [derive_pheno_dates_from_haun]: Could not locate 'Clock.Today' or 'Date' headers in data frame.")
  }
  date_col <- date_col[1]
  
  haun_col <- names(df_working)[grepl("HaunStage", names(df_working), ignore.case = TRUE)]
  if (length(haun_col) != 1) {
    stop("Error [derive_pheno_dates_from_haun]: Unable to isolate a unique 'HaunStage' column in input headers.")
  }
  haun_col <- haun_col[1]
  
  # ---- 3. DATA CLEANING & RECASTING ----
  df_clean <- df_working %>%
    dplyr::filter(!is.na(.data[[haun_col]]), !is.na(.data[[date_col]])) %>%
    dplyr::mutate(
      .temp_date_num = as.numeric(as.Date(
        suppressWarnings(lubridate::parse_date_time(
          as.character(.data[[date_col]]), 
          orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd")
        ))
      )),
      .target_haun = as.numeric(.data[[haun_col]])
    ) %>%
    dplyr::filter(!is.na(.temp_date_num))
  
  # ---- 4. MONOTONIC INTERPOLATION ENGINE ----
  df_wide_metrics <- df_clean %>%
    dplyr::arrange(SimulationName, .target_haun) %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::summarise(
      LeafNumberMaximum = max(.target_haun, na.rm = TRUE),
      FLN               = as.integer(round(LeafNumberMaximum)),
      LeafNumberLimit   = LeafNumberMaximum * max_leaf_limit,
      
      Haun_TS = FLN - 3,
      Haun_DR = max(2, FLN - 6),
      
      Date_Num_Limit = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = .target_haun, y = .temp_date_num, xout = LeafNumberLimit, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      Date_Num_TS = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = .target_haun, y = .temp_date_num, xout = Haun_TS, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      Date_Num_DR = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = .target_haun, y = .temp_date_num, xout = Haun_DR, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      .groups = "drop"
    )
  
  # ---- 5. INTERNAL LOGIC CONSOLE TRACE ----
  df_diagnostic <- df_wide_metrics %>%
    dplyr::mutate(
      Date_MaxLeafLimit = as.Date(Date_Num_Limit, origin = "1970-01-01"),
      Date_Stage4_DR    = as.Date(Date_Num_DR, origin = "1970-01-01"),
      Date_Stage5_TS    = as.Date(Date_Num_TS, origin = "1970-01-01")
    ) %>%
    dplyr::select(SimulationName, LeafNumberMaximum, FLN, LeafNumberLimit, 
                  Date_MaxLeafLimit, Date_Stage4_DR, Date_Stage5_TS)
  
  message("\n===========================================================")
  message("           HAUN DERIVATION INTERNAL TRACE LOGIC            ")
  message("===========================================================")
  print(as.data.frame(df_diagnostic))
  message("===========================================================\n")
  
  # Count metrics for the final summary warning
  sims_with_dr <- sum(!is.na(df_wide_metrics$Date_Num_DR))
  sims_with_ts <- sum(!is.na(df_wide_metrics$Date_Num_TS))
  
  # ---- 6. MELT VERTICAL AND RE-ALIGN TO STANDARD INTERFACE SCHEMA ----
  df_final <- df_wide_metrics %>%
    dplyr::mutate(
      Stage_4 = as.Date(Date_Num_DR, origin = "1970-01-01"), # Double Ridge mapping
      Stage_5 = as.Date(Date_Num_TS, origin = "1970-01-01")  # Terminal Spikelet mapping
    ) %>%
    # ONLY select valid APSIM stages to avoid inventing synthetic stages like 3.95
    dplyr::select(SimulationName, Stage_4, Stage_5) %>%
    tidyr::pivot_longer(
      cols = c(Stage_4, Stage_5),
      names_to = "StageKey",
      values_to = "Clock.Today"
    ) %>%
    dplyr::filter(!is.na(Clock.Today)) %>%
    dplyr::mutate(
      Wheat.Phenology.Stage = dplyr::case_when(
        StageKey == "Stage_4" ~ 4, # LeavesInitiating
        StageKey == "Stage_5" ~ 5, # SpikeletsDifferentiating
        TRUE                  ~ NA_real_
      )
    ) %>%
    dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::distinct() %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  # ---- 7. PIPELINE NOTIFICATION & SUMMARY WARNING ----
  message(sprintf("Success [derive_pheno_stages_from_haun]: Morphologically generated milestone rows from Haun counts. (Mode: %s)", 
                  resolved_type))
  
  message(sprintf(" -> `[Wheat].Phenology.LeavesInitiating.DateToProgress` as Stage 4 generated for %d simulations.", 
                  sims_with_dr))
  
  message(sprintf(" -> `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` as Stage 5 generated for %d simulations.", 
                  sims_with_ts))
  
  return(df_final)
}