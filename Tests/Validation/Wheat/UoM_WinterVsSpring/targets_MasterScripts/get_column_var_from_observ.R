#' Extract Specific Variables with Dynamic Metadata Alignment
#'
#' @description
#' A highly secure validation utility designed to isolate a specific target variable or column string 
#' pattern from an aggregated observation data frame while guaranteeing the absolute preservation of your 
#' specified primary validation metadata tracking columns (e.g., \code{SimulationName}, \code{Clock.Today}).
#'
#' @param df Data frame. The consolidated raw or processed multi-column observation dataset.
#' @param std_cols Character vector. The exact structural metadata tracking columns that must be preserved 
#'   (e.g., \code{c("SimulationName", "Clock.Today")}).
#' @param col_name String. The exact column name or regex pattern string of the target variable to isolate.
#'
#' @return A tidy, non-empty data frame subset containing strictly your standard tracking parameters 
#'   plus the isolated variable entries.
#' @export
get_column_var_from_observ <- function(df, std_cols, col_name) {
  
  # df<- x
  #   std_cols <- c("SimulationName", "Clock.Today")
  # col_name <- "Wheat.Phenology.HaunStage" 
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || is.null(df) || nrow(df) == 0) {
    stop("Error [get_column_var_from_observ]: Input data frame asset 'df' is missing or completely empty.")
  }
  if (missing(std_cols) || is.null(std_cols) || length(std_cols) == 0) {
    stop("Error [get_column_var_from_observ]: An explicit 'std_cols' metadata vector must be provided.")
  }
  if (is.null(col_name) || length(col_name) != 1 || col_name == "") {
    stop("Error [get_column_var_from_observ]: Target 'col_name' parameter must be a single, non-empty character string.")
  }
  
  # ---- 2. DYNAMIC COLUMN PATTERN RESOLUTION ----
  # Ensure any literal periods inside incoming paths are treated as literals, not regex wildcards
  escaped_col_name <- gsub("\\.", "\\.", col_name)
  
  # Scan column headers dynamically
  matched_var_cols <- grep(escaped_col_name, names(df), ignore.case = TRUE, value = TRUE)
  
  if (length(matched_var_cols) == 0) {
    stop(sprintf("Error [get_column_var_from_observ]: Column pattern '%s' could not be located inside the data frame headers.", col_name))
  }
  
  # Establish mandatory base columns that MUST exist
  missing_metadata <- setdiff(std_cols, names(df))
  if (length(missing_metadata) > 0) {
    stop(paste("Error [get_column_var_from_observ]: Mandatory tracking metadata columns not found in source dataset:", 
               paste(missing_metadata, collapse = ", ")))
  }
  
  # ---- FIX: Changed ':=' to standard R operator '<-' ----
  target_cols <- unique(c(std_cols, matched_var_cols))
  
  # ---- 3. EXECUTE TARGETED SUBSETTING & FILTER PIPELINE ----
  df_selected <- df %>%
    dplyr::select(dplyr::all_of(target_cols)) %>%
    dplyr::filter(dplyr::if_any(dplyr::all_of(matched_var_cols), ~ !is.na(.x) & .x != "NA" & stringr::str_trim(as.character(.x)) != ""))
  
  # ---- 4. PIPELINE COMPLETION NOTIFICATION ----
  message(sprintf("Success [get_column_var_from_observ]: Isolated target(s) (%s) across %d rows while preserving %d standard metadata keys.", 
                  paste(matched_var_cols, collapse = ", "), nrow(df_selected), length(std_cols)))
  
  return(df_selected)
}