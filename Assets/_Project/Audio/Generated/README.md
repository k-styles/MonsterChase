# Generated placeholder audio

Synthesised, not recorded. The project had no footstep or creature sounds at all, so
these were generated to make the ending playable and are meant to be replaced.

| File | What it is |
|---|---|
| `sfx_step_tile_1..6` | Running on hard tile. Wired into `FootstepAudio` on the player rig. |
| `sfx_roar_1..3` | Long roars. `CreatureVoice` uses these at distance. |
| `sfx_snarl_1..3` | Short, close snarls. Used inside `closeRange`. |
| `sfx_chase_loop` | Seamless low rumble, the creature's chase bed. |
| `sfx_kill` | Impact, crunch and a final roar, for the catch. |
| `sfx_breath_loop` | The child breathing. Volume tracks exertion in `PlayerBreath`. |
| `sfx_gasp` | The forced inhale when held breath runs out. Loud on purpose. |

Regenerating them is not automated; they were written once by a script. Swapping in
real recordings needs nothing but matching file names.
