#include "multithreadingtest.h"

MultiThreadingTest::MultiThreadingTest(std::string testType, int maxThreads, int secondsPerTestPass)
	: 
    _testType(testType), 
    _maxThreads(maxThreads), 
    _secondsPerTestPass(secondsPerTestPass), 
    _stopTestNow(false), 
    _successCount(0), 
    _totalCount(0)
{
}

void MultiThreadingTest::StopTestsNow()
{
    _stopTestNow = true;
}

bool MultiThreadingTest::ShouldTestsStop()
{
	return _stopTestNow;
}

void MultiThreadingTest::IncrementSuccessCount(long amount)
{
    InterlockedAdd(&_successCount, amount);
}

void MultiThreadingTest::IncrementTotalCount(long amount)
{
    InterlockedAdd(&_totalCount, amount);
}

void MultiThreadingTest::PrintfImpl(const char * const format, ...)
{
    va_list vl;
    va_start(vl, format);
    vprintf(format, vl);
    va_end(vl);
}

DWORD __stdcall MultiThreadingTest::ThreadStart(LPVOID lpParam)
{
    MultiThreadingTest* pTestClass = (MultiThreadingTest *) lpParam;
    pTestClass->RunTestOnThisThread();
    return 0;
}

void MultiThreadingTest::ResetState()
{
    _stopTestNow = false;
    _successCount = 0;
    _totalCount = 0;
}

void MultiThreadingTest::RunAllTestsNow()
{
    PrintfImpl("\n");
#ifdef _DEBUG
    PrintfImpl("%s - Debug Test\n", _testType.c_str());
#else
    PrintfImpl("%s - Release Test\n", _testType.c_str());
#endif
    PrintfImpl("Thread Count, Total Time, Total Count, Total RPS, RPS Per Thread\n");

    for (int i = 1; i <= _maxThreads; i++)
    {
        HANDLE* ahThread = (HANDLE*)calloc(i, sizeof(HANDLE));
        Timer myTimer;

        for (int j = 0; j < i; j++)
        {
            ahThread[j] = CreateThread(NULL,
                0,
                ThreadStart,
                this,
                0,
                0);
        }

        while (myTimer.elapsed() < _secondsPerTestPass) { Sleep(1000); }
        StopTestsNow();
        WaitForMultipleObjects(i, ahThread, TRUE, INFINITE);

        double totalSeconds = myTimer.elapsed();
        PrintfImpl("%d, %f, %d, %f, %f\n", i, totalSeconds, _successCount, ((double)_successCount) / totalSeconds, ((double)_successCount) / (totalSeconds * i));

        for (int j = 0; j < i; j++) {
            CloseHandle(ahThread[j]);
        }
        free(ahThread);
        ResetState();
    }
}
