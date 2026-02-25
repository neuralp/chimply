@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM --- Usage: sshurl.bat "ssh://user:password@10.1.10.106/"
set "URL=%~1"
if not defined URL (
  echo Usage: %~nx0 "ssh://user:password@host[:port]/"
  exit /b 1
)

REM Strip surrounding quotes are already handled by %~1

REM Basic validation / strip scheme
if /I not "%URL:~0,6%"=="ssh://" (
  echo Error: URL must start with ssh://
  exit /b 1
)
set "REST=%URL:~6%"

REM Remove any path after host (everything after first /)
for /f "delims=/ tokens=1" %%A in ("%REST%") do set "AUTHHOST=%%A"

REM Split at @ : [userinfo@]host[:port]
set "USERINFO="
set "HOSTPORT=%AUTHHOST%"
for /f "tokens=1,2 delims=@" %%A in ("%AUTHHOST%") do (
  set "LEFT=%%A"
  set "RIGHT=%%B"
)
if defined RIGHT (
  set "USERINFO=%LEFT%"
  set "HOSTPORT=%RIGHT%"
)

REM From userinfo, extract user (ignore password for OpenSSH)
set "USER="
if defined USERINFO (
  for /f "tokens=1 delims=:" %%A in ("%USERINFO%") do set "USER=%%A"
)

REM If username wasn't provided in the URL, prompt for it now
if not defined USER (
  set /p "USER=Enter username: "
  if not defined USER (
    echo Error: Username required.
    exit /b 1
  )
)

REM Split host and port if present
set "HOST=%HOSTPORT%"
set "PORT="
for /f "tokens=1,2 delims=:" %%A in ("%HOSTPORT%") do (
  set "HOST=%%A"
  set "PORT=%%B"
)

REM Build ssh command
set "TARGET=%HOST%"
if defined USER set "TARGET=%USER%@%HOST%"

set "CMD=ssh"
if defined PORT set "CMD=%CMD% -p %PORT%"
set "CMD=%CMD% %TARGET%"

echo Running: %CMD%
REM Execute in same terminal
%CMD%

endlocal
``