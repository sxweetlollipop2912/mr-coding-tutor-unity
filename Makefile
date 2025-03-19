.PHONY: shremdup tts whisper pyenv

shremdup:
	./Assets/Scenes/DesktopDuplication/shremdup.exe 3030
tts:
	python python_servers/tts_server.py
whisper:
	python python_servers/whisper_server.py
espeak:
	espeak-ng.exe
