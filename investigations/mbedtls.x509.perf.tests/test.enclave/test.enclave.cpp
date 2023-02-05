#include "enclavetest.h"

int main()
{
    const int minNumberThreads = 1;
    const int maxNumberThreads = 8;
    const int numberSecondsPerTestPass = 6;

    EnclaveTest myTestManager(minNumberThreads, maxNumberThreads, numberSecondsPerTestPass);

    myTestManager.RunAllTestsNow();
}

