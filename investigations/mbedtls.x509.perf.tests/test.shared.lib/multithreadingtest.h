#pragma once
#include <string>
#include <iostream>
#include <fstream>
#include <windows.h>
#include "timer.h"

class MultiThreadingTest
{
public:
	MultiThreadingTest(std::string testType, int minThreads, int maxThreads, int secondsPerTestPass);

	/// <summary>
	/// Start the uber test run
	/// </summary>
	void RunAllTestsNow();

	/// <summary>
	/// Pure virtual that must be implemented by a derived class.
	/// This method should:
	///   * run until ShouldTestsStop returns true or StopTestsNow is called
	///   * call IncrementSuccessCount before returning
	///   * call IncrementTotalCount before returning
	/// </summary>
	virtual void RunTestOnThisThread() = 0;

	/// <summary>
	/// Virtual method that can be overridden and will be 
	/// callwed when it's time to stop running tests.
	/// </summary>
	virtual void StopTestsNow();

	/// <summary>
	/// Helper methods for RunTestOnThisThread to call
	/// </summary>
	bool ShouldTestsStop();
	void IncrementSuccessCount(long amount);
	void IncrementTotalCount(long amount);

	virtual void PrintfImpl(const char * const format, ...);

private:
	static DWORD WINAPI ThreadStart(LPVOID lpParam);
	void ResetState();
	void PrintfCsvImpl(const char* const format, ...);

	int _minThreads;
	int _maxThreads;
	int _secondsPerTestPass;
	std::string _testType;
	std::string _buildType;
	std::string _csvFileName;
	bool _stopTestNow;
	long _successCount;
	long _totalCount;
};

