#include <openenclave/host.h>
#include "mbedTlsTestEnclave_u.h"

oe_enclave_t* enclave = NULL;
int enclaveReferenceCount = 0;

oe_result_t create_mbedTlsTestEnclave_enclave(const char* enclave_name, oe_enclave_t** out_enclave);

void create_enclave() {
	if (enclave == NULL) {
		create_mbedTlsTestEnclave_enclave("mbedTlsTestEnclave.elf.signed", &enclave);
	}
	enclaveReferenceCount++;
}

void terminate_enclave() {
	enclaveReferenceCount--;
	if ((enclaveReferenceCount == 0) && (enclave != NULL))
	{
		oe_terminate_enclave(enclave);
		enclave = NULL;
	}
}

int call_enclave_startTesting() {
	oe_result_t result = OE_OK;
	int successCount = 0;

	result = ECALL_mbedTlsTestMethod(enclave, &successCount);
	if (result != OE_OK) {
		printf("call_enclave_startTesting failed enclave call!  result=%u (%s)\n", result, oe_result_str(result));
	}

	return successCount;
}

int call_enclave_stopTesting() {
	oe_result_t result = OE_OK;
	int resultValue = 0;

	result = ECALL_stopTesting(enclave, &resultValue);
	if (result != OE_OK) {
		printf("call_enclave_stopTesting failed enclave call!  result=%u (%s)\n", result, oe_result_str(result));
	}

	return resultValue;
}

oe_result_t create_mbedTlsTestEnclave_enclave(
	const char* enclave_name,
	oe_enclave_t** out_enclave)
{
	oe_enclave_t* enclave = NULL;
	uint32_t enclave_flags = 0;
	oe_result_t result;

	*out_enclave = NULL;

	// Create the enclave
#ifdef _DEBUG
	enclave_flags |= OE_ENCLAVE_FLAG_DEBUG;
#endif
	result = oe_create_mbedTlsTestEnclave_enclave(
		enclave_name, OE_ENCLAVE_TYPE_AUTO, enclave_flags, NULL, 0, &enclave);
	if (result != OE_OK)
	{
		printf(
			"Error %d creating enclave, trying simulation mode...\n", result);
		enclave_flags |= OE_ENCLAVE_FLAG_SIMULATE;
		result = oe_create_mbedTlsTestEnclave_enclave(
			enclave_name,
			OE_ENCLAVE_TYPE_AUTO,
			enclave_flags,
			NULL,
			0,
			&enclave);
	}
	if (result != OE_OK)
	{
		return result;
	}

	*out_enclave = enclave;
	return OE_OK;
}

