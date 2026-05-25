#' Extract and Interpolate Phenology Dates from Continuous PCD Lists (Step 1 - Wagga)
#'
#' @description
#' A specialized Step 1 component for the Wagga dataset. It collapses a nested list 
#' of continuous PCD scorings, interpolates the exact date the crop reached the target 
#' development threshold, and maps it to the universal schema.
#'
#' @details
#' **Threshold Failure Warning:** If a crop's continuous scoring ends before it actually 
#' reaches the target percentage (e.g., stops at 42% on Nov 10th), the script intercepts 
#' the failure and prints a detailed console warning containing the simulation name, 
#' the stage, the peak percentage reached, and the final observation date.
#'
#' @param list_pcds List of data frames. The output from \code{filter_and_extract_pcds}.
#' @param target_perc Numeric. The fractional progress score (e.g., 0.5) representing stage achievement.
#'
#' @return A validated tidy data frame matching the intermediate interface standard.
#' @export
get_pheno_dates_from_pcd_list <- function(list_pcds, target_perc = 0.5) {
  
  if (missing(list_pcds) || !is.list(list_pcds) || length(list_pcds) == 0) {
    stop("Error [get_pheno_dates_from_pcd_list]: Input must be a populated list of data frames.")
  }
  
  # ---- 1. EXTRACT DATA & FRACTIONAL SCORES SAFELY ----
  df_processed <- purrr::map_dfr(names(list_pcds), function(sheet_name) {
    df <- list_pcds[[sheet_name]]
    
    date_col <- intersect(c("Clock.Today", "Date"), names(df))[1]
    val_col <- setdiff(names(df), c("SimulationName", date_col))[1]
    
    df %>%
      dplyr::select(
        SimulationName, 
        Clock.Today = dplyr::all_of(date_col), 
        ProgressValue = dplyr::all_of(val_col)
      ) %>%
      dplyr::mutate(
        Clock.Today = as.Date(suppressWarnings(lubridate::parse_date_time(
          as.character(Clock.Today), orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd")
        ))),
        ProgressValue = as.numeric(ProgressValue),
        PCD_Source = sheet_name
      ) %>%
      dplyr::filter(!is.na(Clock.Today), !is.na(ProgressValue))
  })
  
  # ---- 2. INTERPOLATE EXACT CALENDAR DATE & TRACK FAILURES ----
  df_interpolated <- df_processed %>%
    dplyr::arrange(SimulationName, PCD_Source, Clock.Today) %>%
    dplyr::group_by(SimulationName, PCD_Source) %>%
    dplyr::summarise(
      MaxProgress = max(ProgressValue, na.rm = TRUE),
      MinProgress = min(ProgressValue, na.rm = TRUE),
      MaxDate     = max(Clock.Today, na.rm = TRUE),
      
      Date_Num = if (dplyr::n() >= 2 && MaxProgress >= target_perc && MinProgress <= target_perc) {
        approx(x = ProgressValue, y = as.numeric(Clock.Today), xout = target_perc, ties = "mean")$y
      } else if (MaxProgress >= target_perc && MinProgress > target_perc) {
        as.numeric(min(Clock.Today, na.rm = TRUE))
      } else if (dplyr::n() == 1 && ProgressValue[1] >= target_perc) {
        as.numeric(Clock.Today[1])
      } else {
        NA_real_
      },
      .groups = "drop"
    ) %>%
    dplyr::mutate(Clock.Today = as.Date(Date_Num, origin = "1970-01-01"))
  
  # ---- 3. DIAGNOSTIC WARNING FOR INCOMPLETE STAGES ----
  df_failures <- df_interpolated %>% dplyr::filter(MaxProgress < target_perc)
  
  if (nrow(df_failures) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      "  ⚠️  WARNING: PHENOLOGY STAGE DID NOT REACH TARGET THRESHOLD  ⚠️",
      "======================================================================",
      sprintf(" Target Threshold Required: %.0f%%", target_perc * 100),
      " The following simulations ended before the stage was fully reached:\n"
    )
    
    for (i in seq_len(nrow(df_failures))) {
      msg <- sprintf("   -> Simulation '%s': Stage '%s' peaked at %.1f%% on %s",
                     df_failures$SimulationName[i],
                     df_failures$PCD_Source[i],
                     df_failures$MaxProgress[i] * 100,
                     format(df_failures$MaxDate[i], "%Y-%m-%d"))
      warning_box <- c(warning_box, msg)
    }
    
    warning_box <- c(warning_box, "======================================================================", "")
    message(paste(warning_box, collapse = "\n"))
    warning("Some phenology stages failed to reach the target threshold. See console.", call. = FALSE)
  }
  
  # ---- 4. ALIGN TO UNIVERSAL 3-COLUMN SCHEMA ----
  df_final <- df_interpolated %>%
    dplyr::filter(!is.na(Clock.Today)) %>% # Drop the failures from the actual timeline
    dplyr::mutate(
      Wheat.Phenology.Stage = dplyr::case_when(
        grepl("Emerg|3", PCD_Source, ignore.case = TRUE) ~ 3,
        grepl("PCDS.*6|Stem|6", PCD_Source, ignore.case = TRUE) ~ 6,
        grepl("Head|7", PCD_Source, ignore.case = TRUE) ~ 7,
        grepl("PCDS.*8|Flower|8", PCD_Source, ignore.case = TRUE) ~ 8,
        grepl("PCDS.*10|Grain|10", PCD_Source, ignore.case = TRUE) ~ 10,
        TRUE ~ NA_real_
      )
    ) %>%
    dplyr::filter(!is.na(Wheat.Phenology.Stage)) %>%
    dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::distinct() %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  message(sprintf("Success [get_pheno_dates_from_pcd_list]: Interpolated %d standardized raw records at %.0f%% progress.", 
                  nrow(df_final), target_perc * 100))
  
  return(df_final)
}