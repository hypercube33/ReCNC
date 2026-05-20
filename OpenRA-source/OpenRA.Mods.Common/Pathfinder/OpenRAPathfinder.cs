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
	/// OpenRA's default pathfinding strategy using Hierarchical A* (HPA*).
	/// This is a wrapper around the existing <see cref="HierarchicalPathFinder"/> that implements
	/// <see cref="IPathfindingStrategy"/> to allow runtime algorithm switching.
	/// </summary>
	/// <remarks>
	/// This wrapper preserves all existing OpenRA pathfinding behavior, including known issues:
	/// - Moving units are treated as complete blockers (BlockedByActor.All)
	/// - Units may re-path around other moving units on bridges/chokepoints
	/// - Uses abstract graph hierarchy for improved heuristics
	/// </remarks>
	public sealed class OpenRAPathfinder : IPathfindingStrategy
	{
		readonly HierarchicalPathFinder hierarchicalPathFinder;

		public string Name => "OpenRA";

		/// <summary>
		/// Creates a new OpenRA pathfinder strategy wrapping the given hierarchical path finder.
		/// </summary>
		/// <param name="hierarchicalPathFinder">The existing HPA* implementation to wrap.</param>
		public OpenRAPathfinder(HierarchicalPathFinder hierarchicalPathFinder)
		{
			this.hierarchicalPathFinder = hierarchicalPathFinder ?? throw new ArgumentNullException(nameof(hierarchicalPathFinder));
		}

		/// <inheritdoc/>
		public List<CPos> FindPath(
			Actor self,
			CPos source,
			CPos target,
			BlockedByActor check,
			int heuristicWeightPercentage,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			bool inReverse,
			bool laneBias,
			PathFinderOverlay pathFinderOverlay)
		{
			return hierarchicalPathFinder.FindPath(
				self,
				source,
				target,
				check,
				heuristicWeightPercentage,
				customCost,
				ignoreActor,
				inReverse,
				laneBias,
				pathFinderOverlay);
		}

		/// <inheritdoc/>
		public List<CPos> FindPath(
			Actor self,
			IReadOnlyCollection<CPos> sources,
			CPos target,
			BlockedByActor check,
			int heuristicWeightPercentage,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			bool inReverse,
			bool laneBias,
			PathFinderOverlay pathFinderOverlay)
		{
			return hierarchicalPathFinder.FindPath(
				self,
				sources,
				target,
				check,
				heuristicWeightPercentage,
				customCost,
				ignoreActor,
				inReverse,
				laneBias,
				pathFinderOverlay);
		}

		/// <inheritdoc/>
		public bool PathExists(CPos source, CPos target)
		{
			return hierarchicalPathFinder.PathExists(source, target);
		}
	}
}
