#!/usr/bin/env python3
"""Print the paths of PE files (exe/dll) under a directory that have no Authenticode signature."""
import os
import struct
import sys


def is_unsigned_pe(path):
    with open(path, "rb") as f:
        header = f.read(64)
        if len(header) < 64 or header[:2] != b"MZ":
            return False
        pe_offset = struct.unpack_from("<I", header, 0x3C)[0]
        f.seek(pe_offset)
        if f.read(4) != b"PE\0\0":
            return False
        f.seek(pe_offset + 24)
        magic = struct.unpack("<H", f.read(2))[0]
        # Offset of the data directories within the optional header.
        dirs_offset = 112 if magic == 0x20B else 96
        # Security directory is data directory index 4 (8 bytes each).
        f.seek(pe_offset + 24 + dirs_offset + 4 * 8)
        cert_rva, cert_size = struct.unpack("<II", f.read(8))
        return cert_size == 0


def main():
    root = sys.argv[1]
    for dirpath, _, files in os.walk(root):
        for name in sorted(files):
            if name.lower().endswith((".exe", ".dll")):
                path = os.path.join(dirpath, name)
                if is_unsigned_pe(path):
                    print(path)


if __name__ == "__main__":
    main()
