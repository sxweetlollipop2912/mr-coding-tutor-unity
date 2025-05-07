.PHONY: shremdup tts whisper pyenv

shremdup:
	./auxiliary_services/core_servers/shremdup.exe 3030
tts:
	python auxiliary_services/core_servers/tts_server.py
whisper:
	python auxiliary_services/core_servers/whisper_server.py
espeak:
	espeak-ng.exe
