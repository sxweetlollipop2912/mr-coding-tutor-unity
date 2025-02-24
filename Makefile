.PHONY: shremdup tts whisper pyenv

shremdup:
	./Assets/Scenes/DesktopDuplication/shremdup.exe 3030
tts:
	python tts_server.py
whisper:
	python whisper_server.py
espeak:
	espeak-ng.exe

