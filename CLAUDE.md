# MonsterChase

First-person co-op horror. Unity 6000.0.84f1 LTS, URP, new Input System only
(`activeInputHandler: 1` — the old `Input.GetKey` API will not compile).

**This repository is public.** See `Docs/ASSETS.md` before importing anything.

## The design

You are hunted through a map by a monster that heals and cannot be killed.

Five corpses scattered around the map **anchor** it. Burn one and its healing
slows and its stagger lengthens. Burn all five and the healing stops, and only
then do guns actually kill it.

That arc is the game: guns go **useless -> situational -> lethal**. The blood and
the bodies are not set dressing — the bodies are the objective.

The mechanic lives or dies on the heal timing being *readable*. If the player
cannot feel that burning an anchor changed something, the whole thing reads as
busywork. Build the numbers visible first, dress them later.

## Scope

Single-player now. Co-op (Netcode for GameObjects) later, deliberately deferred.

Because the retrofit is coming, systems are written **netcode-shaped** from the
start: one authoritative owner per piece of state, mutated in one place, never
scattered across local callers. Anchor state, monster health and heal timers,
and objective progress all belong to a single owner object. This costs nothing
now and makes the retrofit much smaller.

## Layout

- `Assets/_Project/` — everything we make. Nothing else goes here.
- `Assets/<PackName>/` — third-party packs, at the top level, gitignored.
- `Docs/ASSETS.md` — what each pack is and where to reimport it from.
