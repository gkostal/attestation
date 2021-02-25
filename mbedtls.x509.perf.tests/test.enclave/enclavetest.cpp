#include "enclavetest.h"

EnclaveTest::EnclaveTest(int maxThreads, int secondsPerTestPass)
    : MultiThreadingTest("EnclaveHosted", maxThreads, secondsPerTestPass)
{
}

void EnclaveTest::RunTestOnThisThread()
{
    IncrementSuccessCount(1000);
    IncrementTotalCount(1000);
}
