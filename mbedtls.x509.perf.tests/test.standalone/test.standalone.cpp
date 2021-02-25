#include "standalonetest.h"

int main()
{
    const int maxNumberThreads = 3;
    const int numberSecondsPerTestPass = 3;

    StandaloneTest myTestManager(maxNumberThreads, numberSecondsPerTestPass);

    myTestManager.RunAllTestsNow();
}

