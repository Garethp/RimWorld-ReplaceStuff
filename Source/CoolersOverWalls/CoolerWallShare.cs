﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff
{
	[DefOf]
	public static class OverWallDef
	{
		public static bool IsWall(this BuildableDef bdef)
		{
			//return bdef == ThingDefOf.Wall;
			return bdef is ThingDef def && def.coversFloor &&
				(!def.building?.isNaturalRock ?? true);
		}

		public static ThingDef Cooler_Over;
		public static ThingDef Cooler_Over2W;
		public static ThingDef Vent_Over;
		public static bool IsOverWall(this BuildableDef bdef)
		{
			return bdef == Cooler_Over || bdef == Cooler_Over2W || bdef == Vent_Over;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	class CoolerWallShare_Blocks
	{
		//public static bool BlocksConstruction(Thing constructible, Thing t)
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (!__result) return;

			ThingDef thingDef = constructible is Blueprint ? constructible.def
				: constructible is Frame ? constructible.def.entityDefToBuild.blueprintDef
				: constructible.def.blueprintDef;

			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if (thingDef.entityDefToBuild is ThingDef def
				&& ((def.IsWall() && t.def.IsOverWall())
				|| (t.def.IsWall() && def.IsOverWall())))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver")]
	class CoolerWallShare_Blueprint
	{
		//public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
		public static void Postfix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
		{
			if (__result) return;

			BuildableDef oldBuildDef = GenConstruct.BuiltDefOf(oldDef);
			if (oldDef.category == ThingCategory.Building || oldDef.IsBlueprint || oldDef.IsFrame)
			{
				//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
				if ((newDef.IsOverWall() && oldBuildDef.IsWall())	|| (newDef.IsWall() && oldBuildDef.IsOverWall()))
				{
					__result = true;
				}
			}
		}
	}


	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	class CoolerWallShare_Wipes
	{
		//public static bool SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef)
		public static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
		{
			if (!__result) return;

			ThingDef newDef = newEntDef as ThingDef;
			ThingDef oldDef = oldEntDef as ThingDef;
			BuildableDef newBuiltDef = GenConstruct.BuiltDefOf(newDef);
			BuildableDef oldBuiltDef = GenConstruct.BuiltDefOf(oldDef);
			
			//Power conduit sharing is hardcoded, so cooler sharing is hardcoded too
			if ((newBuiltDef.IsOverWall() && oldBuiltDef.IsWall())
				|| (newBuiltDef.IsWall() && oldBuiltDef.IsOverWall()))
			{
				__result = false;	
			}
		}
	}
}
