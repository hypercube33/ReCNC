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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Manages pathfinding strategy instances and selection based on game settings.
	/// </summary>
	public sealed class PathfindingStrategyManager
	{
		readonly World world;
		readonly Dictionary<Locomotor, IPathfindingStrategy> openRAStrategies = new();
		readonly Dictionary<Locomotor, IPathfindingStrategy> classicStrategies = new();
		readonly Dictionary<Locomotor, IPathfindingStrategy> improvedStrategies = new();

		string lobbyAlgorithm;

		// BEGIN ReCnC PERF-011
		// Cache the resolved algorithm enum so CurrentAlgorithm does not re-parse
		// the settings string (two case-insensitive StringComparisons) on every
		// GetStrategy call. Invalidated by ClearCache / SetAlgorithmFromLobby.
		PathfindingAlgorithm? cachedAlgorithm;
		// END ReCnC PERF-011

		/// <summary>
		/// Creates a new pathfinding strategy manager for the given world.
		/// </summary>
		/// <param name="world">The game world.</param>
		public PathfindingStrategyManager(World world)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
		}

		/// <summary>
		/// Sets the algorithm from lobby selection. Called by AlgorithmLobbyOptions.
		/// </summary>
		public void SetAlgorithmFromLobby(string algorithm)
		{
			lobbyAlgorithm = algorithm;
			ClearCache();
		}

		/// <summary>
		/// Gets the currently selected pathfinding algorithm based on lobby or game settings.
		/// </summary>
		public PathfindingAlgorithm CurrentAlgorithm
		{
			get
			{
				// BEGIN ReCnC PERF-011
				if (cachedAlgorithm.HasValue)
					return cachedAlgorithm.Value;

				var setting = lobbyAlgorithm ?? Game.Settings.Game.PathfindingAlgorithm;
				PathfindingAlgorithm resolved;
				if (setting?.Equals("ClassicCnC", StringComparison.OrdinalIgnoreCase) == true)
					resolved = PathfindingAlgorithm.ClassicCnC;
				else if (setting?.Equals("Improved", StringComparison.OrdinalIgnoreCase) == true)
					resolved = PathfindingAlgorithm.Improved;
				else
					resolved = PathfindingAlgorithm.OpenRA;

				cachedAlgorithm = resolved;
				return resolved;
				// END ReCnC PERF-011
			}
		}

		/// <summary>
		/// Gets the appropriate pathfinding strategy for the given locomotor based on current settings.
		/// </summary>
		/// <param name="locomotor">The locomotor to get a strategy for.</param>
		/// <param name="hpf">The hierarchical path finder (for OpenRA and Improved strategies).</param>
		/// <returns>The pathfinding strategy to use.</returns>
		public IPathfindingStrategy GetStrategy(Locomotor locomotor, HierarchicalPathFinder hpf)
		{
			if (locomotor == null)
				throw new ArgumentNullException(nameof(locomotor));

			var algorithm = CurrentAlgorithm;

			if (algorithm == PathfindingAlgorithm.ClassicCnC)
			{
				if (!classicStrategies.TryGetValue(locomotor, out var strategy))
				{
					strategy = new ClassicPathfinder(world, locomotor, world.ActorMap);
					classicStrategies[locomotor] = strategy;
				}

				return strategy;
			}
			else if (algorithm == PathfindingAlgorithm.Improved)
			{
				if (!improvedStrategies.TryGetValue(locomotor, out var strategy))
				{
					strategy = new ImprovedPathfinder(world, locomotor, world.ActorMap, hpf);
					improvedStrategies[locomotor] = strategy;
				}

				return strategy;
			}
			else
			{
				if (hpf == null)
					throw new ArgumentNullException(nameof(hpf), "HierarchicalPathFinder required for OpenRA strategy");

				if (!openRAStrategies.TryGetValue(locomotor, out var strategy))
				{
					strategy = new OpenRAPathfinder(hpf);
					openRAStrategies[locomotor] = strategy;
				}

				return strategy;
			}
		}

		/// <summary>
		/// Registers an OpenRA strategy for a locomotor. Called during WorldLoaded.
		/// </summary>
		public void RegisterOpenRAStrategy(Locomotor locomotor, HierarchicalPathFinder hpf)
		{
			if (locomotor == null || hpf == null)
				return;

			openRAStrategies[locomotor] = new OpenRAPathfinder(hpf);
		}

		/// <summary>
		/// Clears all cached strategies. Call when settings change.
		/// </summary>
		public void ClearCache()
		{
			openRAStrategies.Clear();
			classicStrategies.Clear();
			improvedStrategies.Clear();

			// BEGIN ReCnC PERF-011
			cachedAlgorithm = null;
			// END ReCnC PERF-011
		}
	}
}
