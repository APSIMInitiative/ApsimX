#' Universal Variable Name Remapper for Observation Pipelines
#'
#' @description
#' A highly resilient validation function that standardizes variable headers inside 
#' finalized observation data frames. It accepts an external translation mapping dictionary CSV 
#' or falls back to an internal hardcoded naming vector to execution-safe column swaps.
#'
#' @details
#' **Pipeline Safety Engineering:** Instead of executing hard assignments that break if a 
#' column is missing from a specific trial iteration, this routine maps variables dynamically using 
#' Tidyverse selectors. It updates present match groups and leaves unmapped background variables completely untouched.
#'
#' @param df_obs Data frame. The long or wide finalized observation dataset requiring name remapping.
#' @param mapping_csv_path String. Optional path to a layout mapping configuration CSV containing 
#'   \code{RawDataName} and \code{ObservedFileName} headers. Defaults to \code{NULL}.
#'
#' @return A processed data frame with uniform, APSIM-compliant variable column names.
#' @export
#'
#' @examples
#' \dontrun{
#' # Method A: Dynamic CSV Execution (Recommended for targets)
#' df_clean_names <- apply_name_corrections_Grass25(
#'   df_obs = df_final_observed, 
#'   mapping_csv_path = "Grass25/Grass25_NameMapping.csv"
#' )
#' 
#' # Method B: Hardcoded internal fallback execution
#' df_clean_names <- apply_name_corrections_Grass25(df_final_observed)
#' }
apply_name_corrections_Grass25 <- function(df_obs, mapping_csv_path = NULL) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_obs) || is.null(df_obs) || nrow(df_obs) == 0) {
    stop("Error [apply_name_corrections_Grass25]: Incoming data frame asset 'df_obs' is missing or empty.")
  }
  
  # ---- 2. ESTABLISH THE TRANSLATION MATRIX MAP ----
  # Initialize an empty named character vector for mapping
  lookup_map <- c()
  
  if (!is.null(mapping_csv_path) && file.exists(mapping_csv_path)) {
    # Method A: Load dynamic configuration file
    tryCatch({
      df_map_cfg <- read.csv(mapping_csv_path, stringsAsFactors = FALSE, header = TRUE)
      names(df_map_cfg) <- stringr::str_trim(names(df_map_cfg))
      
      if (all(c("RawDataName", "ObservedFileName") %in% names(df_map_cfg))) {
        # Build standard named vector: c("NewName" = "OldName")
        lookup_map <- structure(
          stringr::str_trim(df_map_cfg$RawDataName), 
          names = stringr::str_trim(df_map_cfg$ObservedFileName)
        )
        message("Success [apply_name_corrections_Grass25]: Loaded translation matrix from CSV.")
      } else {
        warning("Warning [apply_name_corrections_Grass25]: CSV missing required columns. Using hardcoded fallback.", call. = FALSE)
      }
    }, error = function(e) {
      warning(paste("Warning [apply_name_corrections_Grass25]: Failed to parse CSV. Reason:", e$message, "- Using hardcoded fallback."), call. = FALSE)
    })
  }
  
  # Method B Fallback: If no CSV path was provided or loading failed, load the absolute defaults
  if (length(lookup_map) == 0) {
    # Key-Value Mapping Pair layout: "ObservedFileName" = "RawDataName"
    lookup_map <- c(
      "Wheat.Leaf.Dead.NConc"  = "Wheat.Leaf.Dead.NConc",
      "Wheat.Leaf.Dead.WSCc"   = "Wheat.Leaf.Dead.WSC",
      "Wheat.Leaf.Live.NConc"  = "Wheat.Leaf.Live.NConc",
      "Wheat.Leaf.Live.WSCc"   = "Wheat.Leaf.Live.WSC",
      "Wheat.Spike.Live.NConc" = "Wheat.Spike.NConc",
      "Wheat.Spike.Live.WSCc"  = "Wheat.Spike.WSC",
      "Wheat.Stem.Live.Wt"     = "Wheat.Stem.Wt",
      "Wheat.Stem.Live.NConc"  = "Wheat.Stem.NConc",
      "Wheat.Stem.Live.WSCc"   = "Wheat.Stem.WSC"
    )
    message("Notice [apply_name_corrections_Grass25]: Utilizing built-in hardcoded dictionary mappings.")
  }
  
  # ---- 3. EXECUTE SAFE DYNAMIC ROW/COLUMN REMAPPING ----
  # Intercept and isolate only the translation elements that are actively present inside this data frame slice
  active_renames <- lookup_map[lookup_map %in% names(df_obs)]
  
  if (length(active_renames) > 0) {
    # In dplyr::rename(), the syntax is: rename(new_name = old_name)
    # Passing our named vector using !!! (triple-bang) evaluates the whole mapping array concurrently
    df_remapped <- df_obs %>%
      dplyr::rename(!!!active_renames)
    
    # ---- 4. PIPELINE COMPLETION AUDIT LOGGING ----
    # Extract the old names (values) and new names (names) from the active map
    rename_list <- paste("   [OLD]", unname(active_renames), "--> [NEW]", names(active_renames))
    
    log_box <- c(
      "",
      "----------------------------------------------------------------------",
      " 🔄 PIPELINE ACTION: VARIABLE NAMES REMAPPED 🔄",
      "----------------------------------------------------------------------",
      sprintf(" Successfully standardized %d columns:", length(active_renames)),
      "",
      rename_list,
      "----------------------------------------------------------------------",
      ""
    )
    
    message(paste(log_box, collapse = "\n"))
    warning(sprintf("Remapped %d variable names to APSIM standards. See console for mapping details.", length(active_renames)), call. = FALSE)
  } else {
    message("Warning [apply_name_corrections_Grass25]: No matching raw headers located inside input table. Columns left as-is.")
    df_remapped <- df_obs
  }
  
  # ---- 5. SPECIFIC DATA SCALING CORRECTIONS ----
  if ("Wheat.Leaf.Height" %in% names(df_remapped)) {
    # Safety gate to ensure we don't multiply strings
    if (is.numeric(df_remapped$Wheat.Leaf.Height)) {
      df_remapped <- df_remapped %>%
        dplyr::mutate(Wheat.Leaf.Height = Wheat.Leaf.Height * 10)
      
      height_log_box <- c(
        "",
        "----------------------------------------------------------------------",
        " 📏 PIPELINE ACTION: DATA SCALED 📏",
        "----------------------------------------------------------------------",
        " -> Variable : 'Wheat.Leaf.Height'",
        " -> Action   : Multiplied by 10 (Unit conversion to mm)",
        "----------------------------------------------------------------------",
        ""
      )
      
      message(paste(height_log_box, collapse = "\n"))
      warning("Wheat.Leaf.Height was multiplied by 10 for unit conversion.", call. = FALSE)
      
    } else {
      warning("Warning [apply_name_corrections_Grass25]: 'Wheat.Leaf.Height' exists but is not numeric. Scaling by 10 was SKIPPED.", call. = FALSE)
    }
  }
  
  return(df_remapped)
}