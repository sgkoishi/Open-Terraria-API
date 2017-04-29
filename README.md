# Open Terraria API 

[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/108qjnbwd629645j?svg=true)](https://ci.appveyor.com/project/DeathCradle/Open-Terraria-API)
[![Travis Build Status](https://travis-ci.org/DeathCradle/Open-Terraria-API.svg?branch=v3)](https://travis-ci.org/DeathCradle/Open-Terraria-API)


The Open Terraria API, known as OTAPI, is a unique low-level API for [Terraria](https://terraria.org) that exposes events and provides performance optimisations while supporting both client and server executables on all official platforms.

You can use this modification as a [NuGet package](https://www.nuget.org/packages/OTAPI/) to power your own project with minimal update downtime*, or you can build plugins for [NyxStudios' TShock](https://github.com/NyxStudios/TShock) which uses OTAPI under the hood.

This version of the OTAPI introduces a couple cool new features.
- The first being a pattern based query mechanism that you can use to find meta data (ie methods, fields etc).
- Secondly a new auto hook mechanism has been introduced

Combine these two new features together and you can hook a number of methods in one go, see below.
```
// hooks any method prefixed with "Send" in Terraria.NetMessage
// this generates a number of Pre/Post hooks in ModFramework.ModHooks.NetMessage
// which can be used in external API consumers
new Query("Terraria.NetMessage.Send*", _framework.CecilAssemblies)
	.Run()
	.Hook()
;

// hooking a single method
// this generates PreAddShop & PostAddShop hooks in ModFramework.ModHooks.Chest
// which can be used in external API consumers
new Query("Terraria.Chest.AddShop(Terraria.Item)", _framework.CecilAssemblies)
	.Run()
	.Hook()
;
```
