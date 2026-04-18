# Behide 🔺
A prop hunt made with [Godot](https://godotengine.org)  
This game is in **<ins>slow</ins> development**

# WIP
- [X] Networking
- [X] Base user interface
- [X] Base gameplay
- [ ] Playable game
- [ ] Player account?
- [ ] Coherent design

# Detailed WIP
- [ ] Add effects when hunter shoots
- [X] Add end game scene
- [X] Add ability to start a new game
- [X] Add ability to choose the hunters/props repartition
- [ ] Add game info on HUD
  - [ ] Ammo
  - [ ] Kill field
- [ ] Crash test

# Development
- Clone the repo: `git clone https://github.com/behide-game/behide --recursive`
- Download the [WebRTC native extension](https://github.com/godotengine/webrtc-native/releases)
  - Download the [`godot-extension-webrtc.zip` file](https://github.com/godotengine/webrtc-native/releases)
  - Unzip it
  - Put the `webrtc` folder at the root of the project
- Download the models [here](https://nc.titaye.dev/s/7wa2iic9mDo8QDz)
(put the `Models` folder in the `Assets` folder)
- Let Godot import files (you can run the `godot --import` command)
- Add a `.env` file:
    ```.dotenv
    SIGNALING_URL=https://signaling-server-url.com/
    RELAY_USERNAME=username
    RELAY_PASSWORD=password
    ```
