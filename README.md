## Setup env
1.
```bash
conda create --name myenv python=3.11
```

2.
```bash
pip install -r auxiliary_services/requirements.txt
```

3.
```bash
winget install espeak-ng
```

## Setup VSCode

1. Save `settings.json` with:
```json
{
    // ... other settings ...
    "code-runner.executorMap": {
        "python": "powershell -ExecutionPolicy Bypass -Command \"cd \\\"$dirWithoutTrailingSlash\\\"; $filename=\\\"$fileNameWithoutExt\\\"; $timestamp=Get-Date -Format yyyyMMdd_HHmmss; New-Item -ItemType Directory -Force -Path ../on_run; Copy-Item \\\"$fileName\\\" -Destination \\\"../on_run/${timestamp}_${filename}_on_run.py\\\"; python \\\"$fileName\\\" 2>&1 | Tee-Object -FilePath \\\"../on_run/${timestamp}_${filename}_on_run.log\\\"\""
    },
    // ... other settings ...
}
```

**Note**: This configuration assumes you're running code from within a user directory (e.g., `data/1/`) and will save outputs to the `data/on_run/` folder. The outputs will include both the code file and execution log with timestamps.

## For Study Administrators

### 1. First time setup:
```bash
conda activate myenv
make build_klog
make setup-data-dir      # Creates the data directory structure
make shremdup
# For AI tutor
make tts
make whisper
make espeak
```

### 2. For each user session:

For user N (replace N with user number)

```bash
make setup-user USER=N
```

```bash
make new-session USER=N

# Monitor active sessions
make list-users

# Stop a user's session
make stop-session USER=N

# Delete a user's data
make clean-user USER=N
```

## User perspective

1. Your administrator will set up your session with a unique number N. You will work in the `data/N/` directory:
   - Your main code file is: `data/N/main_N.py`
   - Run your code using VSCode's run button (or Ctrl+Alt+N with Code Runner)
   - All your code runs will be automatically saved in `data/on_run/`
   - Do not modify any files outside your assigned directory

2. When you're done:
   - Save your work
   - Let your administrator know to stop your session

## Important Notes

- **DO NOT** manually stop or start the monitoring tools
- **ALWAYS** run your code through VSCode to ensure proper logging
- If you encounter any issues, contact your study administrator
