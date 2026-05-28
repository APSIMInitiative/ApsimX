#' Universal CSV Exporter and Date Normalizer for APSIM-X Input Parameters
#'
#' @description
#' A highly resilient pipeline utility that intercepts data frames destined for APSIM-X injection, 
#' verifies or creates the underlying target disk directory structure, standardizes all date configurations 
#' into the absolute English locale string format (\code{DD-MMM-YYYY}), and exports a clean CSV payload.
#'
#' @details
#' **Locale Protection System:** APSIM Next Gen's internal engine requires three-letter month abbreviations 
#' to conform strictly to English syntax rules (e.g., \code{Jan}, \code{May}, \code{Aug}). This routine 
#' intercepts the system environment state, temporarily forces an explicit English (\code{"C"}) conversion scale 
#' during string assembly, and safely reverts the operating system back to its native global settings immediately 
#' afterwards. This guarantees portability when sharing pipelines across international multi-locale teams.
#' 
#' **Polymorphic Date Resolution:** The function scans the dataset for native R temporal classifications 
#' (\code{Date}, \code{POSIXct}, \code{POSIXlt}) and standardizes them. It also looks for character strings 
#' that contain date patterns, parsing and restructuring them seamlessly into the required format.
#'
#' @param df_input Data frame. The target input dataset or parameter map matrix to save.
#' @param folder_path String. The directory path where the output file should be written.
#' @param file_name_saved String. The exact name of the destination file (e.g., \code{"WaggaWagga2024_PhenoDatesInput.csv"}).
#'
#' @return A character string containing the normalized full path to the saved CSV file, 
#'   ensuring exact side-effect asset tracking inside the \code{{targets}} build framework.
#' @export
#'
#' @examples
#' \dontrun{
#' msg_param_saved <- save_input_param(
#'   df_input = df_apsimStageInput_haunBased,
#'   folder_path = config$folder_inputs,
#'   file_name_saved = config$file_name_input_pheno
#' )
#' }
save_input_param_csv <- function(df_input, folder_path, file_name_saved) {
  
  # ---- 1. SANITIZE ARGUMENTS ----
  if (missing(df_input) || is.null(df_input) || nrow(df_input) == 0) {
    stop("Error [save_input_param]: The parameter payload data frame 'df_input' is missing or empty.")
  }
  if (is.null(folder_path) || folder_path == "") {
    stop("Error [save_input_param]: The destination directory path 'folder_path' is missing or invalid.")
  }
  if (is.null(file_name_saved) || file_name_saved == "") {
    stop("Error [save_input_param]: The destination string 'file_name_saved' must be specified explicitly.")
  }
  
  # ---- 2. LOCATE AND UNIFY CHRONOLOGICAL TRACKS ----
  # Track explicit native date data types
  date_cols <- df_input %>%
    dplyr::select(dplyr::where(lubridate::is.Date) | dplyr::where(lubridate::is.POSIXt)) %>%
    names()
  
  # Also scan for character-encoded columns that look like standard calendar arrays
  char_cols <- setdiff(names(df_input), date_cols)
  potential_str_dates <- char_cols[sapply(df_input[char_cols], function(col) {
    if (!is.character(col)) return(FALSE)
    # Check if a sample row mirrors typical ISO or separator layout configurations
    sample_val <- col[!is.na(col) & col != "NA" & col != ""][1]
    if (is.na(sample_val)) return(FALSE)
    return(stringr::str_detect(sample_val, "^\\d{4}[-/]\\d{2}[-/]\\d{2}$|^\\d{2}[-/]\\d{2}[-/]\\d{4}$"))
  })]
  
  df_formatted <- df_input
  
  # ---- 3. SYSTEM LOCALE INTERCEPTION AND RE-ENCODING ----
  if (length(date_cols) > 0 || length(potential_str_dates) > 0) {
    
    # Store previous locale properties to restore afterward
    old_locale <- Sys.getlocale("LC_TIME")
    
    # Force international standard English naming rules for month strings (e.g. "May", "Aug")
    # This prevents Brazilian/European system configurations from printing regional variations
    if (.Platform$OS.type == "windows") {
      suppressWarnings(Sys.setlocale("LC_TIME", "English"))
    } else {
      suppressWarnings(Sys.setlocale("LC_TIME", "C"))
    }
    
    tryCatch({
      # Normalize native R dates first
      if (length(date_cols) > 0) {
        df_formatted <- df_formatted %>%
          dplyr::mutate(dplyr::across(
            dplyr::all_of(date_cols),
            ~ if_else(is.na(.x), NA_character_, format(.x, format = "%d-%b-%Y"))
          ))
      }
      
      # Clean and recast character string dates next
      if (length(potential_str_dates) > 0) {
        df_formatted <- df_formatted %>%
          dplyr::mutate(dplyr::across(
            dplyr::all_of(potential_str_dates),
            ~ {
              parsed_dt <- suppressWarnings(lubridate::parse_date_time(.x, orders = c("ymd", "dmy", "mdy")))
              if_else(is.na(parsed_dt), as.character(.x), format(parsed_dt, format = "%d-%b-%Y"))
            }
          ))
      }
    }, finally = {
      # CRITICAL SAFEGUARD: Revert the operating system back to its original native language state
      suppressWarnings(Sys.setlocale("LC_TIME", old_locale))
    })
  }
  
  # ---- 4. VERIFY FILE ENVIRONMENT AND ARTIFACT DEPLOYMENT ----
  # Build the complete path and fix directional slashes cleanly
  full_path <- file.path(folder_path, file_name_saved)
  full_path <- normalizePath(full_path, winslash = "/", mustWork = FALSE)
  
  if (!dir.exists(folder_path)) {
    dir.create(folder_path, recursive = TRUE)
  }
  
  # ---- 5. EMIT PAYLOAD ----
  # na = "" handles missing fields cleanly, which prevents APSIM database parsing anomalies
  readr::write_csv(
    x = df_formatted,
    file = full_path,
    na = "",
    append = FALSE
  )
  
  message(sprintf("Success [save_input_param]: Exported parameter asset directory file cleanly to: %s", full_path))
  
  return(full_path)
}