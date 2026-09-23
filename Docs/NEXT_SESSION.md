# Next session — Kartik's brief, verbatim

Written 2026-09-23 at the end of the previous session. His words, unedited.

---

> hey, so, i assume have the game access. i want you to make some changes to the
> game. the MonsterCHase game. my idea

> My idea is that it will be a kind of multiplayer game wherein some people can
> join, and at max, for now, let's say 5 people can join in.
>
> There should be a small story behind it where the players go through that whole
> story together, discussing what happened and what went down in the environment
> that they are currently in. As the story progresses, things start to get scary.
> They start hearing noises and stuff like that, but then there is no one around.
> They are still hearing some sounds or whatever, some monster kind of sounds.

> As the story progresses, they are met with this monster that's in the town that
> they are currently in. That monster starts chasing them. They try to kill it,
> but apparently that monster can't die for some reason. It's like an
> interdimensional entity, I guess. Let's go with that: the monster can't die,
> and they still have to progress through the story to find out what actually
> happened and how they can defeat the monster, essentially.

> Don't hand it over to them. Make them work for it, just a bit, just a bit, to
> have fun. Here, I just want the players to have fun. I don't necessarily want
> them to solve any difficult problems, right? They figure out that there is this
> ritual that they have to do first. For now, in the game, the ritual is that
> there are 5 bodies. They would have to go over there to all the bodies and burn
> all of them, and only then, after that, the monster will be vulnerable.

> By the way, when I was playing this game, when I was testing my game, there was
> one issue: when the monster died at the end, there were still noises coming out
> from the monster. Remove them. For the session, you are only allowed to work for
> about, I would say, 15 minutes, and that's it. Then you'll stop, and I don't
> think you'll need to spawn many agents to do this. Just one agent would do.
> I'll be happy to hear your thoughts as well on the story or whatever you might
> want to give feedback on.

---

## Where that session actually got to

Session was stopped early ("hold on. stop everything") before verification.

**Written to disk, uncommitted, never confirmed to compile:**

- `Assets/_Project/Scripts/Monster/MonsterSilence.cs` — new. Fades every AudioSource
  on the monster to zero over 1.2s when `MonsterVitals.Died` fires, disables
  `CreatureVoice` so it cannot pick a new sound mid-fade, and clears `loop` before
  stopping. This is the fix for "when the monster died there were still noises".
- One line in `BuildHospital.cs` attaching it, above the `MonsterDirector` line.

A headless build ran and exited 0, but the compile check was cancelled before it
was read. **First action next session: confirm it compiles, rebuild
FloodedChase, verify, commit.**

Nothing was started on multiplayer or the story.

## Feedback already given, so it is not repeated

- The "they try to kill it and it gets up" beat is the centre of the game; protect it.
- Five people on voice chat will talk over written lore. Write for one player
  reading a short concrete thing aloud to four others.
- The five bodies are already the story: five people who worked out the ritual
  first and did not finish. One readable detail each, and the players assemble it
  themselves while doing the objective.
- The "make them work for it" ordering is already enforced in code — bodies do not
  become interactable until the monster has been seen. What is missing is any hint
  that burning is the answer. Probably one note near the first body.
- Open question raised, not answered: with 5 players, what happens on death?
  Permanent means someone sits out 20 minutes; free means nothing is scary.
- Open question: should burning a body need two people holding at once? It would
  force the group to split and is where the "who goes with who" conversation
  actually comes from.
- `MonsterDirector` already knows where everyone is, so it can be told to prefer
  the player it has bothered least — worth doing early or one loud player soaks
  up the whole game.

See `Docs/MISTAKES.md` for what has already gone wrong and why.
