#' Loads a package, installing it if necessary.
#'
#' @param pkg Name of the package to be installed/loaded.
#' @param pkgpath Path where package is to be installed.
#' @return Nothing.
getPackage <- function(pkg, pkgpath) {
    pkgName <- pkg
    if (grepl('/', pkgName, fixed = TRUE)) {
        # if pkg looks like owner/repo, we want pkgName to
        # be just "repo"
        pkgName <- unlist(strsplit(pkgName, '/'))[2]
	}
    if (!pkgName %in% rownames(installed.packages(lib.loc = pkgpath))) {
        if (!dir.exists(pkgpath)) {
            dir.create(pkgpath)
        }
        devtools::install_github(pkg, lib = pkgpath)
	} else {
		print(paste('Package', pkg, 'is already installed.'))
	}
}

args = commandArgs(TRUE)
getPackage(args[1], args[2])
