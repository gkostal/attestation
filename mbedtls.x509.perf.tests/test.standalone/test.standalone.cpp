#include "standalonetest.h"

int main()
{
    const int maxNumberThreads = 8;
    const int numberSecondsPerTestPass = 6;

    StandaloneTest myTestManager(maxNumberThreads, numberSecondsPerTestPass);

    myTestManager.RunAllTestsNow();
}

