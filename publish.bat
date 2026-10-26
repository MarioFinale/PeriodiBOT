@echo off
REM publish.bat - Build single-file self-contained executables for Windows, Linux and macOS
REM Equivalent of publish.sh for Windows cmd

setlocal enabledelayedexpansion

REM ================== CONFIGURATION ==================
set PROJECT=.\PeriodiBOT-IRC\PeriodiBOT-IRC.vbproj
set CONFIG=Release
set OUTPUT=.\publish

set RIDS=win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64
REM ==================================================

echo Cleaning old publish folder...
if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"

for %%R in (%RIDS%) do (
    echo ========================================
    echo Publishing for %%R ...
    echo ========================================

    dotnet publish "%PROJECT%" ^
        -c "%CONFIG%" ^
        -r "%%R" ^
        --self-contained true ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -o "%OUTPUT%\%%R" ^
        --nologo

    if errorlevel 1 (
        echo Failed to publish for %%R
        exit /b 1
    )

    echo %%R completed → %OUTPUT%\%%R\
)

echo.
echo All builds finished!
echo Output folder: %OUTPUT%\
dir "%OUTPUT%"

endlocal
