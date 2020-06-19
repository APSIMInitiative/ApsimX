## libSQLite.Interop.so

Build instructions:

1. Get code from [here](https://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki) (look for sqlite-netFx-full-source-xxxxx.zip)

```sh
$ unzip sqlite-netFx-full-source-xxxxx.zip
$ cd Setup
$ chmod u+x compile-interop-assembly-release.sh
$ ./compile-interop-assembly-release.sh
$ result=../bin/2013/Release/bin/libSQLite.so.Interop.so
```