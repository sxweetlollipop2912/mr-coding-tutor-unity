#!/usr/bin/env python3

# Usage: python3 periodic_log.py --target <path_to_file> --sec <interval_in_seconds> --outdir <output_directory>

import argparse
import os
import time
import shutil
from datetime import datetime


def parse_args():
    parser = argparse.ArgumentParser(
        description="Periodically save a file with timestamps"
    )
    parser.add_argument(
        "--target", required=True, help="Path to target file (absolute or relative)"
    )
    parser.add_argument("--sec", type=float, required=True, help="Interval in seconds")
    parser.add_argument(
        "--outdir", required=True, help="Output directory (absolute or relative)"
    )
    return parser.parse_args()


def main():
    args = parse_args()

    # Convert paths to absolute paths
    target_path = os.path.abspath(args.target)
    outdir_path = os.path.abspath(args.outdir)

    # Ensure target file exists
    if not os.path.isfile(target_path):
        print(f"Error: Target file '{args.target}' does not exist.")
        return

    # Ensure output directory exists, create if needed
    if not os.path.exists(outdir_path):
        os.makedirs(outdir_path)
        print(f"Created output directory: {outdir_path}")

    # Get original filename without path
    original_filename = os.path.basename(target_path)
    name, ext = os.path.splitext(original_filename)

    print(f"Starting periodic logging of '{target_path}'")
    print(f"Interval: {args.sec} seconds")
    print(f"Output directory: {outdir_path}")

    previous_content = None

    while True:
        try:
            # Try to read the file in binary mode to handle any content type
            try:
                with open(target_path, "rb") as f:
                    current_content = f.read()
            except Exception as e:
                print(f"Error reading file '{target_path}': {e}")
                time.sleep(args.sec)
                continue

            # Only save if content has changed
            if current_content != previous_content:
                try:
                    # Generate timestamp
                    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

                    # Create output filename with timestamp
                    output_filename = f"{timestamp}_{name}{ext}"
                    output_path = os.path.join(outdir_path, output_filename)

                    # Copy the target file to the output directory with the new name
                    shutil.copy2(target_path, output_path)
                    print(f"Content changed, saved copy to: {output_path}")

                    # Update previous content only if copy was successful
                    previous_content = current_content
                except Exception as e:
                    print(f"Error saving file copy: {e}")
            else:
                print(f"No changes detected in '{target_path}', skipping save")

        except KeyboardInterrupt:
            print("\nLogging stopped by user.")
            break
        except Exception as e:
            print(f"Unexpected error: {e}")

        # Wait for the specified interval
        try:
            time.sleep(args.sec)
        except KeyboardInterrupt:
            print("\nLogging stopped by user.")
            break


if __name__ == "__main__":
    main()
