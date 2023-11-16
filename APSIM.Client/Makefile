SHELL=/bin/bash
CFLAGS= -Wall -Werror -pedantic -O3 -fPIC
LDFLAGS=-lapsimclient
INCLUDES=src/
CHECKFLAGS=`pkg-config --cflags --libs check`
OUTPUT=libapsimclient.so

prefix=/usr/local
exec_prefix=$(prefix)
includedir=$(prefix)/include
libdir=$(exec_prefix)/lib
INSTALL=install -CDv -m 644
HEADERS=src/{apsimclient,replacement}.h
PC=src/apsimclient.pc

.PHONY: all clean install uninstall
all: $(OUTPUT) example unittests
clean:
	$(RM) $(OUTPUT) example unittests
install:
	$(INSTALL) $(OUTPUT) $(DESTDIR)$(libdir)/$(OUTPUT)
	for f in $(HEADERS); do $(INSTALL) $$f $(DESTDIR)$(includedir)/`basename $$f`; done
	$(INSTALL) $(PC) $(DESTDIR)$(libdir)/pkgconfig/`basename $(PC)`
uninstall:
	for f in $(HEADERS); do $(RM) $(DESTDIR)$(includedir)/`basename $$f`; done
	$(RM) $(DESTDIR)$(libdir)/$(OUTPUT)
	$(RM) $(DESTDIR)$(libdir)/pkgconfig/`basename $(PC)`

$(OUTPUT): src/client.c src/replacement.c src/encode.c src/protocol.c
	$(CC) -I $(INCLUDES) $(CFLAGS) -shared $^ -o $@
example: examples/main.c $(OUTPUT)
	$(CC) -L. -I $(INCLUDES) $(CFLAGS) $(LDFLAGS) $^ -o $@
unittests: tests/tests.c tests/test_replacements.c tests/test_client.c $(OUTPUT)
	$(CC) -L. -I $(INCLUDES) $(CFLAGS) -pthread $(LDFLAGS) $^ $(CHECKFLAGS) -o $@
