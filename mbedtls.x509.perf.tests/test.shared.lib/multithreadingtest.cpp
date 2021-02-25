#include "multithreadingtest.h"

MultiThreadingTest::MultiThreadingTest(std::string testType, int minThreads, int maxThreads, int secondsPerTestPass)
	: 
    _testType(testType), 
    _minThreads(minThreads),
    _maxThreads(maxThreads), 
    _secondsPerTestPass(secondsPerTestPass), 
    _stopTestNow(false), 
    _successCount(0), 
    _totalCount(0)
{
#ifdef _DEBUG
    _buildType = "Debug";
#else
    _buildType = "Release";
#endif
    _csvFileName = testType + "." + _buildType + ".csv";
    std::ofstream csvFile(_csvFileName, std::ios::out | std::ios::trunc);
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

void MultiThreadingTest::PrintfCsvImpl(const char* const format, ...)
{
    char lineBuffer[4096];

    va_list vl;
    va_start(vl, format);
    vsprintf_s(lineBuffer, format, vl);
    vprintf(format, vl);
    va_end(vl);

    std::ofstream csvFile(_csvFileName, std::ios::out | std::ios::app);
    csvFile << lineBuffer;
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
    PrintfImpl("%s - %s Test\n", _buildType.c_str(), _testType.c_str());

    PrintfCsvImpl("Thread Count, Total Time, Total Count, Total RPS, RPS Per Thread\n");

    for (int i = _minThreads; i <= _maxThreads; i++)
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
        PrintfCsvImpl("%d, %f, %d, %f, %f\n", i, totalSeconds, _successCount, ((double)_successCount) / totalSeconds, ((double)_successCount) / (totalSeconds * i));

        for (int j = 0; j < i; j++) {
            CloseHandle(ahThread[j]);
        }
        free(ahThread);
        ResetState();
    }
}
