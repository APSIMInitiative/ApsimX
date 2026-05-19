#' Securely Zip a Folder using 7-Zip (Universal Master)
#'
#' @description
#' Compresses a directory into an AES-256 encrypted .zip archive using 7-Zip.
#' Includes strict pathfinding, command-line sanitization, error capturing, 
#' and "Ghost Zip" detection.
#'
#' @param input_folder Character. The directory to compress.
#' @param output_zip Character. The full path of the output .zip file.
#' @param pass_file Character. The text file containing the password.
#'
#' @return Character. The exact path to the output zip file (for `{targets}` tracking).
#' @export
secure_zip_folder <- function(input_folder, output_zip, pass_file) {
  
  # ------------------------------------------------------------------
  # 1. INPUT VALIDATION & SETUP
  # ------------------------------------------------------------------
  if (!dir.exists(input_folder)) {
    stop("CRITICAL: Input folder to zip does not exist: ", input_folder)
  }
  
  if (!file.exists(pass_file)) {
    stop("CRITICAL: Password file not found at ", pass_file)
  }
  
  secret_pass <- trimws(readLines(pass_file, warn = FALSE)[1])
  if (length(secret_pass) == 0 || nchar(secret_pass) == 0) {
    stop("CRITICAL: Password file is empty!")
  }
  
  # ------------------------------------------------------------------
  # 2. OUTPUT PREPARATION
  # ------------------------------------------------------------------
  if (file.exists(output_zip)) {
    file.remove(output_zip)
  }
  
  # Ensure the target directory actually exists
  out_dir <- dirname(output_zip)
  if (!dir.exists(out_dir)) {
    dir.create(out_dir, recursive = TRUE)
  }
  
  # ------------------------------------------------------------------
  # 3. THE PATHFINDER (Find 7-Zip explicitly)
  # ------------------------------------------------------------------
  exe_7z <- Sys.which("7z")
  
  if (exe_7z == "") {
    # If the background worker can't find it, fallback to default Windows path
    fallback_path <- "C:/Program Files/7-Zip/7z.exe"
    if (file.exists(fallback_path)) {
      exe_7z <- fallback_path 
    } else {
      stop("CRITICAL: 7-Zip is not in PATH and not at C:/Program Files/7-Zip/7z.exe")
    }
  }
  
  # ------------------------------------------------------------------
  # 4. CONSTRUCT & EXECUTE SYSTEM COMMAND
  # ------------------------------------------------------------------
  # shQuote() safely wraps strings for the command line, preventing space/character crashes
  cmd <- sprintf(
    '%s a -tzip -p%s -mem=AES256 %s %s',
    shQuote(exe_7z),
    shQuote(secret_pass), 
    shQuote(output_zip), 
    shQuote(file.path(input_folder, "*"))
  )
  
  message("Encrypting archive...")
  sys_result <- system(cmd, intern = TRUE)
  
  # ------------------------------------------------------------------
  # 5. ERROR CAPTURING & GHOST ZIP DETECTION
  # ------------------------------------------------------------------
  # R stores the error code as an attribute. If it succeeded, it is NULL.
  exit_status <- attr(sys_result, "status")
  
  if (!is.null(exit_status) && exit_status != 0) {
    stop(
      "\n========================================================\n",
      "🚨 CRITICAL 7-ZIP ERROR (Status ", exit_status, ") 🚨\n",
      "========================================================\n",
      "7-Zip failed to zip the files. Last lines from 7-Zip:\n  ", 
      paste(tail(sys_result, 5), collapse = "\n  "), "\n",
      "========================================================\n"
    )
  }
  
  if (!file.exists(output_zip)) {
    stop("CRITICAL: The archive was not created at all.")
  }
  
  # An empty AES-256 zip file wrapper is tiny (usually ~22 to 100 bytes).
  # If it's less than 500 bytes, we know absolutely no data was added.
  zip_size <- file.info(output_zip)$size
  
  if (zip_size < 500) {
    # Delete the empty ghost file so it doesn't trick the pipeline later
    file.remove(output_zip)
    
    stop(
      "\n========================================================\n",
      "🚨 GHOST ZIP DETECTED 🚨\n",
      "========================================================\n",
      "The zip file was created, but NO FILES WERE ADDED to it.\n",
      "Double-check that this path actually contains files:\n", 
      " -> ", input_folder, "\n",
      "========================================================\n"
    )
  }
  
  message("\u2705 Successfully created password-protected archive: ", basename(output_zip))
  
  # 6. Return the exact file path for `{targets}` tracking
  return(output_zip)
}