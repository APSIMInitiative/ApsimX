#' Linearly Interpolate Missing Phenology Milestone Stages (Step 2 - Universal Engine)
#'
#' @description
#' A clean, interface-driven Step 2 pipeline component that accepts normalized raw phenology 
#' observations and calculates missing intermediate micro-milestones—specifically Stage 4 
#' (Spikelet Differentiation) and Stage 7 (Heading)—based on a fractional progress parameter.
#'
#' @details
#' **Strict Interface Compliance:** This function reads and returns the exact same standard 
#' intermediate three-column schema: \code{(SimulationName, Clock.Today, Wheat.Phenology.Stage)}. 
#' It completely strips away downstream responsibilities like character string reformatting or 
#' hardcoded APSIM path renames, making it universally plug-and-play.
#'
#' @param df_raw Data frame. The normalized output from Step 1 containing \code{SimulationName}, 
#'   \code{Clock.Today} (Date class), and \code{Wheat.Phenology.Stage} (numeric codes).
#' @param btwStgPerc Numeric (0-1). The fractional progress coefficient to interpolate 
#'   between adjacent developmental stages (e.g., \code{0.50} for an exact chronological midpoint).
#'
#' @return A validated tidy data frame matching the intermediate interface standard containing 
#'   strictly the calculated intermediate stages 4 and 7.
#' @export
create_interp_pheno_dates <- function(df_raw, btwStgPerc) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_raw) || is.null(df_raw) || nrow(df_raw) == 0) {
    stop("Error [create_interp_pheno_dates]: Input data frame asset 'df_raw' is missing or empty.")
  }
  if (missing(btwStgPerc) || is.null(btwStgPerc) || btwStgPerc < 0 || btwStgPerc > 1) {
    stop("Error [create_interp_pheno_dates]: Interpolation parameter 'btwStgPerc' must be a numeric value between 0 and 1.")
  }
  
  # Verify standard interface column presence
  req_cols <- c("SimulationName", "Clock.Today", "Wheat.Phenology.Stage")
  missing_cols <- setdiff(req_cols, names(df_raw))
  if (length(missing_cols) > 0) {
    stop(paste("Error [create_interp_pheno_dates]: Input data does not match the universal interface schema. Missing:", 
               paste(missing_cols, collapse = ", ")))
  }
  
  # ---- 2. PIVOT WIDE FOR SAFE PAIRWISE DATE MATH ----
  # Transform numeric stages into predictable column headers safely
  df_wide <- df_raw %>%
    dplyr::mutate(StageKey = paste0("Stage_", Wheat.Phenology.Stage)) %>%
    dplyr::select(SimulationName, Clock.Today, StageKey) %>%
    tidyr::pivot_wider(
      names_from = StageKey, 
      values_from = Clock.Today,
      values_fn = max # Safeguard against duplicate sampling marks per day
    )
  
  # ---- 3. CALCULATE INTERMEDIATE CALENDAR MILESTONES ----
  # Ensure target columns are generated dynamically even if entirely missing from a trial run
  allocated_stages <- names(df_wide)
  if (!"Stage_3"  %in% allocated_stages) df_wide$Stage_3  <- as.Date(NA)
  if (!"Stage_6"  %in% allocated_stages) df_wide$Stage_6  <- as.Date(NA)
  if (!"Stage_8"  %in% allocated_stages) df_wide$Stage_8  <- as.Date(NA)
  
  df_interp_wide <- df_wide %>%
    dplyr::mutate(
      # Stage 4 occurs at a specific progress interval between Stage 3 (Emergence) and Stage 6 (Stem Elongation)
      Stage_4 = Stage_3 + (as.numeric(Stage_6 - Stage_3) * btwStgPerc),
      
      # Stage 7 occurs at a specific progress interval between Stage 6 (Stem Elongation) and Stage 8 (Flowering)
      Stage_7 = Stage_6 + (as.numeric(Stage_8 - Stage_6) * btwStgPerc)
    )
  
  # ---- 4. MELT VERTICAL AND RE-ALIGN TO STANDARD INTERFACE SCHEMA ----
  df_interp_long <- df_interp_wide %>%
    # Select ONLY the newly generated intermediate columns to avoid duplicating raw data rows
    dplyr::select(SimulationName, Stage_4, Stage_7) %>%
    tidyr::pivot_longer(
      cols = c(Stage_4, Stage_7),
      names_to = "StageName",
      values_to = "Clock.Today"
    ) %>%
    # Drop rows where interpolation was impossible due to missing anchor blocks
    dplyr::filter(!is.na(Clock.Today)) %>%
    dplyr::mutate(
      Wheat.Phenology.Stage = dplyr::case_when(
        StageName == "Stage_4" ~ 4,
        StageName == "Stage_7" ~ 7,
        TRUE                   ~ NA_real_
      )
    ) %>%
    # Format directly back to the strict 3-column interface specification
    dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::distinct()
  
  # ---- 5. PIPELINE NOTIFICATION LOG ----
  message(sprintf("Success [create_interp_pheno_dates]: Linearly generated %d missing micro-milestone rows (Stages 4/7).", 
                  nrow(df_interp_long)))
  
  return(df_interp_long)
}