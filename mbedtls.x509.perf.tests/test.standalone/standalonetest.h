#pragma once
#include <mbedtls/x509_crt.h>
#include "multithreadingtest.h"

class StandaloneTest : public MultiThreadingTest
{
public:
    StandaloneTest(int maxThreads, int secondsPerTestPass);
    void RunTestOnThisThread();

private:
    const static mbedtls_x509_crt_profile mbedtls_x509_crt_profile_test;
};