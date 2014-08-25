IF "%IsEmulated%" == "false" (
REM Powershell.exe -ExecutionPolicy Unrestricted -NoProfile -Command "& {%~dp0Import-DscModules.ps1}" < NUL  >> "%~dp0output.txt" 2>> "%~dp0error.txt"
Powershell.exe -ExecutionPolicy Unrestricted -NoProfile -Command "& {%~dp0New-DscPullServer.ps1 -SubjectName '%SubjectName%'}" < NUL  >> "%~dp0output.txt" 2>> "%~dp0error.txt"
echo "done" >"%ROLEROOT%\startup.task.done.sem"
)
exit 0