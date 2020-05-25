cmake_minimum_required(VERSION 3.11)

project("MAA Generate Quotes Example" LANGUAGES C CXX)

find_package(OpenEnclave CONFIG REQUIRED)

set(CMAKE_CXX_STANDARD 11)

add_subdirectory(common)
add_subdirectory(enclave)
add_subdirectory(host)

add_custom_target(sign ALL DEPENDS enclave_signed)

if ((NOT DEFINED ENV{OE_SIMULATION}) OR (NOT $ENV{OE_SIMULATION}))
  add_custom_target(
    run
    DEPENDS remote_attestation_host sign
    COMMAND
      remote_attestation_host ${CMAKE_BINARY_DIR}/enclave/enclave.signed)
endif ()
