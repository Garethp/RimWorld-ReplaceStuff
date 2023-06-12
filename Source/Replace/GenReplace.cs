﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace Replace_Stuff
{
	[StaticConstructorOnStartup]
	public static class QBTypes
	{
		public static DesignationDef qbDesDef;
		public static Type compQBType;
		public static Type compPropQBType;

		static QBTypes()
		{
			try
			{
				compQBType = AccessTools.TypeByName("CompQualityBuilder");
				compPropQBType = AccessTools.TypeByName("CompProperties_QualityBuilderr");
				qbDesDef = DefDatabase<DesignationDef>.GetNamed("SkilledBuilder", false);
			}
			catch (System.Reflection.ReflectionTypeLoadException) //Aeh, this happens to people, should not happen, meh.
			{
				Verse.Log.Warning("Replace Stuff failed to check for Quality Builder");
			}
		}
	}

	static class GenReplace
	{
		public static ReplaceFrame PlaceReplaceFrame(Thing oldThing, ThingDef stuff)
		{
			ThingDef replaceFrameDef = ThingDefGenerator_ReplaceFrame.ReplaceFrameDefFor(oldThing.def);

			if (replaceFrameDef == null) return null;

			//if (oldThing.Position.GetFirstThing(oldThing.Map, replaceFrameDef) != null) return null;

			ReplaceFrame replaceFrame = (ReplaceFrame)ThingMaker.MakeThing(replaceFrameDef, stuff);

			//QualityBuilder
			if(QBTypes.qbDesDef != null &&
				replaceFrame.def.HasComp(QBTypes.compQBType))
				oldThing.Map.designationManager.AddDesignation(new Designation(replaceFrame, QBTypes.qbDesDef));

			replaceFrame.SetFactionDirect(Faction.OfPlayer);
			oldThing.SetFaction(Faction.OfPlayer);
			replaceFrame.oldThing = oldThing;
			replaceFrame.oldStuff = oldThing.Stuff;
			GenSpawn.Spawn(replaceFrame, oldThing.Position, oldThing.Map, oldThing.Rotation);
			return replaceFrame;
		}
	}

	//[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
	[StaticConstructorOnStartup]
	public static class ThingDefGenerator_ReplaceFrame
	{
		/*
		public static void Postfix()
		{
			// would be nice but mods don't add defs early enough.
			// I mean they assume blueprints/frames don't need to be implied from them
			// But replace frames sure can!
			AddReplaceFrames(false);
		}
		*/
		public delegate void GiveShortHashDel(Def d, Type t, HashSet<ushort> h);
		public static GiveShortHashDel GiveShortHash = AccessTools.MethodDelegate<GiveShortHashDel>(AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash"));
		public static void AddReplaceFrames(bool addShortHash = true)
		{
			Type type = typeof(ThingDef);

			// Slow reflection since this is only once:
			HashSet<ushort> takenHashes = ((Dictionary < Type, HashSet<ushort> > )AccessTools.Field(typeof(ShortHashGiver), "takenHashesPerDeftype").GetValue(null))[type];

			foreach (ThingDef current in ThingDefGenerator_ReplaceFrame.ImpliedReplaceFrameDefs())
			{
				if (addShortHash)  //Wouldn't need this if other mods added defs earlier. Oh well.
					GiveShortHash(current, type, takenHashes);
				current.PostLoad();
				DefDatabase<ThingDef>.Add(current);
			}
		}

		public static Dictionary<ThingDef, ThingDef> replaceFrameDefs;
		public static ThingDef ReplaceFrameDefFor(ThingDef def)
		{
			if (replaceFrameDefs.TryGetValue(def, out ThingDef replaceFrame))
				return replaceFrame;
			Verse.Log.Warning($"Couldn't find replace frame for {def} : probably a mod's building that isn't added to the database soon enough");

			return null;
		}

		public static bool HasReplaceFrame(this ThingDef def)
		{
			return replaceFrameDefs.ContainsKey(def);
		}

		public static IEnumerable<ThingDef> ImpliedReplaceFrameDefs()
		{
			replaceFrameDefs = new Dictionary<ThingDef, ThingDef>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.ToList<ThingDef>())
			{
				if (def.designationCategory != null && def.IsBuildingArtificial && !def.IsFrame && def.MadeFromStuff)
				{
					ThingDef replaceFrameDef = NewReplaceFrameDef_Thing(def);
					replaceFrameDefs[def] = replaceFrameDef;
					yield return replaceFrameDef;
				}
			}
		}

		private static Color DrawColor(ThingDef def)
		{
				if (def.MadeFromStuff)
					return Color.white;
				
				var costList = def.entityDefToBuild.CostList;
				if (costList == null) return new Color(0.6f, 0.6f, 0.6f);
				
				foreach (var costItem in costList)
				{
					var costDef = costItem.thingDef;
					if (costDef.IsStuff && costDef.stuffProps.color != Color.white)
						return def.GetColorForStuff(costDef);
				}
				
				return new Color(0.6f, 0.6f, 0.6f);
		}
		
		public static ThingDef NewReplaceFrameDef_Thing(ThingDef def)
		{
			ThingDef thingDef = ThingDefGenerator_ReplaceFrame.BaseFrameDef();
			thingDef.defName = def.defName + "_ReplaceStuff";
			thingDef.label = def.label + "TD.ReplacingTag".Translate();//Not entirely sure if this is needed since ReplaceFrame.Label doesn't use it, but, this is vanilla Frame code.
			thingDef.size = def.size;
			thingDef.SetStatBaseValue(StatDefOf.MaxHitPoints, (float)def.BaseMaxHitPoints * 0.25f);
			thingDef.SetStatBaseValue(StatDefOf.Beauty, -8f);
			thingDef.fillPercent = 0.2f;
			thingDef.pathCost = 10;
			thingDef.description = def.description;
			thingDef.passability = def.passability;
			thingDef.selectable = def.selectable;
			thingDef.constructEffect = def.constructEffect;
			thingDef.building.isEdifice = false;
			thingDef.constructionSkillPrerequisite = def.constructionSkillPrerequisite;
			thingDef.clearBuildingArea = false;
			thingDef.drawPlaceWorkersWhileSelected = def.drawPlaceWorkersWhileSelected;
			thingDef.stuffCategories = def.stuffCategories;

			if (def.size.x <= 4 && def.size.z <= 4)
			{
				thingDef.drawerType = DrawerType.MapMeshOnly;
				thingDef.graphicData = new GraphicData();
				thingDef.graphicData.graphicClass = typeof(Graphic_Single);
				thingDef.graphicData.texPath = $"ReplaceStuffFrame/{def.size.x}x{def.size.z}";
				thingDef.graphicData.drawSize = new Vector2(def.size.x, def.size.z);
				thingDef.graphicData.drawOffset = def.graphicData.drawOffset;
				thingDef.graphicData.shaderType = ShaderTypeDefOf.Transparent;
				thingDef.graphicData.color = DrawColor(thingDef);
			}

			//Support QualityBuilder
			if (QBTypes.compPropQBType!= null)
				if (def.HasComp(typeof(CompQuality)) && def.building != null)
					thingDef.comps.Add((CompProperties)Activator.CreateInstance(QBTypes.compPropQBType));

			thingDef.entityDefToBuild = def;
			//def.replaceFrameDef = thingDef;	//Dictionary instead

			thingDef.modContentPack = LoadedModManager.GetMod<Mod>().Content;
			return thingDef;
		}

		static ThingDef BaseFrameDef()
		{
			return new ThingDef
			{
				isFrameInt = true,
				category = ThingCategory.Building,
				label = "Unspecified stuff replacement frame",
				thingClass = typeof(ReplaceFrame),
				altitudeLayer = AltitudeLayer.BuildingOnTop,
				useHitPoints = true,
				selectable = true,
				building = new BuildingProperties(),
				comps =
				{
					new CompProperties_Forbiddable()
				},
				scatterableOnMapGen = false,
				leaveResourcesWhenKilled = true
			};
		}
	}
}
