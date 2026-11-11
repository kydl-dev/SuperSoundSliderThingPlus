# SuperSoundSliderThingPlus

Client-side audio control for Valheim: a master volume, the music, and a mute slider for the sound groups that annoy you most.

## Features
- **Eight sliders** in the KY hub (F6), all *mute levels*: 0% = full loudness, 100% = muted.
- **Master**, **Music** and **Sound effects** are the game's own volumes seen from the other side. Move one here and the game's audio settings follow; move the game's slider and ours follows. The Sound effects slider changes more than the game's own one: it covers every sound effect that has no slider of its own, candles included.
- **World weather** (rain, wind, thunder), **Wolf pups**, **Shield generator** (also its humming wall), **Chicken love** and **Portals** each have a slider of their own.
- **Loudness matching** (on by default, switch it off in the config): every sound is measured once and scaled toward one target loudness, so loud sounds come down and quiet ones come up. Switch it off with `EqualizeLoudness = false` if you prefer the game's own balance.
- The first time the game starts with the mod, the five group sliders start at 50% so you hear them working; after that your values stand.
- Client-side only: nothing is sent to other players and nothing in the world changes.

## How to use
- Requires **KydlCore** (installed automatically by the mod manager).
- Press **F6**, open the **Sound** tab and drag the sliders. Changes apply at once and are saved.

## Configuration
`BepInEx/config/com.kydldev.SuperSoundSliderThingPlus.cfg` (or the in-game config manager):

| Section | Setting | Default | What it does |
|---------|---------|---------|--------------|
| General | EnableAll | true | Switches the whole mod on / off while playing |
| General | MasterMute | 0 | Master mute level (the game's master volume) |
| General | MusicMute | 0 | Music mute level (the game's music volume) |
| General | SoundEffectsMute | 0 | Sound effects without a slider of their own (the game's sound effects volume) |
| General | WeatherMute / WolfPupMute / ShieldGeneratorMute / ChickenLoveMute / PortalMute | 0 (50 after the first start) | Mute level of that group |
| General | EqualizeLoudness | true | Match the loudness of all sounds |
| DebugLog | Enable | false | Detailed lines in the BepInEx log for bug reports |

Edits to the file are picked up while the game runs.

## Multiplayer
Client-side: it works on any server. Every player who wants the sliders installs it for themselves.

## Known limits
- Menu sounds are reached by the Master slider only, and the music by Master and Music; the group sliders only change sound effects.

## Bug reports
Turn on `[DebugLog] Enable`, reproduce the problem, and attach `BepInEx/LogOutput.log`.
