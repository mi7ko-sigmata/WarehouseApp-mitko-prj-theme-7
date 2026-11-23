@echo off
chcp 65001 >nul
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
echo.
echo Готово! Изпълнимият файл е в папка "publish".
pause
