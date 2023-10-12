# https://stackoverflow.com/questions/3349105/how-can-i-set-the-current-working-directory-to-the-directory-of-the-script-in-ba
cd "${0%/*}"

clang -g -dynamiclib -framework Foundation -framework ApplicationServices main.m -o MacTweaksAXUIStub.dylib
