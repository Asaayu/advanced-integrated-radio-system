@echo off
msbuild extension/AdvancedIntegratedRadioSystem.sln /p:Configuration=Release /p:Platform=x64
msbuild extension/AdvancedIntegratedRadioSystem.sln /p:Configuration=Release /p:Platform=x86
hemtt build
pause
