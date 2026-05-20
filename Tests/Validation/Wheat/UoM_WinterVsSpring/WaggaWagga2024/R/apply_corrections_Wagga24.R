#' Apply Manual Data Corrections to Raw Observations (Wagga Wagga 2024 Context)
#'
#' @description
#' Performs targeted manual corrections and data patches on a compiled, nested observation tibble.
#' Specifically, it repairs a known calendar year-offset issue inside raw NDVI records and back-fills
#' missing collection dates for physical biomass and tissue chemical cuts (e.g., Nconc, WSC) at 
#' Stage 6 (Stem Elongation) and Stage 8 (Flowering) by extracting and cross-referencing your synthetic dates.
#'
#' @details
#' **Dynamic Internal Extraction:** Because the synthetic phenology data frame has been pre-injected 
#' into the nested structure via \code{add_to_observed_list()}, this function reads its own internal 
#' records to generate the date alignment dictionary. This removes parameter pollution from your 
#' \code{_targets.R} workflow file.
#'
#' @param df_tbl A nested tibble structure containing the raw \code{df_name} (character identifiers) 
#'   and \code{data} (list-column containing individual experiment data frames).
#'
#' @return The corrected nested tibble with normalized calendar records and patched observation matrices.
#' @export
#'
#' @examples
#' \dontrun{
#' list_observed_clean <- apply_corrections_Wagga24(list_observed_stage)
#' }
apply_corrections_Wagga24 <- function(df_tbl) {
  
  # ---- 1. EXTRACT DATA-STAGES RECORD GENERATED INTERNALLY ----
  if (!all(c("df_name", "data") %in% names(df_tbl))) {
    stop("Error [apply_corrections_Wagga24]: Input target must be a valid nested tibble with 'df_name' and 'data' blocks.")
  }
  
  # Dynamically pull the index where the stage data tracker was appended
  stage_idx <- grep("stage|pheno", df_tbl$df_name, ignore.case = TRUE)
  if (length(stage_idx) == 0) {
    stop("Error [apply_corrections_Wagga24]: Could not locate pre-appended synthetic stage records within the nested data structure.")
  }
  
  df_stages_Observ <- df_tbl$data[[stage_idx[1]]]
  
  # ---- 2. IDENTIFY TARGET STAGE COLUMNS ----
  if (!"SimulationName" %in% names(df_stages_Observ)) {
    stop("Error [apply_corrections_Wagga24]: Extracted internal stage dataset lacks a 'SimulationName' anchor.")
  }
  
  stage_col <- names(df_stages_Observ)[grepl("Stage", names(df_stages_Observ))]
  if (length(stage_col) != 1) {
    stop("Error [apply_corrections_Wagga24]: Unable to locate unique numeric stage key assignments inside internal records.")
  }
  stage_col <- stage_col[1]
  
  # ---- 3. GENERATE THE DICTIONARY MATRIX (WIDE SYNCHRONIZATION LOOKUP) ----
  date_lookup <- df_stages_Observ %>%
    dplyr::filter(.data[[stage_col]] %in% c(6, 8)) %>%
    dplyr::select(SimulationName, Date, dplyr::all_of(stage_col)) %>%
    dplyr::mutate(PhenoDate = paste0("PhenoDate_", .data[[stage_col]])) %>%
    dplyr::select(-dplyr::all_of(stage_col)) %>%
    tidyr::pivot_wider(names_from = PhenoDate, values_from = Date)
  
  if (!all(c("PhenoDate_6", "PhenoDate_8") %in% names(date_lookup))) {
    stop("Error [apply_corrections_Wagga24]: Alignment map failed to calculate 'PhenoDate_6' or 'PhenoDate_8' constraints.")
  }
  
  # ---- 4. ITERATE METADATA CORRECTIONS PIPELINE ----
  df_tbl_corrected <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          
          # Fix A: Repair NDVI 2025 sensor year rollover offsets
          if (nm == "ndvi_raw") {
            df <- df %>%
              dplyr::mutate(
                Date = as.Date(Date),
                Date = dplyr::if_else(
                  lubridate::year(Date) == 2025,
                  Date %m+% lubridate::years(-1),
                  Date
                )
              )
          }
          
          # Fix B: Patch collection dates for physiological components at Stage 6
          target_dfs_6 <- c(
            "stemYield_6_raw", "spikeYield_6_raw", "senescLeafYield_6_raw", 
            "totalAboveGround_6_raw", "par_6_raw", "greenLeaf_6_raw",
            "leafDead_6_Nconc", "leafDead_6_WSCc",
            "leaf_6_Nconc", "leaf_6_WSCc","leaf_6_Nconc","leaf_6_WSCc",
            "stem_6_Nconc","stem_6_WSCc","spike_6_Nconc","spike_6_WSCc"
          )
          
          if (nm %in% target_dfs_6) {
            if (!"SimulationName" %in% names(df)) {
              stop(sprintf("Error [apply_corrections_Wagga24]: Table %s lacks 'SimulationName' indexing fields.", nm))
            }
            
            df <- df %>%
              dplyr::left_join(date_lookup, by = "SimulationName") %>%
              dplyr::mutate(Date = as.Date(.data$PhenoDate_6)) %>% 
              dplyr::select(-dplyr::any_of(c("PhenoDate_6", "PhenoDate_8")))
          }
          
          # Fix C: Patch collection dates for physiological components at Stage 8
          target_dfs_8 <- c(
            "stemYield_8_raw", "spikeYield_8_raw", "senescLeafYield_8_raw", 
            "totalAboveGround_8_raw", "par_8_raw", "greenLeaf_8_raw",
            "leafDead_8_Nconc", "leafDead_8_WSCc",
            "leaf_8_Nconc", "leaf_8_WSCc","leaf_8_Nconc","leaf_8_WSCc",
            "stem_8_Nconc","stem_8_WSCc","spike_8_Nconc","spike_8_WSCc"
          )
          
          if (nm %in% target_dfs_8) {
            if (!"SimulationName" %in% names(df)) {
              stop(sprintf("Error [apply_corrections_Wagga24]: Table %s lacks 'SimulationName' indexing fields.", nm))
            }
            
            df <- df %>%
              dplyr::left_join(date_lookup, by = "SimulationName") %>%
              dplyr::mutate(Date = as.Date(.data$PhenoDate_8)) %>%
              dplyr::select(-dplyr::any_of(c("PhenoDate_6", "PhenoDate_8")))
          }
          
          return(df)
        }
      )
    )
  
  message("Success [apply_corrections_Wagga24]: Processed NDVI calendar years and back-filled Stage 6/8 cutting timelines.")
  return(df_tbl_corrected)
}