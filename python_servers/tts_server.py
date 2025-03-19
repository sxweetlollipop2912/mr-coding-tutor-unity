from flask import Flask, request, jsonify, Response
import edge_tts
import asyncio
import io
import threading
import time
import os

app = Flask(__name__)

# Enable multi-threading for the server
app.config["THREADED"] = True

# Default voice settings (used if not specified in the request)
DEFAULT_VOICE = "en-US-ChristopherNeural"  # Default male voice
DEFAULT_RATE = "+15%"  # Faster speed


@app.route("/tts", methods=["POST"])
def generate_tts():
    # Parse incoming JSON data
    data = request.get_json()
    text = data.get("text", "")
    if not text:
        return {"error": "No text provided"}, 400

    # Get voice and rate from request or use defaults
    voice = data.get("voice", DEFAULT_VOICE)
    rate = data.get("rate", DEFAULT_RATE)

    # Generate audio directly to memory
    try:
        # Use asyncio to run the async TTS function
        start_time = time.time()
        audio_data = asyncio.run(synthesize_speech(text, voice, rate))
        end_time = time.time()
        print(
            f"TTS generation completed in {end_time - start_time:.2f} seconds for {len(text)} characters"
        )

        # Return the audio data directly from memory
        return Response(audio_data, mimetype="audio/mp3")

    except Exception as e:
        print(f"TTS Error: {str(e)}")
        return {"error": str(e)}, 500


async def synthesize_speech(text, voice, rate):
    """Synthesize speech using Edge TTS and return the audio bytes."""
    # Create temporary file path
    temp_file = f"temp_tts_{int(time.time())}.mp3"

    try:
        # Use communicate to generate speech to a file
        communicate = edge_tts.Communicate(text, voice, rate=rate)

        # Edge TTS works better with real files
        await communicate.save(temp_file)

        # Read the file
        with open(temp_file, "rb") as f:
            audio_data = f.read()

        # Clean up
        os.remove(temp_file)

        return audio_data
    except Exception as e:
        print(f"Error in synthesize_speech: {e}")
        # Clean up if file exists
        if os.path.exists(temp_file):
            os.remove(temp_file)
        raise


@app.route("/voices", methods=["GET"])
def get_voices():
    """Return a list of recommended voices for different languages"""
    recommended_voices = {
        "en": [
            {
                "name": "en-US-ChristopherNeural",
                "gender": "Male",
                "description": "Christopher (Default)",
            },
            {"name": "en-US-GuyNeural", "gender": "Male", "description": "Guy"},
            {"name": "en-US-JennyNeural", "gender": "Female", "description": "Jenny"},
            {"name": "en-US-AriaNeural", "gender": "Female", "description": "Aria"},
            {
                "name": "en-GB-RyanNeural",
                "gender": "Male",
                "description": "Ryan (British)",
            },
        ],
        "fr": [
            {"name": "fr-FR-HenriNeural", "gender": "Male", "description": "Henri"},
            {"name": "fr-FR-DeniseNeural", "gender": "Female", "description": "Denise"},
        ],
        "de": [
            {"name": "de-DE-ConradNeural", "gender": "Male", "description": "Conrad"},
            {"name": "de-DE-KatjaNeural", "gender": "Female", "description": "Katja"},
        ],
        "es": [
            {"name": "es-ES-AlvaroNeural", "gender": "Male", "description": "Alvaro"},
            {"name": "es-ES-ElviraNeural", "gender": "Female", "description": "Elvira"},
        ],
        "zh": [
            {"name": "zh-CN-YunjianNeural", "gender": "Male", "description": "Yunjian"},
            {
                "name": "zh-CN-XiaoxiaoNeural",
                "gender": "Female",
                "description": "Xiaoxiao",
            },
        ],
        "ja": [
            {"name": "ja-JP-KeitaNeural", "gender": "Male", "description": "Keita"},
            {"name": "ja-JP-NanamiNeural", "gender": "Female", "description": "Nanami"},
        ],
    }

    return jsonify(recommended_voices)


@app.route("/health", methods=["GET"])
def health_check():
    return jsonify(
        {
            "status": "healthy",
            "engine": "Microsoft Edge TTS",
            "default_voice": DEFAULT_VOICE,
            "default_rate": DEFAULT_RATE,
        }
    )


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5112, threaded=True)
