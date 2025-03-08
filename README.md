# MR Coding Tutor Unity Applications

This repository contains two (three actually) applications for remote coding tutoring:
- **Teacher**: Application for teachers to share their screen and provide guidance
- **VRStudent**: Application for students to view the teacher's screen and receive instruction

## Setup and Usage Instructions

### 1. Update Agora Token (if 24h has passed)

Agora tokens expire after 24 hours and need to be refreshed for continued use:

a. Go to Agora Console at [https://console.agora.io/token/KJkokymWN](https://console.agora.io/token/KJkokymWN)
   - Login with Google: email `sanglythesis@gmail.com`, pass: *[use your password]*

b. Enter channel name as `main`, then click `Generate`. 

c. Copy the generated token to `agora_config.json` in BOTH app folders:
   ```json
   {
     "token": "YOUR_NEWLY_GENERATED_TOKEN",
     "channelName": "main"
   }
   ```

### 2. Start the Desktop Capture Server (Student Side)

a. Open an Administrator terminal 
b. Navigate to the student app folder:
   ```
   cd [path_to_VRStudent_app_folder]
   ```
c. Run the desktop capture server:
   ```
   make shremdup
   ```

### 3. Launch the Applications

a. Run `VRStudent` as administrator (so that it can interact with the desktop capture server)
b. Run `Teacher` app

### Important Notes

- Make sure both machines have a webcam and working microphone
- The machine running `VRStudent` should be Windows-based for desktop capture to work
- If you encounter connection issues, ensure both applications are using the same token and channel name
- The Agora AppID is built into the applications, only the token and channel name need to be updated

## Troubleshooting

If you encounter issues with the applications:

1. Check that both applications have identical `agora_config.json` files with the same token
2. Ensure the desktop capture server AND `VRStudent` app are running with administrator privileges
3. Verify that all hardware (webcam, microphone) is properly connected and functioning
4. Check that both computers are connected to the internet and not blocked by firewalls
