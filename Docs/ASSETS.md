# Assets

Our own art and code live under `Assets/_Project`. Third-party packs live at the
**top level** of `Assets/` and are deliberately **not** in git.

## This repository is public

Asset Store licences forbid redistributing pack contents. A public repo is
redistribution. So:

1. Import a pack to `Assets/<PackName>/`, never inside `Assets/_Project/`.
2. Add `/Assets/<PackName>/` and `/Assets/<PackName>.meta` to `.gitignore`
   **the same day**, before the next commit.
3. Record it in the table below so a fresh clone knows what to reimport.
4. Verify with `git status` that none of it is staged.

CC0 / public-domain assets may be tracked. Anything bought or licensed may not.

> This is not hypothetical. The previous project committed 370MB of a paid
> hospital pack before anyone checked, and publishing meant rewriting history.

## Tracked, in git

| Pack | Path | Licence | Notes |
|---|---|---|---|
| _(none yet)_ | | | |

## Not tracked, reimport from Package Manager

| Pack | Path | Size | Notes |
|---|---|---|---|
| _(none yet)_ | | | |
