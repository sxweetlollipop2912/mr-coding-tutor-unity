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

1.
```bash
conda activate myenv
```



Command for running the periodic log
python periodic_log.py --target /Users/sang.pham/.leetcode/416.partition-equal-subset-sum.py --sec 1 --outdir /Users/sang.pham/.leetcode/output_dir
