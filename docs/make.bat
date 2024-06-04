@ECHO OFF
pushd %~dp0

REM Command file for Sphinx documentation

if "%SPHINXBUILD%" == "" (
	set SPHINXBUILD=sphinx-build
)
set SOURCEDIR=.
set BUILDDIR=_build

%SPHINXBUILD% >NUL 2>NUL
if errorlevel 9009 (
	echo The 'sphinx-build' command was not found. Make sure you have Sphinx installed, then set the SPHINXBUILD environment variable to point to the full path of the 'sphinx-build' executable.
	echo Alternatively you may add the Sphinx directory to PATH.
	echo If you don't have Sphinx installed, grab it from https://www.sphinx-doc.org/
	exit /b 1
)

set command="%1" &:: html, clean and etc.
call :dequote %command%
echo Doing %ret% ...

IF %command% == "" (
   set status="FAILED"
   echo There is no build command! Available commands: clean, html
   echo See Sphinx Help below.
   %SPHINXBUILD% -M help %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%
) ELSE (
   %SPHINXBUILD% -M %command% %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%
   set status="DONE"
)
call :dequote %status%
echo Build %ret%

popd

:dequote
setlocal
set thestring=%~1
endlocal&set ret=%thestring%
goto :eof
