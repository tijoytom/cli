@Echo OFF
SETLOCAL
SET ERRORLEVEL=

dnu restore %*

exit /b %ERRORLEVEL%
ENDLOCAL