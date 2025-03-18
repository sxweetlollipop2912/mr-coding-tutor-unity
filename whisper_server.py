from flask import Flask, request, jsonify
import whisper
import io
import numpy as np
import threading
import torch
import base64

app = Flask(__name__)

# Use the tiny model for speed
model = whisper.load_model("tiny")

# Ensure we're using GPU if available
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device}")

# Set number of threads when using CPU
if device == "cpu":
    torch.set_num_threads(4)  # Adjust based on your CPU


def process_audio_bytes(audio_bytes):
    # Convert bytes to numpy array without saving to disk
    audio_np = np.frombuffer(audio_bytes, np.int16).astype(np.float32) / 32768.0
    # Process with Whisper
    result = model.transcribe(audio_np, language="en")
    return result["text"]


@app.route("/transcribe", methods=["POST"])
def transcribe():
    if "audio" in request.files:
        # Handle file upload
        audio = request.files["audio"]
        audio_bytes = audio.read()
    elif request.data:
        # Handle raw bytes
        audio_bytes = request.data
    elif request.json and "audio_base64" in request.json:
        # Handle base64 encoded audio
        audio_bytes = base64.b64decode(request.json["audio_base64"])
    else:
        return jsonify({"error": "No audio data provided"}), 400

    try:
        # Process in a thread to avoid blocking
        text = process_audio_bytes(audio_bytes)
        return jsonify({"transcription": text})
    except Exception as e:
        print(f"Error processing audio: {str(e)}")
        return jsonify({"error": str(e)}), 500


# Add websocket support for streaming
@app.route("/health", methods=["GET"])
def health_check():
    return jsonify({"status": "healthy", "model": "tiny"})


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5111, threaded=True)
