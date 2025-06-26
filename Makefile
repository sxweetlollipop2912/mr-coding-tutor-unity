# Force Make to use Windows cmd.exe shell
SHELL := C:/Windows/System32/cmd.exe

# Set default goal to help instead of shremdup
.DEFAULT_GOAL := help

.PHONY: shremdup tts whisper setup-data-dir setup-user start-session stop-session clean-user list-users help

shremdup:
	auxiliary_services/core_servers/shremdup.exe 3030
tts:
	python auxiliary_services/core_servers/tts_server.py
whisper:
	python auxiliary_services/core_servers/whisper_server.py
espeak:
	espeak-ng.exe
build_klog:
	g++ -o auxiliary_services/monitoring/keylogger.exe auxiliary_services/monitoring/Keylogger/klog_main.cpp
klog:
	auxiliary_services/monitoring/keylogger.exe

# User Study Management Commands

# Setup base data directory structure
setup-data-dir:
	@echo Setting up data directory structure...
	@if not exist data mkdir data
	@if not exist data\on_run mkdir data\on_run
	@echo Data directory structure created successfully!

# Setup directory for a specific user
# Usage: make setup-user user_id=1
setup-user:
	@if "$(user_id)"=="" ( \
		echo Error: user_id parameter required. Usage: make setup-user user_id=^<number^> & exit /b 1 \
	)
	@if exist data\$(user_id) ( \
		echo Error: User $(user_id) already exists. Use 'make clean-user user_id=$(user_id)' first to remove existing data. & exit /b 1 \
	)
	@echo Setting up user $(user_id) directory...
	@if not exist data\$(user_id) mkdir data\$(user_id)
	@if not exist data\$(user_id)\period mkdir data\$(user_id)\period
	@if not exist data\$(user_id)\main_$(user_id).py ( \
		echo # User $(user_id) main code file > data\$(user_id)\main_$(user_id).py \
	)
	@echo User $(user_id) setup complete!
	@echo   - Directory: data\$(user_id)\
	@echo   - Code file: data\$(user_id)\main_$(user_id).py
	@echo   - Period logs: data\$(user_id)\period\

# Start monitoring session for a user
# Usage: make start-session user_id=1
start-session:
	@if "$(user_id)"=="" ( \
		echo Error: user_id parameter required. Usage: make start-session user_id=^<number^> & exit /b 1 \
	)
	@if not exist data\$(user_id) ( \
		echo Error: User $(user_id) not set up. Run 'make setup-user user_id=$(user_id)' first & exit /b 1 \
	)
	@echo Starting monitoring session for user $(user_id)...
	@echo Starting periodic logging...
	@start "periodic_log_$(user_id)" /b cmd /c "python auxiliary_services\monitoring\periodic_log.py --target %CD%\data\$(user_id)\main_$(user_id).py --sec 1 --outdir %CD%\data\$(user_id)\period > data\$(user_id)\period_log.out 2>&1"
	@timeout /t 2 /nobreak > nul
	@for /f "usebackq skip=1 tokens=2 delims=," %%i in (`tasklist /fi "windowtitle eq periodic_log_$(user_id)" /fo csv 2^>nul`) do @echo %%~i > data\$(user_id)\period_pid.txt
	@echo Starting keylogger...
	@start "keylogger_$(user_id)" /b cmd /c "auxiliary_services\monitoring\keylogger.exe > data\$(user_id)\keylog.out 2>&1"
	@timeout /t 2 /nobreak > nul
	@for /f "usebackq skip=1 tokens=2 delims=," %%i in (`tasklist /fi "windowtitle eq keylogger_$(user_id)" /fo csv 2^>nul`) do @echo %%~i > data\$(user_id)\keylog_pid.txt
	@echo Session started for user $(user_id)!
	@echo   - Periodic logging started (PID saved in data\$(user_id)\period_pid.txt)
	@echo   - Keylogger started (PID saved in data\$(user_id)\keylog_pid.txt)

# Stop monitoring session for a user
# Usage: make stop-session user_id=1
stop-session:
	@if "$(user_id)"=="" ( \
		echo Error: user_id parameter required. Usage: make stop-session user_id=^<number^> & exit /b 1 \
	)
	@echo Stopping monitoring session for user $(user_id)...
	@cmd /c if exist data\$(user_id)\period_pid.txt ( echo Stopping periodic logging... && for /f %%p in (data\$(user_id)\period_pid.txt) do taskkill /f /pid %%p /t >nul 2>&1 || echo No periodic logging process found && del data\$(user_id)\period_pid.txt >nul 2>&1 )
	@cmd /c if exist data\$(user_id)\keylog_pid.txt ( echo Stopping keylogger... && for /f %%p in (data\$(user_id)\keylog_pid.txt) do taskkill /f /pid %%p /t >nul 2>&1 || echo No keylogger process found && del data\$(user_id)\keylog_pid.txt >nul 2>&1 )
	@echo Session stopped for user $(user_id)!

# Clean up user data
# Usage: make clean-user user_id=1
clean-user:
	@if "$(user_id)"=="" ( \
		echo Error: user_id parameter required. Usage: make clean-user user_id=^<number^> & exit /b 1 \
	)
	@echo Cleaning up user $(user_id) data...
	@make stop-session user_id=$(user_id) >nul 2>&1 || echo No active session to stop
	@if exist data\$(user_id) rmdir /s /q data\$(user_id)
	@if exist data\on_run ( \
		echo Cleaning up user $(user_id) files in data\on_run\... && del data\on_run\*main_$(user_id)_on_run.* >nul 2>&1 || echo No files to clean \
	)
	@echo User $(user_id) data cleaned up!

# List all users
list-users:
	@echo Current users:
	@cmd /c if exist data ( for /d %%i in (data\*) do ( if not "%%~nxi"=="on_run" ( echo   - User %%~nxi && if exist data\%%~nxi\period_pid.txt ( echo     Status: ACTIVE SESSION ) else if exist data\%%~nxi\keylog_pid.txt ( echo     Status: ACTIVE SESSION ) else ( echo     Status: Inactive ) ) ) ) else ( echo   No data directory found. Run 'make setup-data-dir' first. )

# Complete setup for a new user study session
# Usage: make new-session user_id=1
new-session: setup-data-dir setup-user start-session
	@echo New session ready for user $(user_id)!

# Complete workflow example
help:
	@echo User Study Management Commands:
	@echo.
	@echo Setup (New Users Only):
	@echo   make setup-data-dir            - Create base data directory structure
	@echo   make setup-user user_id=^<n^>  - Setup directory for new user ^<n^> (fails if user exists)
	@echo   make new-session user_id=^<n^> - Complete setup + start session for new user ^<n^> (fails if user exists)
	@echo.
	@echo Session Management:
	@echo   make start-session user_id=^<n^> - Start monitoring for user ^<n^>
	@echo   make stop-session user_id=^<n^>  - Stop monitoring for user ^<n^>
	@echo.
	@echo Utilities:
	@echo   make list-users               - List all users and their status
	@echo.
	@echo Server Commands:
	@echo   make shremdup                 - Start screen duplication server
	@echo   make tts                      - Start text-to-speech server
	@echo   make whisper                  - Start speech recognition server
	@echo   make build_klog               - Build keylogger executable
	@echo.
	@echo Example workflow:
	@echo   make new-session user_id=1    - Setup and start session for user 1
	@echo   # User works on code in data\1\main_1.py and runs via VSCode
	@echo   make stop-session user_id=1   - Stop session for user 1
