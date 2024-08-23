#!/usr/bin/python3
"""
utils.py
"""
import json;
import logging;
from typing import Union;


# File I/O.
def _load_file_default(fpath: str) -> str:
    with open(fpath) as fp:
        contents = fp.read();
    logging.debug("Read contents of file: {}.".format(fpath));
    return contents;

def _load_json(fpath: str) -> str:
    with open(fpath) as fp:
        contents = json.load(fp);
    logging.debug("Read contents of JSON file: {}.".format(fpath));
    return contents;

def load_file(fpath: str) -> Union[dict, str]:
    try:
        ext = fpath.split(".")[-1];
    except:
        return _load_file_default(fpath);
    if (ext == "json" or ext == "apsimx"):
        return _load_json(fpath);
    return _load_file_default(fpath);

def _write_file_default(content: str, fpath: str) -> None:
    with open(fpath, "w+") as fp:
        fp.write(content);
    logging.debug("Wrote to {}.".format(fpath));

def _write_json(content: Union[dict, str], fpath: str) -> None:
    if (type(content) == dict):
        content = json.dumps(content);
    _write_file_default(content, fpath);

def write_file(content: Union[dict, str], fpath: str) -> None:
    try:
        ext = fpath.split(".")[-1];
    except:
        return _write_file_default(content, fpath);
    if (ext == "json"):
        return _write_json(content, fpath);
    return _write_file_default(content, fpath);


if __name__ == "__main__":
    print("Hi nub.");
