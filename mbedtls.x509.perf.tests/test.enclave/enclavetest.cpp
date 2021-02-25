#include "enclavetest.h"

extern "C" {
    void create_enclave();
    void terminate_enclave();
    int call_enclave_startTesting();
    int call_enclave_stopTesting();
}

EnclaveTest::EnclaveTest(int minThreads, int maxThreads, int secondsPerTestPass)
    : MultiThreadingTest("EnclaveHosted", minThreads, maxThreads, secondsPerTestPass)
{
    create_enclave();
}

EnclaveTest::~EnclaveTest()
{
    terminate_enclave();
}

void EnclaveTest::RunTestOnThisThread()
{
    int result = call_enclave_startTesting();
    
    IncrementSuccessCount(result);
    IncrementTotalCount(result);
}

void EnclaveTest::StopTestsNow()
{
    call_enclave_stopTesting();
    MultiThreadingTest::StopTestsNow();
}
