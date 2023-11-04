cmake_minimum_required(VERSION 3.26)
project(MacTweaksAXUIStub C)

set(CMAKE_C_STANDARD 11)

add_library(MacTweaksAXUIStub SHARED
        main.m
        main.h
        brightness.h
        brightness.c)

set_target_properties(MacTweaksAXUIStub PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR} OUTPUT_NAME MacTweaksAXUIStub)

target_link_libraries(MacTweaksAXUIStub "-framework Foundation")
target_link_libraries(MacTweaksAXUIStub "-framework ApplicationServices")
target_link_libraries(MacTweaksAXUIStub "-framework ScriptingBridge")
target_link_libraries(MacTweaksAXUIStub "-framework IOKit")
target_link_libraries(MacTweaksAXUIStub "-framework CoreDisplay")
target_link_libraries(MacTweaksAXUIStub "-F /System/Library/PrivateFrameworks")
target_link_libraries(MacTweaksAXUIStub "-framework DisplayServices")