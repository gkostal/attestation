# Copyright (c) Open Enclave SDK contributors.
# Licensed under the MIT License.

include ../config.mk

CFLAGS=$(shell pkg-config oehost-$(C_COMPILER) --cflags)
CXXFLAGS=$(shell pkg-config oehost-$(CXX_COMPILER) --cflags)
LDFLAGS=$(shell pkg-config oehost-$(CXX_COMPILER) --libs)
INCDIR=$(shell pkg-config oehost-$(C_COMPILER) --variable=includedir)

all: build

build:
	@ echo "Compilers used: $(CC), $(CXX)"
	oeedger8r ../remoteattestation.edl --untrusted --search-path $(INCDIR) --search-path $(INCDIR)/openenclave/edl/sgx
	$(CC) -g -c $(CFLAGS) $(CINCLUDES) remoteattestation_u.c
	$(CXX) -g -c $(CXXFLAGS) $(INCLUDES) host.cpp quotefile.cpp
	$(CXX) -o genquote_host host.o quotefile.o remoteattestation_u.o $(LDFLAGS)

clean:
	rm -f genquote_host *.o remoteattestation_u.*  remoteattestation_args.h



