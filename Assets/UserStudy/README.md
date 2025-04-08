
Command for running the periodic log
python periodic_log.py --target /Users/sang.pham/.leetcode/416.partition-equal-subset-sum.py --sec 1 --outdir /Users/sang.pham/.leetcode/output_dir

Command for compilation in settings.json of vscode Code Runner
"python": "cd $dirWithoutTrailingSlash; filename=\"$fileNameWithoutExt\"; timestamp=$(date +%Y%m%d_%H%M%S) && mkdir -p output_dir && cp \"$fileName\" \"output_dir/${timestamp}_${filename}_on_save.py\" && python3 \"$fileName\" 2>&1 | tee \"output_dir/${timestamp}_${filename}_output.log\""
