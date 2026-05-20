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

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Defines a pathfinding algorithm strategy that can be swapped at runtime.
	/// This allows switching between OpenRA's default HPA* and classic C&amp;C pathfinding.
	/// </summary>
	public interface IPathfindingStrategy
	{
		/// <summary>
		/// Display name for this pathfinding strategy (e.g., "OpenRA", "Classic C&amp;C").
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Calculates a path for the actor from a single source to target.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		/// <param name="self">The actor requesting the path.</param>
		/// <param name="source">Starting cell position.</param>
		/// <param name="target">Target cell position.</param>
		/// <param name="check">How to handle blocking actors.</param>
		/// <param name="heuristicWeightPercentage">Weight for the heuristic (100 = optimal, higher = faster but less optimal).</param>
		/// <param name="customCost">Optional custom cost function for cells.</param>
		/// <param name="ignoreActor">Optional actor to ignore when checking blocked cells.</param>
		/// <param name="inReverse">Whether the path is being calculated in reverse.</param>
		/// <param name="laneBias">Whether to apply lane bias to avoid head-on collisions.</param>
		/// <param name="recorder">Optional recorder for path visualization overlay.</param>
		/// <returns>List of cells from target to source, or empty list if no path exists.</returns>
		List<CPos> FindPath(
			Actor self,
			CPos source,
			CPos target,
			BlockedByActor check,
			int heuristicWeightPercentage,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			bool inReverse,
			bool laneBias,
			PathFinderOverlay pathFinderOverlay);

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources to target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and the target is returned.
		/// </summary>
		/// <param name="self">The actor requesting the path.</param>
		/// <param name="sources">Collection of possible starting cell positions.</param>
		/// <param name="target">Target cell position.</param>
		/// <param name="check">How to handle blocking actors.</param>
		/// <param name="heuristicWeightPercentage">Weight for the heuristic (100 = optimal, higher = faster but less optimal).</param>
		/// <param name="customCost">Optional custom cost function for cells.</param>
		/// <param name="ignoreActor">Optional actor to ignore when checking blocked cells.</param>
		/// <param name="inReverse">Whether the path is being calculated in reverse.</param>
		/// <param name="laneBias">Whether to apply lane bias to avoid head-on collisions.</param>
		/// <param name="recorder">Optional recorder for path visualization overlay.</param>
		/// <returns>List of cells from target to source, or empty list if no path exists.</returns>
		List<CPos> FindPath(
			Actor self,
			IReadOnlyCollection<CPos> sources,
			CPos target,
			BlockedByActor check,
			int heuristicWeightPercentage,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			bool inReverse,
			bool laneBias,
			PathFinderOverlay pathFinderOverlay);

		/// <summary>
		/// Determines if a path exists between source and target.
		/// </summary>
		/// <param name="source">Starting cell position.</param>
		/// <param name="target">Target cell position.</param>
		/// <returns>True if a path exists, false otherwise.</returns>
		bool PathExists(CPos source, CPos target);
	}

	/// <summary>
	/// Enumerates the available pathfinding algorithm types.
	/// </summary>
	public enum PathfindingAlgorithm
	{
		/// <summary>
		/// OpenRA's default Hierarchical A* pathfinding.
		/// Uses abstract graphs for faster searches but treats moving units as complete blockers.
		/// </summary>
		OpenRA,

		/// <summary>
		/// Classic Command &amp; Conquer pathfinding from the original 1995 game.
		/// Uses LOS + edge-following algorithm with cost-based blocking (moving units are passable with penalty).
		/// </summary>
		ClassicCnC,

		/// <summary>
		/// Improved pathfinding combining the best of OpenRA and classic C&amp;C.
		/// Uses HPA* with cost-based blocking, movement prediction, and smart waiting at chokepoints.
		/// </summary>
		Improved
	}
}
