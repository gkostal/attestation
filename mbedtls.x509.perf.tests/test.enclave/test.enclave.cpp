#include "enclavetest.h"

int main()
{
    const int maxNumberThreads = 8;
    const int numberSecondsPerTestPass = 6;

    EnclaveTest myTestManager(maxNumberThreads, numberSecondsPerTestPass);

    myTestManager.RunAllTestsNow();
}

