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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// OpenRA's default docking strategy.
	/// Uses pathfinding distance with an occupancy cost modifier.
	/// Does not consider actual wait times or re-evaluate decisions.
	/// </summary>
	public class OpenRADockingStrategy : IDockingStrategy
	{
		public string Name => "OpenRA (Distance + Occupancy)";

		public TraitPair<IDockHost>? FindBestDock(
			Actor client,
			IEnumerable<TraitPair<IDockHost>> availableDocks,
			DockClientManager dockManager)
		{
			return availableDocks.ClosestDock(client, dockManager);
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
