from flask import Flask, request, send_file
from TTS.api import TTS

app = Flask(__name__)

# Load the VITS model (single-speaker model)
tts = TTS(model_name="tts_models/en/ljspeech/vits", gpu=False)

@app.route('/tts', methods=['POST'])
def generate_tts():
    # Parse incoming JSON data
    data = request.get_json()
    text = data.get("text", "")
    if not text:
        return {"error": "No text provided"}, 400

    # Generate audio from text
    output_path = "tts_output.wav"
    try:
        # tts.tts_to_file(text=text, file_path=output_path)  # Ensure no extra parameters are passed

        tts.tts_to_file(text=text, file_path=output_path, length_scale=0.9)
    except Exception as e:
        return {"error": str(e)}, 500

    # Return the audio file
    return send_file(output_path, mimetype="audio/wav", as_attachment=True)

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5112)

