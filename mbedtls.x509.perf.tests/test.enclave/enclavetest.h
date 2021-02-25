#pragma once
#include "multithreadingtest.h"

class EnclaveTest : public MultiThreadingTest
{
public:
    EnclaveTest(int maxThreads, int secondsPerTestPass);
    void RunTestOnThisThread();

private:
}; 

