@echo off
setlocal
cd /d "%~dp0"
python -m pip install -r "%~dp0requirements.txt"
echo.
pause
