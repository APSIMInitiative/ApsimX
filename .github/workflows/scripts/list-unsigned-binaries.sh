#!/usr/bin/env bash
#
# Prints the paths of PE files (exe/dll) under a directory that have no Authenticode signature.
# A file is unsigned if the Security data directory in its PE header is empty.
#
# Usage: list-unsigned-binaries.sh <directory>
set -e

test $# -eq 1 || { echo "Usage: $0 <directory>"; exit 1; }

# read_uint <file> <offset> <bytes> - prints a little endian unsigned integer.
function read_uint() {
    od -An --endian=little -t u$3 -j "$2" -N "$3" "$1" | tr -d ' '
}

# read_hex <file> <offset> <bytes> - prints bytes as a contiguous hex string.
function read_hex() {
    od -An -t x1 -j "$2" -N "$3" "$1" | tr -d ' \n'
}

function is_unsigned_pe() {
    local file=$1
    [[ $(read_hex "$file" 0 2) == "4d5a" ]] || return 1 # "MZ"

    local pe_offset
    pe_offset=$(read_uint "$file" 60 4)
    [[ -n "$pe_offset" && $(read_hex "$file" "$pe_offset" 4) == "50450000" ]] || return 1 # "PE\0\0"

    # The optional header starts 24 bytes after the PE signature. Its magic
    # number tells us whether this is PE32+ (0x20b) or PE32.
    local optional=$((pe_offset + 24))
    local dirs_offset=96
    [[ $(read_uint "$file" "$optional" 2) -eq 523 ]] && dirs_offset=112

    # The Security directory is data directory 4 (8 bytes each: RVA then size).
    local cert_size
    cert_size=$(read_uint "$file" $((optional + dirs_offset + 4 * 8 + 4)) 4)
    [[ -n "$cert_size" && "$cert_size" -eq 0 ]]
}

while IFS= read -r file; do
    if is_unsigned_pe "$file"; then
        echo "$file"
    fi
done < <(find "$1" -type f \( -iname '*.exe' -o -iname '*.dll' \) | sort)
