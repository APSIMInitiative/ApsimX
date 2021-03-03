#' Loads a package, installing it if necessary.
#'
#' @param pkg Name of the package to be installed/loaded.
#' @param pkgpath Path where package is to be installed.
#' @return Nothing.
getPackage <- function(pkg, pkgpath) {
    print(pkg)
    if (!pkg %in% rownames(installed.packages())) {
        if (!dir.exists(pkgpath)) {
            dir.create(pkgpath)
        }
        install.packages(pkg, repos = "https://cran.csiro.au/", lib = pkgpath, dependencies = TRUE)
	} else {
		print(paste('Package', pkg, 'is already installed.'))
	}
}

args = commandArgs(TRUE)
getPackage(args[1], args[2])

