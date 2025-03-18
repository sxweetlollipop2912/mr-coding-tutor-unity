from flask import Flask, request, send_file, jsonify, Response
from TTS.api import TTS
import io
import threading
import torch

app = Flask(__name__)

# Enable multi-threading for the server
app.config["THREADED"] = True

# Set number of threads when using CPU
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device}")

if device == "cpu":
    torch.set_num_threads(4)  # Adjust based on your CPU

# Load the VITS model (single-speaker model)
tts = TTS(model_name="tts_models/en/ljspeech/vits", gpu=device == "cuda")


@app.route("/tts", methods=["POST"])
def generate_tts():
    # Parse incoming JSON data
    data = request.get_json()
    text = data.get("text", "")
    if not text:
        return {"error": "No text provided"}, 400

    # Generate audio directly to memory
    try:
        # Use a BytesIO buffer instead of a file
        output_buffer = io.BytesIO()

        # Set length_scale to 0.9 for faster speech
        # Use fast preset for additional speed
        tts.tts_to_file(
            text=text,
            file_path=output_buffer,
            length_scale=0.9,
            speed=1.1,  # Increase speed slightly
        )

        # Reset buffer position to beginning
        output_buffer.seek(0)

        # Return the audio data directly from memory
        return Response(output_buffer.getvalue(), mimetype="audio/wav")

    except Exception as e:
        print(f"TTS Error: {str(e)}")
        return {"error": str(e)}, 500


@app.route("/health", methods=["GET"])
def health_check():
    return jsonify({"status": "healthy", "model": "vits"})


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5112, threaded=True)
