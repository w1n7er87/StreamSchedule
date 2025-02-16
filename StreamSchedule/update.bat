@echo off
cd /d "%~dp0"
git fetch
git status
git merge origin/main
git add .
git commit -m "backup"
git push
git pull
start "" "%~dp0StreamSchedule.exe"
exit