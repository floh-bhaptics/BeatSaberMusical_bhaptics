# bhapticsMusical

This mod hooks into a few functions in Beat Saber and provides some feedback effects in the bhaptics vest on musical cues.

I made a second *"bHapticsFunctional"* mod that will provide feedback for actions like slicing or hitting walls, hence the "Musical" in the name.

You can see a short demo of the effects provided by both mods here:
[https://www.youtube.com/watch?v=X15WuW8BiaM](https://www.youtube.com/watch?v=X15WuW8BiaM)

## Adjusting the feedback patterns

On light effects, the mod will randomly trigger one of the patterns found in ``UserData\bHapticsMusical\``. If the folder does not exist
(e.g. on the first run), it will be created and 6 default patterns will be placed in there. You can easily delete, replace, or add any effects
here to adjust the mod to your liking. Use the [bhaptics designer](https://designer.bhaptics.com/) for this, which can import, adjust and
export the ``.tact`` files. To reset the patterns, just delete or rename the ``bHapticsMusical`` folder and start the game with the mod once.

## The way it works

On loading a song, the mod will analyze the lighting effect events in the map and estimate what "amount" of light changes constitutes a larger
musical cue. It will then trigger some feedback effects when this happens within the song. It also dynamically adjusts the trigger if it
turns out to be set way too high or too low during the song.

Additionally, the mod will trigger a pattern drawing a circle on your chest if the background spiral rotation effect appears in the song.


## Compilation / installation

The mod uses BSIPA to hook into Beat Saber methods via Harmony, and uses the bHaptics library to interface the HTTP/TLS interface of the bHaptics Player.
So BSIPA and the bHaptics library have to be installed on Beat Saber.  It does not touch the game variables or functions and only
hooks in to trigger feedback. It expects the haptics feedback patterns to be placed in `UserData\bHapticsMusical\`. They can be modified or replaced by
the user if they want different kinds of feedback.

The mod is built with Visual Studio 2019 and should just compile if the BSIPA modding tools are installed correctly.
