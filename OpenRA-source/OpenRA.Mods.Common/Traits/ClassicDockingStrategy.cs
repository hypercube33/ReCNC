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
	/// Classic C&amp;C docking strategy.
	/// Simple distance-based selection with radio contact check.
	/// Port of Find_Docking_Bay from TECHNO.CPP:
	/// - Loop through all buildings of specified type
	/// - Check if building can accept (RADIO_CAN_LOAD == RADIO_ROGER)
	/// - Pick closest by straight-line distance
	/// - Prefer "leader" (primary) buildings
	/// 
	/// Does NOT consider: queue length, wait times, or pathfinding distance.
	/// This matches the original C&amp;C behavior where harvesters would all
	/// pile up at the nearest refinery regardless of congestion.
	/// </summary>
	public class ClassicDockingStrategy : IDockingStrategy
	{
		public string Name => "Classic C&C (Simple Distance)";

		public TraitPair<IDockHost>? FindBestDock(
			Actor client,
			IEnumerable<TraitPair<IDockHost>> availableDocks,
			DockClientManager dockManager)
		{
			if (client == null || availableDocks == null)
				return null;

			var clientPos = client.CenterPosition;
			TraitPair<IDockHost>? best = null;
			long bestDistance = long.MaxValue;
			bool bestIsPrimary = false;

			foreach (var dock in availableDocks)
			{
				if (dock.Trait == null || !dock.Trait.IsEnabledAndInWorld)
					continue;

				var isPrimary = dock.Actor.TraitOrDefault<PrimaryBuilding>()?.IsPrimary ?? false;
				var distance = (clientPos - dock.Trait.DockPosition).LengthSquared;

				var isBetter = false;
				if (isPrimary && !bestIsPrimary)
				{
					isBetter = true;
				}
				else if (isPrimary == bestIsPrimary)
				{
					isBetter = distance < bestDistance;
				}

				if (isBetter)
				{
					best = dock;
					bestDistance = distance;
					bestIsPrimary = isPrimary;
				}
			}

			return best;
		}

		public bool ShouldWaitAtCurrentDock(
			Actor client,
			IDockHost currentDock,
			IEnumerable<TraitPair<IDockHost>> alternates)
		{
			return true;
		}

		public Activity CreateMoveToDockActivity(
			Actor self,
			Actor dockHostActor = null,
			IDockHost dockHost = null,
			bool forceEnter = false,
			bool ignoreOccupancy = false,
			Color? dockLineColor = null)
		{
			return new MoveToDock(self, dockHostActor, dockHost, forceEnter, ignoreOccupancy, dockLineColor);
		}
	}
}
