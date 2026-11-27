# 再见潮汐 | Farewell, Tide

A story-driven puzzle platformer for GitHub Game Off 2026. Press **Shift** to slow the world and switch into platform placement mode. Collect the yellow orbs, place platforms at marked spots or find your own route, and follow the memory fragments to two endings.

## Play on Windows

Download the Windows x64 ZIP from the game's itch.io page. Extract the entire archive and launch `再见潮汐.exe`. The game is in Simplified Chinese.

- A/D or arrow keys: move. Space: jump or advance dialogue. Hold Space for two seconds during dialogue to fast-forward.
- Shift: enter or leave platform placement. Drag a platform with the mouse after collecting a yellow orb.
- Red platforms provide footing. Green platforms boost jumps. On a purple platform, press V to dash.
- E or Enter: interact. Walk up to the backup terminal before interacting.
- R: retry the current trial. Esc: pause, settings and exit.

## Unity source

Open the repository root with **Unity 2022.3.62f3c1**. The initial scene is `Assets/Scenes/Tide/00_Title.unity`. Unity's checked-in build settings contain the campaign scene sequence.

This source archive includes the C# code, serialized scenes, map data, configuration, journal text and the Noto Sans CJK SC font under the SIL Open Font License. Raw artwork, animation sprites, tiles, music and sound files are distributed separately where authorized. To run the full game in Unity, restore the matching asset folders with their original Unity .meta files. See [asset locations](ASSETS_REQUIRED.md). The Windows release includes the assets it needs to run.

AI tools assisted with code implementation and debugging, game-art iteration and testing. Project contributors retain their respective rights. The Noto and Kenney notices are provided in `Assets/Tide`; those notices do not apply to other assets.

## Game Off build — 2026-11-26

Falls, spikes, and other lethal hazards return the player to the center of the last fixed platform checkpoint, restoring placed platforms, orbs and energy. The fourth trial's guide now skips the old third platform. R restarts the current trial from the beginning.

The title screen resumes the last story scene or trial checkpoint. The Esc stage menu includes six story acts and ten trials, unlocked as they are reached. Selecting an unlocked entry replays it from its start.

After collecting a journal, use Tab to browse the collection. Space turns the page and Esc closes the journal. Checkpoints activate when the player stands near the center of a fixed platform.


A checkpoint uses the latest fixed brick center for respawn. It restores the platforms and energy from the most recent orb pickup confirmed by standing on fixed brick.
