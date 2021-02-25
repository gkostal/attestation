#pragma once
#include "multithreadingtest.h"

class EnclaveTest : public MultiThreadingTest
{
public:
    EnclaveTest(int minThreads, int maxThreads, int secondsPerTestPass);
    ~EnclaveTest();
    void RunTestOnThisThread();
    void StopTestsNow();

private:
}; 

