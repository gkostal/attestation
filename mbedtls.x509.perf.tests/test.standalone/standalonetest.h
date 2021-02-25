#pragma once
#include <mbedtls/x509_crt.h>
#include "multithreadingtest.h"

class StandaloneTest : public MultiThreadingTest
{
public:
    StandaloneTest(std::string testType, int maxThreads, int secondsPerTestPass) : MultiThreadingTest(testType, maxThreads, secondsPerTestPass) {}
    void RunTestOnThisThread();
};