# https://stackoverflow.com/questions/3349105/how-can-i-set-the-current-working-directory-to-the-directory-of-the-script-in-ba
cd "${0%/*}"

clang -c brightness.c -o brightness.o
clang -w -dynamiclib -install_name “$(pwd)/MacTweaksAXUIStub.dylib” -framework Foundation -framework ApplicationServices -framework IOKit -framework CoreDisplay -F /System/Library/PrivateFrameworks -framework DisplayServices main.m brightness.o -o MacTweaksAXUIStub.dylib
rm brightness.o
