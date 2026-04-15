@echo off
setlocal enabledelayedexpansion

REM Configuration
set FRAMEWORK=net10.0
set CONFIG=Debug
set BACKEND_DIR=Aether.Backend
set PLUGINS_OUT=%BACKEND_DIR%\bin\%CONFIG%\%FRAMEWORK%\plugins

echo 🚀 Starting Aether Build Script...

REM Function to clean and build project
goto :main

:build_project
    set PROJECT_DIR=%~1
    echo 📦 Building %PROJECT_DIR%...
    if exist "%PROJECT_DIR%\bin" rd /s /q "%PROJECT_DIR%\bin"
    if exist "%PROJECT_DIR%\obj" rd /s /q "%PROJECT_DIR%\obj"
    dotnet build "%PROJECT_DIR%" -c "%CONFIG%" -f "%FRAMEWORK%"
    if errorlevel 1 (
        echo ❌ Build failed for %PROJECT_DIR%
        exit /b 1
    )
    exit /b 0

:copy_plugin
    set SRC_PROJECT=%~1
    set DLL_NAME=%~2
    echo ------------- Processing %DLL_NAME% -------------
    
    set SRC_DIR=Plugins\Official\%SRC_PROJECT%\bin\Debug\%FRAMEWORK%
    
    if exist "%SRC_DIR%" (
        REM Copy all DLLs
        copy /Y "%SRC_DIR%\*.dll" "%PLUGIN_DIR%\" >nul
        
        REM Remove shared assemblies
        if exist "%PLUGIN_DIR%\Aether.PluginSDK.dll" del /q "%PLUGIN_DIR%\Aether.PluginSDK.dll"
        if exist "%PLUGIN_DIR%\Serilog.dll" del /q "%PLUGIN_DIR%\Serilog.dll"
        if exist "%PLUGIN_DIR%\Serilog.Sinks.File.dll" del /q "%PLUGIN_DIR%\Serilog.Sinks.File.dll"
        if exist "%PLUGIN_DIR%\Google.Protobuf.dll" del /q "%PLUGIN_DIR%\Google.Protobuf.dll"
        
        echo ✅ Deployed %DLL_NAME% and dependencies to plugins folder
    ) else (
        echo ❌ Error: Could not find directory %SRC_DIR%
        exit /b 1
    )
    exit /b 0

:main

REM 1. Clean and Build SDK
echo ------------- Building PluginSDK -------------
call :build_project "Aether.PluginSDK"
if errorlevel 1 exit /b 1

REM 2. Build Backend
echo ------------- Building Backend -------------
call :build_project "Aether.Backend"
if errorlevel 1 exit /b 1

REM 3. Create Plugins Directory
echo ------------- Preparing Plugins -------------
if not exist "%PLUGINS_OUT%" mkdir "%PLUGINS_OUT%"
echo 📂 Plugins directory: %PLUGINS_OUT%

REM 4. Build Importers
call :build_project "Plugins\Official\Aether.Importers.Steam\Aether.Importers.Steam.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.Gog\Aether.Importers.Gog.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.Epic\Aether.Importers.Epic.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.AppStore\Aether.Importers.AppStore.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.Custom\Aether.Importers.Custom.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.IGDB\Aether.Importers.IGDB.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.Crossover\Aether.Importers.Crossover.csproj"
if errorlevel 1 exit /b 1
call :build_project "Plugins\Official\Aether.Importers.Web\Aether.Importers.Web.csproj"
if errorlevel 1 exit /b 1

REM Copy Plugins to Backend Output
echo ------------- Preparing Plugins -------------
set PLUGIN_DIR=Aether.Backend\bin\Debug\%FRAMEWORK%\plugins
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"
echo 📂 Plugins directory: %PLUGIN_DIR%

call :copy_plugin "Aether.Importers.Steam" "Aether.Importers.Steam"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.Gog" "Aether.Importers.Gog"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.Epic" "Aether.Importers.Epic"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.AppStore" "Aether.Importers.AppStore"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.Custom" "Aether.Importers.Custom"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.IGDB" "Aether.Importers.IGDB"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.Crossover" "Aether.Importers.Crossover"
if errorlevel 1 exit /b 1
call :copy_plugin "Aether.Importers.Web" "Aether.Importers.Web"
if errorlevel 1 exit /b 1

REM Check for bundle argument
set SHOULD_BUNDLE=false
set VERSION=1.0.0

:parse_args
if "%~1"=="" goto :after_args
if /i "%~1"=="--bundle" (
    set SHOULD_BUNDLE=true
    shift
    goto :parse_args
)
if /i "%~1"=="--version" (
    set VERSION=%~2
    shift
    shift
    goto :parse_args
)
echo Unknown parameter passed: %~1
exit /b 1

:after_args

if "%SHOULD_BUNDLE%"=="true" (
    echo ═══════════════════════════════════════════════════════════════════════════
    echo 📦 Bundling Backend for Windows Distribution (Version: %VERSION%^)
    echo ═══════════════════════════════════════════════════════════════════════════
    
    set PUBLISH_DIR=.\publish\win-x64
    echo ------------- Publishing Backend -------------
    if exist "%PUBLISH_DIR%" rd /s /q "%PUBLISH_DIR%"
    
    REM Publish as self-contained executable
    dotnet publish Aether.Backend -c Release -r win-x64 ^
        --self-contained true ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -o "%PUBLISH_DIR%"
    
    if errorlevel 1 (
        echo ❌ Publish failed
        exit /b 1
    )
    
    REM Standardize executable name
    if exist "%PUBLISH_DIR%\Aether.Backend.exe" (
        move /Y "%PUBLISH_DIR%\Aether.Backend.exe" "%PUBLISH_DIR%\AetherBackend.exe" >nul
    )
    
    REM Copy plugins to publish folder
    echo 📦 Copying plugins to publish folder...
    if not exist "%PUBLISH_DIR%\plugins" mkdir "%PUBLISH_DIR%\plugins"
    copy /Y "%PLUGIN_DIR%\*.dll" "%PUBLISH_DIR%\plugins\" >nul
    
    echo ✅ Backend published to %PUBLISH_DIR%
    echo.
    echo ══════════════════════════════════════════════════════════════════════════════
    echo ⚠️  IMPORTANT: Distribution Notes
    echo ══════════════════════════════════════════════════════════════════════════════
    echo 1. The backend executable is at: %PUBLISH_DIR%\AetherBackend.exe
    echo 2. Plugin DLLs are in: %PUBLISH_DIR%\plugins\
    echo 3. Ensure your WinUI app can locate these files at runtime
    echo ══════════════════════════════════════════════════════════════════════════════
) else (
    echo ℹ️  Skipping bundling (use --bundle to enable^)
)

echo ----------------------------------------------
echo 🎉 Build Complete!
if "%SHOULD_BUNDLE%"=="true" (
    echo 👉 Backend published to %PUBLISH_DIR%
) else (
    echo 👉 You can now run the backend:
    echo    cd Aether.Backend ^&^& dotnet run
)

endlocal
