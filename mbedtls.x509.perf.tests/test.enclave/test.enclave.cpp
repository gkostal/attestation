#include "enclavetest.h"

int main()
{
    const int maxNumberThreads = 3;
    const int numberSecondsPerTestPass = 3;

    EnclaveTest myTestManager(maxNumberThreads, numberSecondsPerTestPass);

    myTestManager.RunAllTestsNow();
}

