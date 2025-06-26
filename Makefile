.PHONY: shremdup tts whisper pyenv setup-data-dir setup-user start-session stop-session clean-user list-users

shremdup:
	./auxiliary_services/core_servers/shremdup.exe 3030
tts:
	python auxiliary_services/core_servers/tts_server.py
whisper:
	python auxiliary_services/core_servers/whisper_server.py
espeak:
	espeak-ng.exe
period:
	python auxiliary_services/monitoring/periodic_log.py --target /Users/sang.pham/.leetcode/416.partition-equal-subset-sum.py --sec 1 --outdir /Users/sang.pham/.leetcode/output_dir
build_klog:
	g++ -o auxiliary_services/monitoring/keylogger.exe auxiliary_services/monitoring/Keylogger/klog_main.cpp
klog:
	./auxiliary_services/monitoring/keylogger.exe

# User Study Management Commands

# Setup base data directory structure
setup-data-dir:
	@echo "Setting up data directory structure..."
	@mkdir -p data/on_run
	@echo "Data directory structure created successfully!"

# Setup directory for a specific user
# Usage: make setup-user USER=1
setup-user:
	@if [ -z "$(USER)" ]; then \
		echo "Error: USER parameter required. Usage: make setup-user USER=<number>"; \
		exit 1; \
	fi
	@if [ -d "data/$(USER)" ]; then \
		echo "Error: User $(USER) already exists. Use 'make clean-user USER=$(USER)' first to remove existing data."; \
		exit 1; \
	fi
	@echo "Setting up user $(USER) directory..."
	@mkdir -p data/$(USER)/period
	@if [ ! -f data/$(USER)/main_$(USER).py ]; then \
		echo "# User $(USER) main code file" > data/$(USER)/main_$(USER).py; \
	fi
	@echo "User $(USER) setup complete!"
	@echo "  - Directory: data/$(USER)/"
	@echo "  - Code file: data/$(USER)/main_$(USER).py"
	@echo "  - Period logs: data/$(USER)/period/"

# Start monitoring session for a user
# Usage: make start-session USER=1
start-session:
	@if [ -z "$(USER)" ]; then \
		echo "Error: USER parameter required. Usage: make start-session USER=<number>"; \
		exit 1; \
	fi
	@if [ ! -d "data/$(USER)" ]; then \
		echo "Error: User $(USER) not set up. Run 'make setup-user USER=$(USER)' first"; \
		exit 1; \
	fi
	@echo "Starting monitoring session for user $(USER)..."
	@echo "Starting periodic logging..."
	@python auxiliary_services/monitoring/periodic_log.py \
		--target $$(pwd)/data/$(USER)/main_$(USER).py \
		--sec 1 \
		--outdir $$(pwd)/data/$(USER)/period > data/$(USER)/period_log.out 2>&1 & \
	echo $$! > data/$(USER)/period_pid.txt
	@echo "Starting keylogger..."
	@./auxiliary_services/monitoring/keylogger.exe > data/$(USER)/keylog.out 2>&1 & \
	echo $$! > data/$(USER)/keylog_pid.txt
	@sleep 1
	@echo "Session started for user $(USER)!"
	@echo "  - Periodic logging PID: $$(cat data/$(USER)/period_pid.txt)"
	@echo "  - Keylogger PID: $$(cat data/$(USER)/keylog_pid.txt)"

# Stop monitoring session for a user
# Usage: make stop-session USER=1
stop-session:
	@if [ -z "$(USER)" ]; then \
		echo "Error: USER parameter required. Usage: make stop-session USER=<number>"; \
		exit 1; \
	fi
	@echo "Stopping monitoring session for user $(USER)..."
	@if [ -f data/$(USER)/period_pid.txt ]; then \
		kill $$(cat data/$(USER)/period_pid.txt) 2>/dev/null || true; \
		rm data/$(USER)/period_pid.txt; \
		echo "  - Periodic logging stopped"; \
	fi
	@if [ -f data/$(USER)/keylog_pid.txt ]; then \
		kill $$(cat data/$(USER)/keylog_pid.txt) 2>/dev/null || true; \
		rm data/$(USER)/keylog_pid.txt; \
		echo "  - Keylogger stopped"; \
	fi
	@echo "Session stopped for user $(USER)!"

# Clean up user data
# Usage: make clean-user USER=1
clean-user:
	@if [ -z "$(USER)" ]; then \
		echo "Error: USER parameter required. Usage: make clean-user USER=<number>"; \
		exit 1; \
	fi
	@echo "Cleaning up user $(USER) data..."
	@make stop-session USER=$(USER) 2>/dev/null || true
	@rm -rf data/$(USER)
	@if [ -d "data/on_run" ]; then \
		echo "Cleaning up user $(USER) files in data/on_run/..."; \
		rm -f data/on_run/*main_$(USER)_on_run.*; \
	fi
	@echo "User $(USER) data cleaned up!"

# List all users
list-users:
	@echo "Current users:"
	@if [ -d "data" ]; then \
		for dir in data/*/; do \
			if [ -d "$$dir" ] && [ "$$(basename $$dir)" != "on_run" ]; then \
				user=$$(basename $$dir); \
				echo "  - User $$user"; \
				if [ -f "data/$$user/period_pid.txt" ] || [ -f "data/$$user/keylog_pid.txt" ]; then \
					echo "    Status: ACTIVE SESSION"; \
				else \
					echo "    Status: Inactive"; \
				fi \
			fi \
		done \
	else \
		echo "  No data directory found. Run 'make setup-data-dir' first."; \
	fi

# Complete setup for a new user study session
# Usage: make new-session USER=1
new-session: setup-data-dir setup-user start-session
	@echo "New session ready for user $(USER)!"

# Complete workflow example
help:
	@echo "User Study Management Commands:"
	@echo ""
	@echo "Setup (New Users Only):"
	@echo "  make setup-data-dir          - Create base data directory structure"
	@echo "  make setup-user USER=<n>     - Setup directory for new user <n> (fails if user exists)"
	@echo "  make new-session USER=<n>    - Complete setup + start session for new user <n> (fails if user exists)"
	@echo ""
	@echo "Session Management:"
	@echo "  make start-session USER=<n>  - Start monitoring for user <n>"
	@echo "  make stop-session USER=<n>   - Stop monitoring for user <n>"
	@echo ""
	@echo "Utilities:"
	@echo "  make list-users              - List all users and their status"
	@echo ""
	@echo "Server Commands:"
	@echo "  make shremdup                - Start screen duplication server"
	@echo "  make tts                     - Start text-to-speech server"
	@echo "  make whisper                 - Start speech recognition server"
	@echo "  make build_klog              - Build keylogger executable"
	@echo ""
	@echo "Example workflow:"
	@echo "  make new-session USER=1      - Setup and start session for user 1"
	@echo "  # User works on code in data/1/main_1.py and runs via VSCode"
	@echo "  make stop-session USER=1     - Stop session for user 1"
