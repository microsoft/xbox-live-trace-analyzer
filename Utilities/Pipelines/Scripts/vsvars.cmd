echo "Initializing Visual Studio environment..."
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvarsamd64_x86.bat" > nul

echo "Setting environment variables..."
set PATH=%PATH%;C:\My\New\Path
set MY_VAR=my_value

echo "Done."
