@echo off

for /f "tokens=*" %%a in ('powershell -Command "Get-Date -Format 'yyyyMMdd_HHmm'"') do set TIMESTAMP=%%a

gh release create backup-%TIMESTAMP% ^
    "C:\StreamSchedule\Markov2.data" ^
    "C:\StreamSchedule\StreamSchedule.data" ^
    "C:\StreamSchedule\Utility.data" ^
    --title Backup-%TIMESTAMP% ^
    --notes "" ^
    --repo w1n7er87/net9.0 ^
    
gh api repos/w1n7er87/StreamSchedulePublish/zipball/main > "%TEMP%/app.zip" 

echo @echo off > "%TEMP%\final_updater.bat"
echo timeout /t 2 /nobreak > nul >> "%TEMP%\final_updater.bat"
echo tar -xf "%TEMP%\app.zip" -C "C:\StreamSchedule" --strip-components=1 >> "%TEMP%\final_updater.bat"
echo del "%TEMP%\app.zip" >> "%TEMP%\final_updater.bat"
echo start "" "C:\StreamSchedule\StreamSchedule.exe" >> "%TEMP%\final_updater.bat"
echo del "%tmp%\final_updater.bat" ^& exit >> "%TEMP%\final_updater.bat"

start "" "%TEMP%\final_updater.bat"
exit