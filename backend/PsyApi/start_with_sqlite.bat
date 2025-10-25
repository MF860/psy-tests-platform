@echo off
cd /d "c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi"
set USE_SQLITE=1
set SQLITE_PATH=psy_dev.db
echo Starting backend with SQLite database...
echo USE_SQLITE=%USE_SQLITE%
echo SQLITE_PATH=%SQLITE_PATH%
dotnet run