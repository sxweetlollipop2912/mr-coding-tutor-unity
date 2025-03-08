# Agora External Configuration

This application supports loading Agora token and channel name from an external JSON file, allowing you to change these settings without rebuilding the application.

## How to Use

### In a Built Application
1. Place a file named `agora_config.json` in the **same directory** as your .exe file.
2. The JSON file should have the following structure:

```json
{
  "token": "YOUR_TOKEN_HERE",
  "channelName": "YOUR_CHANNEL_NAME_HERE"
}
```

### In the Unity Editor
You can place the `agora_config.json` file in either:
1. The `Assets/Scenes/Agora` folder (recommended)
2. Or directly in the `Assets` folder

Use the same JSON structure as shown above.

## Configuration File Structure

```json
{
  "token": "YOUR_TOKEN_HERE",
  "channelName": "YOUR_CHANNEL_NAME_HERE"
}
```

Replace `YOUR_TOKEN_HERE` with your Agora token and `YOUR_CHANNEL_NAME_HERE` with your desired channel name.

## Notes

- If the external configuration file doesn't exist, a default one will be created automatically in the Assets/Scenes/Agora folder (when in Unity Editor).
- The application will use the AppID set directly in the Unity component, while the token and channel name will be loaded from the external file.
- Both the student and teacher applications use the same configuration file.
- You can disable external configuration loading in the Unity editor by unchecking the "Use External Config" checkbox in the inspector, but you'll need to set token and channel values manually in the inspector if you do this.
- The application validates all parameters at startup and logs errors if any required parameters are missing or empty. 