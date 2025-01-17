#setwd("~/work/Projects/GRDC NaPA 2022/R-scratch/")
#rm(list=ls())

# S3 class wrapping a javascript object in a V8 interpreter
# 
# query routines allow discovery/selection
# get/set routines allow interchange between R and V8 variables


library("V8")
# Documentation at https://github.com/dchester/jsonpath
if (!file.exists("jsonpath.min.js")) {
  download.file("https://unpkg.com/jsonpath@1.1.1/jsonpath.min.js",
                "jsonpath.min.js")
}


reset.jsonManip <- function(v8ctx) {
  v8ctx$reset() 
  v8ctx$source("jsonpath.min.js")
  
  v8ctx$eval("function uid() {
 var s = '_' + ('000000000' + Math.random().toString(36).substr(2, 9)).slice(-9);
 return (s);};")
  
  v8ctx$eval("/* utf.js - UTF-8 <=> UTF-16 convertion
*
  * Copyright (C) 1999 Masanao Izumo <iz@onicos.co.jp>
  * Version: 1.0
* LastModified: Dec 25 1999
* This library is free.  You can redistribute it and/or modify it.
*/
  
  function Utf8ArrayToStr(array) {
    var out, i, len, c;
    var char2, char3;
    
    out = \"\";
    len = array.length;
    i = 0;
    while(i < len) {
      c = array[i++];
      switch(c >> 4)
      { 
        case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
          // 0xxxxxxx
        out += String.fromCharCode(c);
        break;
        case 12: case 13:
          // 110x xxxx   10xx xxxx
        char2 = array[i++];
        out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
        break;
        case 14:
          // 1110 xxxx  10xx xxxx  10xx xxxx
        char2 = array[i++];
        char3 = array[i++];
        out += String.fromCharCode(((c & 0x0F) << 12) |
                                     ((char2 & 0x3F) << 6) |
                                     ((char3 & 0x3F) << 0));
        break;
      }
    }
    return out;
  }
  function StrToUInt8Array (s) {
    return(Uint8Array.from(s.split(\"\").map(x => x.charCodeAt())));
  }")
}

# Constructor
# string argument can be 
# - a file
# - a structured list (eg from JS())
# - a string, encoding a JSON object
# The object is given a unique name in the V8 global namespace
jsonManip <- function(x = {}) {
  name = v8ctx$eval("uid();")
  if (length(x) == 1 && class(x) == "character" && file.exists(x)) {
      x <- readBin(x, "raw", file.info(x)$size)
      v8ctx$assign(name, x, auto_unbox = F)
      v8ctx$eval(paste0(name, " = JSON.parse(Utf8ArrayToStr(", name, "));"))
  } else if (length(x) > 0 && class(x) == "list") {
    v8ctx$assign(name, x)
  } else {
    v8ctx$assign(name, JS(x))
  }
  return(structure(list(jsName = name), class="jsonManip"))
}

# deep copy
jsonClone <- function(x) {
  stopifnot(class(x) == "jsonManip")
  newName = v8ctx$eval("uid();")
  v8ctx$eval(paste0(newName, " = JSON.parse(JSON.stringify(", x$jsName, "));"), await=T)
  return(structure(list(jsName = newName), class="jsonManip"))
}

print.jsonManip <- function (x) {
  cat("js object named", x$jsName, "\n")
}

summary.jsonManip <- function(x) {
  v8ctx$eval(paste0("function roughSizeOfObject( object ) {
    var objectList = [];
    var stack = [ object ];
    var bytes = 0;
    while ( stack.length ) {
      var value = stack.pop();
      if ( typeof value === 'boolean' ) {
        bytes += 4;
      }
      else if ( typeof value === 'string' ) {
        bytes += value.length * 2;
      }
      else if ( typeof value === 'number' ) {
        bytes += 8;
      }
      else if ( typeof value === 'object'
               && objectList.indexOf( value ) === -1)
      {
        objectList.push( value );
        for( var i in value ) {
          stack.push( value[ i ] );
        }
      }
    }
    return bytes;
  }"))
  print(paste("json object of", v8ctx$eval(paste0("roughSizeOfObject(", x$jsName,");")), " bytes"))
}

#getIt.jsonManip <- function(x, name = "") {
#  if (name == "") {
#    return(v8ctx$eval( x$jsName, serialize = T, await=T))
#  } else {
#    stop("fixme")
# }
#}
#write.jsonManip <- function(x, filename, pp=T) {
#  str <- getIt.jsonManip(x)
#  if (pp){
#    cat(
#      jsonlite::toJSON(
#        jsonlite::fromJSON(str, simplifyVector=F, flatten=T),
#        null="null", pretty = 2, auto_unbox = T, digits = 8), file=filename)
#  } else { 
#     cat(str, file=filename)
#  }
#}

# return the first values for a path expression
query <- function(x, jsonPath) {
  pathVar = v8ctx$eval("uid();");   v8ctx$assign(pathVar, jsonPath, auto_unbox = T)
  res <- v8ctx$eval(
    paste0("jsonpath.query(", x$jsName, ",", pathVar, ");"),
       serialize = T, await=T)
  if (length(res) > 0) {
     return(jsonlite::fromJSON(res, simplifyVector = F))
  }
  return(NULL)
}

# return all paths for a path expression
paths <- function(x, jsonPath) {
  pathVar = v8ctx$eval("uid();");   v8ctx$assign(pathVar, jsonPath, auto_unbox = T)
  res <- v8ctx$eval(
    paste0("jsonpath.paths(", x$jsName, ",", pathVar, ");"),
      serialize = T, await=T)
  if (length(res) > 0) {
    return(jsonlite::fromJSON(res, simplifyVector = F))
  }
  return(NULL)
}

# delete leaves/branches from a path expression
delete.jsonManip <- function(x, jsonPath) {
  paths <- strsplit(v8ctx$eval(paste0("jsonpath.paths(", x$jsName, ",\"",  jsonPath, "\").reverse()"), await=T), ",")
  # delete in reverse so that array indexes remain correct
  for (path in paths) {
    # Quote strings, leave numbers (array positions) open
    path <- path[-1] # remove leading '$'
    stopifnot (length(path) > 0)
    for ( i in 1:length(path)) {
      if (!grepl("^[0-9]+$", path[i])) {path[i] <- paste0("'", path[i], "'")}
    }
    pathLen <- length(path)
    if (grepl("^[0-9]+$", path[pathLen])) {
      # arrays use splice
      arr <- paste0("[", paste(path[-1 * pathLen], collapse= "]["), "]")
      js <-paste0(x$jsName, arr, ".splice(", path[pathLen], ",1);")
    } else {
      # objects are deleted
      js <-paste0("delete ", x$jsName, "[", paste(path, collapse= "]["), "];")
    }
    #cat("delete js=", js, "\n")
    v8ctx$eval(js, await=T);
  }
}

# result: "object" return as R (scalar / structured list) object
#         "string" as json string
#         "js"     as jsonManip class
getValue <- function(x, jsonPath = "", result = "string") {
  stopifnot(class(x) == "jsonManip")
  if (jsonPath == "") {
    if (result == "object") {
      return(jsonlite::fromJSON(v8ctx$eval(x$jsName, serialize=T, await=T), simplifyVector = F))
    } else if (result == "string") {
      return(v8ctx$eval(x$jsName, serialize=T, await=T))
    } else if (result == "js") {
      newName = v8ctx$eval("uid();")
      v8ctx$eval(paste0(newName, " = JSON.parse(JSON.stringify(", x$jsName, "));"), await=T)
      return(structure(list(jsName = newName), class="jsonManip"))
    } 
    stop(paste0("Unknown result type", result))
  }
  t1Var = v8ctx$eval("uid();"); t2Var = v8ctx$eval("uid();"); t3Var = v8ctx$eval("uid();")
  v8ctx$assign(t2Var, jsonPath, auto_unbox = T)
  v8ctx$eval(paste0("var ", t3Var, "; ", t1Var, " = jsonpath.paths(",x$jsName, ",", t2Var,
                    ").map(p => jsonpath.value(",x$jsName,", jsonpath.stringify(p)));",
                    "if (", t1Var, ".length == 1) {", t3Var, " = ", t1Var, "[0];} else {", 
                     t3Var, " = ", t1Var, ";}"),
             serialize=T, await=T)

  if (result == "object") {
    return(jsonlite::fromJSON(v8ctx$eval(t3Var, serialize=T, await=T), simplifyVector = F))
  } else if (result == "string") {
    return(v8ctx$eval(t3Var, serialize=T, await=T))
  } else if (result == "js") {
    newName = v8ctx$eval("uid();")
    v8ctx$eval(paste0(newName, " = JSON.parse(JSON.stringify(", t3Var, "));"), await=T)
    return(structure(list(jsName = newName), class="jsonManip"))
  } 
  stop(paste0("Unknown result type", result))
}

setValue<- function(x, jsonPath, newValue) {
  pathVar = v8ctx$eval("uid();")
  v8ctx$assign(pathVar, jsonPath, auto_unbox = T)

  newValueVar = v8ctx$eval("uid();")
  if (class(newValue) == "jsonManip") {
    v8ctx$eval(paste0(newValueVar, " = ", jsonClone(newValue)))
  } else {
    v8ctx$assign(newValueVar, newValue, auto_unbox = T)
  }
  
  res <- v8ctx$eval(paste0("jsonpath.paths(",x$jsName, ",", pathVar,
                           ").map(p => jsonpath.value(",
                           x$jsName,", jsonpath.stringify(p),", newValueVar, "))"),
                    serialize = T, await=T)

  if (length(res) > 0) {
    return(jsonlite::fromJSON(res))
  }
  return(NULL)
}

# Append a child to a leaf
appendChild<- function(x, jsonPath, newValue) {
  newValueVar = v8ctx$eval("uid();")
  if (class(newValue) == "jsonManip") {
    v8ctx$eval(paste0(newValueVar, " = ", newValue$jsName))
  } else {
    v8ctx$assign(newValueVar, newValue, auto_unbox = T)
  }
  
  paths <- strsplit(v8ctx$eval(paste0("jsonpath.paths(", x$jsName, ",\"",  jsonPath, "\")"), await=T), ",")
  for (path in paths) {
    #cat("path=", path, "\n")
    path <- path[-1]
    stopifnot (length(path) > 0)
    # Quote strings, leave numbers (array positions) open
    for ( i in 1:length(path)) {
      if (!grepl("^[0-9]+$", path[i])) {path[i] <- paste0("'", path[i], "'")}
    }
    arr <- paste0("[", paste(path, collapse= "]["), "]")
    js <-paste0(x$jsName, arr, ".push(", newValueVar, ");")
    #cat("append js=", js, "\n")
    v8ctx$eval(js, await=T);
  }
}  


v8ctx <- v8()
reset.jsonManip(v8ctx)
#tests
if (F) {
  x <- jsonManip("~/Downloads/zz.apsimx")
  writeLines(getValue(x, "", result="string"), "~/Downloads/zzz.apsimx")
  writeLines(jsonlite::toJSON(jsonlite::fromJSON(getValue(x), simplifyVector=F, flatten=T),
                              null="null", pretty = 2, auto_unbox = T, digits = 8), "~/Downloads/zzz.apsimx")
  
  unlist(query(x, "$..Name"))
  query(x, "$..[?(@.Name=='Weather')].FileName")
  query(x, "$..[?(@.Name=='Weather')].FileNameMissing")
  query(x, "$..[?(@.Name=='Soil')].LocalName")
  query(x, "$..[?(@.Name=='Weather')]")
  
  
  # Simple values
  getValue(x, "$..[?(@.Name=='Weather')].FileName")
  setValue(x, "$..[?(@.Name=='Weather')].FileName", "blork")
  getValue(x, "$..[?(@.Name=='Weather')].FileName")
  getValue(x, "$..[?(@.Name=='Weather')].FileName", result = "js")
  getValue(x, "$..[?(@.Name=='Weather')].FileName", result = "object")
  
  # Compound values
  z <- getValue(x, "$..[?(@.Name=='Weather')]", result = "object")
  z$Enabled <- FALSE
  setValue(x, "$..[?(@.Name=='Weather')]", z)
  getValue(x, "$..[?(@.Name=='Weather')]")
  
  delete.jsonManip(x, "$..[?(@.Name=='Weather')]")
  getValue(x, "$..[?(@.Name=='Weather')]")

  # Array values
  z <- getValue(x, "$..[?(@.Name=='Water')].Thickness", result="object")
  setValue(x, "$..[?(@.Name=='Water')].Thickness", 2 * unlist(z))
  
  getValue(x, "$..[?(@.$type=='Models.Fertiliser, Models')]")
  getValue(x, "$..[?(@.$type=='Models.Fertiliser, Modelz')]")

  z<- getValue(x, result="js")
    
  #write.jsonManip(x, "~/Downloads/zzz.apsimx")
  
  v8ctx$get(JS("Object.keys(global)"))
  reset.jsonManip(v8ctx)
  v8ctx$get(JS("Object.keys(global)"))
}

if (F) {
  soils <- jsonManip("./Soils.apsimx")
  cat(
    "{
  \"$type\": \"Models.Core.Simulations, Models\",
  \"ExplorerWidth\": 285,
  \"Version\": 155,
  \"Name\": \"Simulations\",
  \"ResourceName\": null,
  \"Children\": [{
      \"$type\": \"Models.Core.Folder, Models\",
      \"ShowInDocs\": false,
      \"GraphsPerPage\": 6,
      \"Name\": \"Soils\",
      \"ResourceName\": null,
      \"Children\": [",
    file = "Soils2.apsimx"
  )
  first <- T
  for (soil in paths(soils, "$..[?(@.$type=='Models.Soils.Soil, Models')]")) {
    if (!first) {
      cat(",", file = "Soils2.apsimx", append = T)
    }
    txt <- getValue(soils, paste0(unlist(soil), collapse = "."))
    cat(
      jsonlite::toJSON(txt,
        null = "null", pretty = 2, auto_unbox = T, digits = 8
      ),
      file = "Soils2.apsimx",
      append = T
    )
    first <- F
  }
  cat("]}]}", file = "Soils2.apsimx", append = T)
}

#v8ctx$eval( x$jsName, serialize = T, await=T)
