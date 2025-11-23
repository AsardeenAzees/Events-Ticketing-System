@echo off
echo ========================================
echo StarEvents Ticketing System
echo ========================================
echo.

REM Stop any running instances
taskkill /F /IM dotnet.exe >nul 2>&1

echo Building application...
dotnet build --no-incremental >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    echo Please check the errors above.
    pause
    exit /b 1
)

echo.
echo ========================================
echo Application Starting...
echo ========================================
echo.
echo The application will be available at:
echo   - HTTPS: https://localhost:5001
echo   - HTTP:  http://localhost:5000
echo.
echo Default Admin Login:
echo   Email: admin@starevents.com
echo   Password: Admin@123
echo.
echo Press Ctrl+C to stop the application
echo.
echo ========================================
echo.

dotnet run

pause

