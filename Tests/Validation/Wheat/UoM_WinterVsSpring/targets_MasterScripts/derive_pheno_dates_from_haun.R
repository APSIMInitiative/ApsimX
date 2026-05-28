#' Derive Interpolated Phenology Dates from Haun Stage Observations
#'
#' @description
#' A unified workflow function that extracts main-stem leaf emergence records, calculates 
#' the Final Leaf Number (FLN), and applies a monotonic linear approximation to map 
#' specific physiological milestones—Maximum Leaf Appearance, Terminal Spikelet (FLN - 3), 
#' and Double Ridge (max(2, FLN - 6))—to true calendar dates.
#'
#' @details
#' **Monotonic Vector Safeguard:** Base R's \code{approx()} requires the interpolation predictor 
#' vector (\code{x}) to be strictly increasing. This function explicitly sorts data slices by the raw 
#' \code{HaunStage} column values before running calculations, preventing pipeline crashes or twisted 
#' dates caused by plateaued field measurements.
#'
#' @param df_input Data frame or List. Can be either a flat, wide observation data frame or a compiled nested tibble.
#' @param max_leaf_limit Numeric. Fractional threshold coefficient used to pinpoint the date 
#'   of maximum leaf development (default = 0.95).
#' @param input_type Character. Core ingestion strategy selector. Options are \code{"auto"} (detects structure), 
#'   \code{"list_dfs"} (forces nested parsing), or \code{"df_wide"} (forces flat parsing).
#'
#' @return A data frame containing the primary group key (\code{SimulationName}), 
#'   calculated leaf metrics (\code{LeafNumberMaximum}, \code{FLN}), and the three mapped 
#'   APSIM-X target date columns.
#' @export
#'
#' @examples
#' \dontrun{
#' # In targets pipeline (Automatically detects list structure):
#' tar_target(
#'   name = df_haun_dates,
#'   command = derive_pheno_dates_from_haun(list_observed_dfs, max_leaf_limit = 0.95)
#' )
#' }
derive_pheno_dates_from_haun <- function(df_input, max_leaf_limit = 0.95, input_type = "auto") {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECK ----
  if (missing(df_input) || is.null(df_input)) {
    stop("Error [derive_pheno_dates_from_haun]: Input data container argument is missing or null.")
  }
  
  # ---- 2. POLYMORPHIC LAYOUT INGESTION SWITCH ----
  # Determine layout type based on automatic detection or manual override
  resolved_type <- input_type
  if (resolved_type == "auto") {
    if (all(c("df_name", "data") %in% names(df_input))) {
      resolved_type <- "list_dfs"
    } else {
      resolved_type <- "df_wide"
    }
  }
  
  # Execute extraction based on resolved type
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
  
  # ---- 3. EXPLICIT COLUMN ANCHOR VALIDATION ----
  # Assert strict adherence to SimulationName anchor requirement
  if (!"SimulationName" %in% names(df_working)) {
    stop("Error [derive_pheno_dates_from_haun]: Core tracker column 'SimulationName' is missing from the working dataset.")
  }
  
  # Dynamic Date Column Locator
  date_col <- intersect(c("Clock.Today", "Date"), names(df_working))
  if (length(date_col) == 0) {
    stop("Error [derive_pheno_dates_from_haun]: Could not locate 'Clock.Today' or 'Date' headers in data frame.")
  }
  date_col <- date_col[1]
  
  # Dynamic Haun Stage Column Locator
  haun_col <- names(df_working)[grepl("HaunStage", names(df_working), ignore.case = TRUE)]
  if (length(haun_col) != 1) {
    stop("Error [derive_pheno_dates_from_haun]: Unable to isolate a unique 'HaunStage' column in input headers.")
  }
  haun_col <- haun_col[1]
  
  # ---- 4. DATA CLEANING & RECASTING ----
  df_clean <- df_working %>%
    dplyr::filter(!is.na(.data[[haun_col]]), !is.na(.data[[date_col]])) %>%
    dplyr::mutate(
      # Parse dates safely to numeric tracking intervals (days since 1970-01-01) for math processing
      .temp_date_num = as.numeric(as.Date(
        suppressWarnings(lubridate::parse_date_time(
          as.character(.data[[date_col]]), 
          orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd")
        ))
      )),
      .target_haun = as.numeric(.data[[haun_col]])
    ) %>%
    dplyr::filter(!is.na(.temp_date_num))
  
  # ---- 5. MONOTONIC INTERPOLATION ENGINE ----
  df_derived <- df_clean %>%
    # CRITICAL SAFEGUARD: Sort rows strictly by the Haun Stage predictor values 
    # to maintain the required increasing order constraint for approx()
    dplyr::arrange(SimulationName, .target_haun) %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::summarise(
      LeafNumberMaximum = max(.target_haun, na.rm = TRUE),
      FLN               = as.integer(round(LeafNumberMaximum)),
      LeafNumberLimit   = LeafNumberMaximum * max_leaf_limit,
      
      Haun_TS = FLN - 3,
      Haun_DR = max(2, FLN - 6),
      
      # Vectorized linear calculations safely wrapped in observation volume checks
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
    ) %>%
    
    # ---- 6. RECAST TO NATIVE CALENDAR DATES & MAP HEADERS ----
  dplyr::mutate(
    Date                                                       = as.Date(Date_Num_Limit, origin = "1970-01-01"),
    `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` = as.Date(Date_Num_TS, origin = "1970-01-01"),
    `[Wheat].Phenology.LeavesInitiating.DateToProgress`         = as.Date(Date_Num_DR, origin = "1970-01-01")
  ) %>%
    dplyr::select(
      SimulationName, FLN, LeafNumberMaximum, LeafNumberLimit,
      Date,
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`,
      `[Wheat].Phenology.LeavesInitiating.DateToProgress`
    )
  
  # ---- 7. PIPELINE COMPLETION NOTIFICATION ----
  message(sprintf("Success [derive_pheno_dates_from_haun]: Calculated crop milestone dates across %d Simulations. (Mode: %s)", 
                  nrow(df_derived), resolved_type))
  
  return(df_derived)
}