from flask import Flask, request, jsonify
import whisper
import os

app = Flask(__name__)
model = whisper.load_model("small")  # Change "base" to "small", "medium", or "large" if needed

@app.route("/transcribe", methods=["POST"])
def transcribe():
    if "audio" not in request.files:
        return jsonify({"error": "No audio file provided"}), 400

    audio = request.files["audio"]
    audio_path = "temp_audio.wav"
    audio.save(audio_path)

    # Transcribe the audio file
    result = model.transcribe(audio_path)
    os.remove(audio_path)

    return jsonify({"transcription": result["text"]})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5111)

