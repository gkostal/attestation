# Copyright (c) Open Enclave SDK contributors.
# Licensed under the MIT License.

add_executable(enclave ecalls.cpp)

if (WIN32)
  maybe_build_using_clangw(enclave)
endif ()

target_include_directories(enclave PRIVATE ${CMAKE_CURRENT_BINARY_DIR})

target_link_libraries(enclave common)

# Generate key
add_custom_command(
  OUTPUT private.pem public.pem
  COMMAND openssl genrsa -out private.pem -3 3072
  COMMAND openssl rsa -in private.pem -pubout -out public.pem)

add_custom_target(public_key DEPENDS public.pem)

# Sign enclave with key
add_custom_command(
  OUTPUT enclave.signed
  DEPENDS enclave enc.conf private.pem
  COMMAND openenclave::oesign sign -e $<TARGET_FILE:enclave> -c
          ${CMAKE_CURRENT_SOURCE_DIR}/enc.conf -k private.pem)

add_custom_target(enclave_signed ALL DEPENDS enclave.signed)
