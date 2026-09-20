# Creature audio

Sourced by Kartik, 2026-09-20. Mapping is his, set in `BuildChaseScene`:

| File | When it plays | Wired to |
|---|---|---|
| `cre_roar_reveal` | The flickering light catches it and it charges | `ChaseSequence.revealRoar` |
| `cre_screech_1/2` | Every time it spots you and commits to a chase | `CreatureVoice.noticeSounds` |
| `cre_growl_constant` | Always, looping, spatialised on the creature | `Presence/ConstantVoice` |
| `cre_bite` | The catch | `ChaseSequence.killClip` |
| `cre_growl_afterkill` | Immediately after the bite, over the blood | `ChaseSequence.afterDeathClip` |

The constant growl is 77s and set to stream rather than decompress into memory.
Because it is spatialised and always on, it doubles as the player's only reliable way
to tell where the creature is while hidden.
