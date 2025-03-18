#!/usr/bin/env python3
import requests
import time
import os
import platform
import sys
import datetime
import json
import re

# Try to import audio analysis libraries but continue if they're not available
try:
    import mutagen
    from mutagen.mp3 import MP3

    AUDIO_ANALYSIS_AVAILABLE = True
except ImportError:
    AUDIO_ANALYSIS_AVAILABLE = False

# Configuration
TTS_SERVER_URL = "http://localhost:5112/tts"

# Voice settings (can be customized)
VOICE = "en-US-ChristopherNeural"  # Default male voice
RATE = "+25%"  # Faster speed

# Fixed English text with approximately 50 tokens
BENCHMARK_TEXT = """
Artificial intelligence is transforming how we interact with technology. Voice assistants use AI algorithms to understand user preferences. These systems improve as more data becomes available.
"""


def get_file_size_str(size_in_bytes):
    """Convert file size in bytes to human-readable format"""
    for unit in ["B", "KB", "MB"]:
        if size_in_bytes < 1024 or unit == "MB":
            return f"{size_in_bytes:.2f} {unit}"
        size_in_bytes /= 1024


def estimate_tokens(text):
    """Roughly estimate the number of tokens in text (based on GPT tokenization)"""
    # This is a very rough estimation - 1 token is ~4 chars in English
    return len(text) // 4


def run_benchmark():
    """Run the TTS benchmark and report results"""
    print("\n=== TTS Server Benchmark ===")
    print(f"Time: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"System: {platform.system()} {platform.release()}")
    print(f"Python: {platform.python_version()}")

    if not AUDIO_ANALYSIS_AVAILABLE:
        print("Note: Install mutagen package for detailed audio analysis")

    # Check if TTS server is running
    try:
        health_check = requests.get("http://localhost:5112/health", timeout=5)
        if health_check.status_code != 200:
            print("Error: TTS server is not responding properly")
            return
        print(f"TTS Server: {health_check.json()['engine']}")
        print(f"Default Voice: {health_check.json()['default_voice']}")
        print(f"Default Rate: {health_check.json()['default_rate']}")
        print(f"Using Voice: {VOICE}")
        print(f"Using Rate: {RATE}")
    except requests.exceptions.RequestException:
        print("Error: TTS server is not running. Please start the server first.")
        return
    except KeyError as e:
        # Handle missing keys in the health check response
        print(f"Warning: Could not read key from health check: {e}")
        print(f"Using Voice: {VOICE}")
        print(f"Using Rate: {RATE}")
        # Continue with the benchmark even if the health check format is unexpected

    # Text statistics
    char_count = len(BENCHMARK_TEXT)
    word_count = len(BENCHMARK_TEXT.split())
    token_estimate = estimate_tokens(BENCHMARK_TEXT)
    print(f"\nBenchmark Text:")
    print(f"Characters: {char_count}")
    print(f"Words: {word_count}")
    print(f"Estimated Tokens: {token_estimate}")

    # Run benchmark
    print("\nRunning benchmark...")

    try:
        # Measure request time
        start_time = time.time()
        response = requests.post(
            TTS_SERVER_URL,
            json={"text": BENCHMARK_TEXT, "voice": VOICE, "rate": RATE},
            timeout=60,  # Allow up to 60 seconds for response
        )
        end_time = time.time()

        # Process results
        if response.status_code == 200:
            # Calculate time metrics
            processing_time = end_time - start_time
            chars_per_second = char_count / processing_time
            words_per_second = word_count / processing_time

            # Save audio file for analysis
            temp_file = "tts_benchmark_output.mp3"
            with open(temp_file, "wb") as f:
                f.write(response.content)

            # Get file size
            file_size = os.path.getsize(temp_file)
            file_size_str = get_file_size_str(file_size)

            # Get audio duration if possible
            audio_duration = None
            if AUDIO_ANALYSIS_AVAILABLE:
                try:
                    audio = MP3(temp_file)
                    audio_duration = audio.info.length
                except Exception as e:
                    print(f"Warning: Could not analyze audio duration: {e}")

            # Print results
            print("\n=== Results ===")
            print(f"Status: Success")
            print(f"Voice: {VOICE}")
            print(f"Rate: {RATE}")
            print(f"Processing Time: {processing_time:.2f} seconds")
            print(f"Characters Per Second: {chars_per_second:.2f}")
            print(f"Words Per Second: {words_per_second:.2f}")
            if audio_duration:
                print(f"Audio Duration: {audio_duration:.2f} seconds")
                print(f"Characters Per Audio Second: {char_count / audio_duration:.2f}")
                print(f"Words Per Audio Second: {word_count / audio_duration:.2f}")
                print(f"Real-time Factor: {audio_duration / processing_time:.2f}x")
            print(f"Output File Size: {file_size_str}")
            print(f"Output File: {os.path.abspath(temp_file)}")

            print("\nBenchmark completed successfully!")
        else:
            print(f"Error: Server returned status code {response.status_code}")
            print(response.text)
    except requests.exceptions.RequestException as e:
        print(f"Error during benchmark: {e}")


if __name__ == "__main__":
    # Check if voice parameter is provided
    if len(sys.argv) > 1:
        VOICE = sys.argv[1]

    # Check if rate parameter is provided
    if len(sys.argv) > 2:
        RATE = sys.argv[2]

    run_benchmark()
