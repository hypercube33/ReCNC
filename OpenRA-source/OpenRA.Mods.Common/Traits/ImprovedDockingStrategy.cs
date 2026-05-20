#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// Improved docking strategy with intelligent wait-vs-travel decisions.
	/// 
	/// Key features:
	/// - Calculates actual travel time using pathfinding
	/// - Estimates wait time based on queue length and dock duration
	/// - Continuously re-evaluates: "Is waiting here faster than going elsewhere?"
	/// - Considers priority (damaged units queue-jump)
	/// - Remembers preferred docks (familiarity bonus)
	/// 
	/// The core question answered:
	/// "Given travel time to alternate + wait time there vs wait time here,
	/// which option gets me docked faster?"
	/// 
	/// Example scenario (harvesters at refineries):
	/// - Refinery A: 0 cells away, 3 harvesters waiting = ~300 tick wait
	/// - Refinery B: 10 cells away = ~200 tick travel, 0 waiting = 0 tick wait
	/// - Decision: 200 &lt; 300, go to Refinery B!
	/// </summary>
	public class ImprovedDockingStrategy : IDockingStrategy
	{
		readonly World world;
		SmartDockingService smartDocking;

		public ImprovedDockingStrategy(World world)
		{
			this.world = world;
		}

		public string Name => "Improved (Wait vs Travel Analysis)";

		SmartDockingService GetSmartDocking()
		{
			smartDocking ??= world?.WorldActor?.TraitOrDefault<SmartDockingService>();
			return smartDocking;
		}

		public TraitPair<IDockHost>? FindBestDock(
			Actor client,
			IEnumerable<TraitPair<IDockHost>> availableDocks,
			DockClientManager dockManager)
		{
			var service = GetSmartDocking();
			if (service == null)
			{
				return availableDocks.ClosestDock(client, dockManager);
			}

			var decision = service.FindBestDock(client, availableDocks, dockManager);
			return decision?.RecommendedDock;
		}

		public bool ShouldWaitAtCurrentDock(
			Actor client,
			IDockHost currentDock,
			IEnumerable<TraitPair<IDockHost>> alternates)
		{
			var service = GetSmartDocking();
			if (service == null)
				return true;

			return service.ShouldWaitAtCurrentDock(client, currentDock, alternates);
		}

		public Activity CreateMoveToDockActivity(
			Actor self,
			Actor dockHostActor = null,
			IDockHost dockHost = null,
			bool forceEnter = false,
			bool ignoreOccupancy = false,
			Color? dockLineColor = null)
		{
			return new ImprovedMoveToDock(self, dockHostActor, dockHost, forceEnter, ignoreOccupancy, dockLineColor);
		}
	}
}
