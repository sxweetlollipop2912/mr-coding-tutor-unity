# AI Server Components

This directory contains the Python server components for speech-to-text and text-to-speech functionalities used by the Unity application.

## Components

1. **Whisper Speech-to-Text Server (`whisper_server.py`)**
   - Runs on port 5111
   - Uses OpenAI's Whisper model (tiny) for fast speech recognition
   - Accepts audio via HTTP POST and returns transcribed text

2. **Edge TTS Text-to-Speech Server (`tts_server.py`)**
   - Runs on port 5112
   - Uses Microsoft Edge TTS for fast voice synthesis
   - Default voice: en-US-ChristopherNeural with +25% speed

## Setup

1. Install required packages:
   ```bash
   pip install -r requirements.txt
   ```

   Or if you prefer to install packages individually:
   ```bash
   pip install flask whisper torch edge-tts
   ```

## Running the Servers

To run the servers directly:

- **Start Whisper server:**
  ```bash
  python whisper_server.py
  ```

- **Start TTS server:**
  ```bash
  python tts_server.py
  ```

You can use these commands in separate terminal windows, or run them in the background:
  ```bash
  python whisper_server.py > whisper_server.log 2>&1 &
  python tts_server.py > tts_server.log 2>&1 &
  ```

To stop background servers:
  ```bash
  pkill -f "python whisper_server.py"
  pkill -f "python tts_server.py"
  ```

## Troubleshooting

- Check the log output for errors
- Ensure both ports (5111 and 5112) are available
- For high accuracy needs, consider changing the Whisper model from "tiny" to "small" or "medium" in `whisper_server.py`

## Performance Notes

- The tiny Whisper model processes audio approximately 3x faster than the small model
- Edge TTS processes about 90 characters per second
- For a typical 50-word response, TTS generation takes ~3-4 seconds 
