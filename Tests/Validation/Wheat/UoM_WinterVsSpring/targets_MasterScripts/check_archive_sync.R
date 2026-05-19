#' Verify Folder and Zip Archive Synchronization
#'
#' @description
#' Compares the contents of a live directory against the contents of a .zip archive.
#' Warns the user if files are missing from the zip (unsaved work) or missing 
#' from the folder (unextracted data).
#'
#' @param target_folder Character. The live directory to check.
#' @param zip_file Character. The path to the .zip archive.
#'
#' @return Logical TRUE if perfectly synced, FALSE if out of sync.
#' @export
check_archive_sync <- function(target_folder, zip_file) {
  
  if (!dir.exists(target_folder)) stop("Folder not found: ", target_folder)
  if (!file.exists(zip_file)) stop("Zip file not found: ", zip_file)
  
  # 1. Get files in the live folder
  folder_files <- list.files(target_folder, recursive = TRUE, full.names = FALSE)
  
  # 2. Get files inside the Zip archive
  zip_info <- unzip(zip_file, list = TRUE)
  # Filter out directory stubs (Length == 0) to only compare actual files
  zip_files <- zip_info$Name[zip_info$Length > 0] 
  
  # 3. Compare them
  missing_in_zip <- setdiff(folder_files, zip_files)
  missing_in_folder <- setdiff(zip_files, folder_files)
  
  is_synced <- (length(missing_in_zip) == 0 && length(missing_in_folder) == 0)
  
  if (!is_synced) {
    warning_msg <- c(
      "\n======================================================================",
      " \u26A0\uFE0F ARCHIVE SYNC WARNING: FOLDER AND ZIP DO NOT MATCH \u26A0\uFE0F",
      "======================================================================"
    )
    
    if (length(missing_in_zip) > 0) {
      warning_msg <- c(warning_msg, 
                       " Files in folder but missing from ZIP (Needs Backup):", 
                       paste("   ->", missing_in_zip))
    }
    
    if (length(missing_in_folder) > 0) {
      warning_msg <- c(warning_msg, 
                       " Files in ZIP but missing from folder (Needs Extraction):", 
                       paste("   ->", missing_in_folder))
    }
    
    warning_msg <- c(warning_msg, "======================================================================\n")
    warning(paste(warning_msg, collapse = "\n"), call. = FALSE)
    
  } else {
    message("\u2705 Archive Sync Passed: Live folder and Zip backup perfectly match.")
  }
  
  return(invisible(is_synced))
}