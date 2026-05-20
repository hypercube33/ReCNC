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

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// Defines the algorithm used for dock selection (refineries, helipads, repair pads).
	/// </summary>
	public enum DockingAlgorithm
	{
		/// <summary>OpenRA's default: closest dock with occupancy cost modifier.</summary>
		OpenRA,

		/// <summary>Classic C&amp;C: simple distance-based, first-come-first-served.</summary>
		ClassicCnC,

		/// <summary>Improved: wait-vs-travel calculation with queue management.</summary>
		Improved
	}

	/// <summary>
	/// Strategy interface for dock selection algorithms.
	/// Answers: "Which dock should this unit go to?"
	/// </summary>
	public interface IDockingStrategy
	{
		/// <summary>Human-readable name for this strategy.</summary>
		string Name { get; }

		/// <summary>
		/// Finds the best dock for a client from available options.
		/// </summary>
		/// <param name="client">The unit seeking a dock.</param>
		/// <param name="availableDocks">All docks the client can use.</param>
		/// <param name="dockManager">The client's dock manager.</param>
		/// <returns>The best dock, or null if none available.</returns>
		TraitPair<IDockHost>? FindBestDock(
			Actor client,
			IEnumerable<TraitPair<IDockHost>> availableDocks,
			DockClientManager dockManager);

		/// <summary>
		/// Determines if the client should wait at current dock or find an alternate.
		/// </summary>
		/// <param name="client">The unit currently waiting.</param>
		/// <param name="currentDock">The dock being waited at.</param>
		/// <param name="alternates">Other available docks.</param>
		/// <returns>True if waiting is optimal, false if should redirect.</returns>
		bool ShouldWaitAtCurrentDock(
			Actor client,
			IDockHost currentDock,
			IEnumerable<TraitPair<IDockHost>> alternates);

		/// <summary>
		/// Creates the appropriate MoveToDock activity for this strategy.
		/// </summary>
		Activity CreateMoveToDockActivity(
			Actor self,
			Actor dockHostActor = null,
			IDockHost dockHost = null,
			bool forceEnter = false,
			bool ignoreOccupancy = false,
			OpenRA.Primitives.Color? dockLineColor = null);
	}
}
