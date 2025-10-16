call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" amd64
cl /LD /FeHapticSoftware.dll HapticSoftware.cpp vendor/seriallib/serialib.cpp /I "vendor/serialib"
