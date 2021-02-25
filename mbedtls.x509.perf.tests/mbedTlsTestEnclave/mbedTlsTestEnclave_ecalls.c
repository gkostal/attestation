#include <stdio.h>
#include "mbedTlsTestEnclave_t.h"

#define HEAP_SIZE_BYTES (2 * 1024 * 1024) /* 2 MB */
#define STACK_SIZE_BYTES (24 * 1024)      /* 24 KB */

#define SGX_PAGE_SIZE (4 * 1024)

#define TA_UUID /* 90b152e6-503f-4cf7-b3a9-0772a2ad9b7f */ {0x90b152e6,0x503f,0x4cf7,{0xb3,0xa9,0x07,0x72,0xa2,0xad,0x9b,0x7f}}

OE_SET_ENCLAVE_OPTEE(
    TA_UUID,               /* UUID */
    HEAP_SIZE_BYTES,       /* HEAP_SIZE */
    STACK_SIZE_BYTES,      /* STACK_SIZE */
    TA_FLAG_MULTI_SESSION, /* FLAGS */
    "1.0.0",               /* VERSION */
    "mbedTlsTestEnclave TA");   /* DESCRIPTION */

OE_SET_ENCLAVE_SGX(
    1, /* ProductID */
    1, /* SecurityVersion */
#ifdef _DEBUG
    1, /* Debug */
#else
    0, /* Debug */
#endif
    HEAP_SIZE_BYTES / SGX_PAGE_SIZE,  /* NumHeapPages */
    STACK_SIZE_BYTES / SGX_PAGE_SIZE, /* NumStackPages */
    12);                              /* NumTCS */

int IMPL_mbedTlsTestMethod();
int IMPL_stopTesting();

int ECALL_mbedTlsTestMethod(void)
{
    return IMPL_mbedTlsTestMethod();
}

int ECALL_stopTesting(void)
{
    return IMPL_stopTesting();
}
