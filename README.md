# Brick by Brick – SkillModify (Mod)

The last skill levels give more: **Strong Arms X carries 50 bricks** (game: 20) and **Brick Magnet X pulls 50
bricks** (game: 10). Works in every world and save slot, also in worlds where you already have these levels.

> Unofficial fan mod, not made by the game's developers. With changed values, every world you open no longer counts
> for the Steam leaderboard (the mod marks it as practice, as the game does for its own development grants).

## Install

1. Download **[skillmodify-mod.zip](https://github.com/michel86ch-ui/brickbybrick-mod-skillmodify/raw/main/skillmodify-mod.zip)**.
2. Unzip everything into the game folder, so that `winhttp.dll` sits next to `BrickByBrick.exe`
   (Steam: right-click the game > Manage > Browse local files). BepInEx already installed (e.g. with
   [Extra Buildings](https://github.com/michel86ch-ui/brickbybrick-mod) or
   [SkillPoints](https://github.com/michel86ch-ui/brickbybrick-mod-skillpoints))? Just overwrite, the files are the same.

Values: `BepInEx\config\michel.brickbybrick.skillmodify.cfg` (created at the first game start).
Uninstall: delete the folder `BepInEx\plugins\SkillModify`.

The ZIP contains BepInEx 5.4.23.5 (unmodified, MIT license) and the mod.

## Building

```
cd src
dotnet build -c Release -p:DeployToGame=true     (copies the DLL into the game)
```

Every level of these skills overrides its stat with a higher priority than the level before, so the mod changes the
value of the last level in the game's skill definitions. The game rebuilds bought levels from these definitions when
a world loads, and the skill tree shows the new value.
