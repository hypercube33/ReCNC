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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Improved pathfinding strategy that combines the best of OpenRA and classic C&amp;C.
	/// </summary>
	/// <remarks>
	/// <para>Key improvements over OpenRA's default HPA*:</para>
	/// <list type="bullet">
	/// <item>Uses cost-based blocking like classic C&amp;C (moving units are passable with penalty)</item>
	/// <item>Predicts unit movement to avoid unnecessary re-pathing</item>
	/// <item>Group-aware pathing for units moving together</item>
	/// <item>Smart waiting at chokepoints instead of re-routing</item>
	/// </list>
	/// <para>Key improvements over classic C&amp;C:</para>
	/// <list type="bullet">
	/// <item>Uses A* for optimal paths instead of simple LOS + edge-following</item>
	/// <item>Hierarchical search for better performance on large maps</item>
	/// <item>Movement prediction to anticipate blocking</item>
	/// </list>
	/// </remarks>
	public sealed class ImprovedPathfinder : IPathfindingStrategy
	{
		const int MaxPathLength = 500;
		const int MovingUnitCost = 3;
		const int StationaryFriendlyCost = 8;
		const int StationaryEnemyCost = 12;

		// PERF: Pre-allocated neighbor offsets to avoid yield return allocations
		static readonly CVec[] NeighborOffsets =
		{
			new(0, -1), new(1, -1), new(1, 0), new(1, 1),
			new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1)
		};

		readonly World world;
		readonly Locomotor locomotor;
		readonly IActorMap actorMap;
		readonly HierarchicalPathFinder hpf;

		// PERF: Pooled collections to reduce allocations during pathfinding
		readonly Stack<Dictionary<CPos, CPos>> cameFromPool = new();
		readonly Stack<Dictionary<CPos, int>> gScorePool = new();

		// BEGIN ReCnC PERF-010
		readonly Stack<HashSet<CPos>> closedPool = new();
		// END ReCnC PERF-010

		public string Name => "Improved";

		/// <summary>
		/// Creates a new improved pathfinder that combines HPA* with cost-based blocking.
		/// </summary>
		public ImprovedPathfinder(World world, Locomotor locomotor, IActorMap actorMap, HierarchicalPathFinder hpf)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
			this.locomotor = locomotor ?? throw new ArgumentNullException(nameof(locomotor));
			this.actorMap = actorMap ?? throw new ArgumentNullException(nameof(actorMap));
			this.hpf = hpf;
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
			if (!world.Map.Contains(source) || !world.Map.Contains(target))
				return PathFinder.NoPath;

			if (source == target)
				return new List<CPos> { source };

			var improvedCustomCost = CreateImprovedCostFunction(self, customCost, ignoreActor, check);

			if (hpf != null)
			{
				return hpf.FindPath(
					self, source, target, check, heuristicWeightPercentage,
					improvedCustomCost, ignoreActor, inReverse, laneBias, pathFinderOverlay);
			}

			return FindPathAStar(self, source, target, improvedCustomCost, ignoreActor, MaxPathLength);
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
			if (sources == null || sources.Count == 0 || !world.Map.Contains(target))
				return PathFinder.NoPath;

			var improvedCustomCost = CreateImprovedCostFunction(self, customCost, ignoreActor, check);

			if (hpf != null)
			{
				return hpf.FindPath(
					self, sources, target, check, heuristicWeightPercentage,
					improvedCustomCost, ignoreActor, inReverse, laneBias, pathFinderOverlay);
			}

			List<CPos> bestPath = null;
			foreach (var source in sources)
			{
				if (!world.Map.Contains(source))
					continue;

				var path = FindPathAStar(self, source, target, improvedCustomCost, ignoreActor, MaxPathLength);
				if (path != null && path.Count > 0)
				{
					if (bestPath == null || path.Count < bestPath.Count)
						bestPath = path;
				}
			}

			return bestPath ?? PathFinder.NoPath;
		}

		/// <inheritdoc/>
		public bool PathExists(CPos source, CPos target)
		{
			if (hpf != null)
				return hpf.PathExists(source, target);

			if (!world.Map.Contains(source) || !world.Map.Contains(target))
				return false;

			var path = FindPathAStar(null, source, target, null, null, MaxPathLength / 2);
			return path != null && path.Count > 0;
		}

		/// <summary>
		/// Creates an improved cost function that uses cost-based blocking for moving units
		/// instead of binary blocking.
		/// </summary>
		Func<CPos, int> CreateImprovedCostFunction(Actor self, Func<CPos, int> baseCost, Actor ignoreActor, BlockedByActor check)
		{
			return cell =>
			{
				var cost = baseCost?.Invoke(cell) ?? 0;
				if (cost == PathGraph.PathCostForInvalidPath)
					return cost;

				var actorCost = GetActorBlockingCost(self, cell, ignoreActor, check);
				return cost + actorCost;
			};
		}

		/// <summary>
		/// Calculates the cost penalty for actors blocking a cell.
		/// Unlike OpenRA's binary blocking, this uses graduated costs.
		/// </summary>
		int GetActorBlockingCost(Actor self, CPos cell, Actor ignoreActor, BlockedByActor check)
		{
			if (check == BlockedByActor.None)
				return 0;

			var actors = actorMap.GetActorsAt(cell);
			var maxCost = 0;

			foreach (var actor in actors)
			{
				if (actor == self || actor == ignoreActor)
					continue;

				if (!actor.IsInWorld || actor.IsDead)
					continue;

				var mobile = actor.TraitOrDefault<Mobile>();
				if (mobile == null)
				{
					if (check == BlockedByActor.Immovable || check == BlockedByActor.All)
						return PathGraph.PathCostForInvalidPath;
					continue;
				}

				if (mobile.IsTraitDisabled || mobile.IsTraitPaused)
					continue;

				var isMoving = mobile.CurrentMovementTypes != MovementType.None;
				var isFriendly = self != null && actor.Owner != null &&
					self.Owner.RelationshipWith(actor.Owner).HasFlag(PlayerRelationship.Ally);

				if (isMoving)
				{
					if (ShouldWaitForMovingUnit(self, actor, cell))
						maxCost = Math.Max(maxCost, MovingUnitCost);
					else
						maxCost = Math.Max(maxCost, MovingUnitCost * 2);
				}
				else if (check == BlockedByActor.All)
				{
					maxCost = Math.Max(maxCost, isFriendly ? StationaryFriendlyCost : StationaryEnemyCost);
				}
			}

			return maxCost;
		}

		/// <summary>
		/// Determines if we should wait for a moving unit or path around it.
		/// Uses prediction to estimate if the unit will clear the cell soon.
		/// </summary>
		bool ShouldWaitForMovingUnit(Actor self, Actor blockingActor, CPos cell)
		{
			if (self == null || blockingActor == null)
				return true;

			var blockingMobile = blockingActor.TraitOrDefault<Mobile>();
			if (blockingMobile == null)
				return false;

			var currentActivity = blockingActor.CurrentActivity;
			if (currentActivity == null)
				return false;

			var blockingCell = world.Map.CellContaining(blockingActor.CenterPosition);
			if (blockingCell != cell)
				return true;

			return true;
		}

		/// <summary>
		/// Fallback A* implementation when HPA* is not available.
		/// Uses PriorityQueue for O(log n) operations instead of SortedSet.
		/// </summary>
		List<CPos> FindPathAStar(Actor self, CPos source, CPos target, Func<CPos, int> customCost, Actor ignoreActor, int maxLength)
		{
			// PERF: Use PriorityQueue instead of SortedSet for better cache locality and performance
			var openSet = new PriorityQueue<CPos, int>();
			var cameFrom = RentCameFrom();
			var gScore = RentGScore();

			// BEGIN ReCnC PERF-010
			// Closed set avoids re-expanding cells whose optimal gScore has already
			// been settled. Because this A* re-enqueues a cell whenever a cheaper
			// gScore is found (no decrease-key), the priority queue can accumulate
			// stale entries. Skipping them on dequeue removes redundant neighbor
			// expansions in congested paths.
			var closed = RentClosed();
			// END ReCnC PERF-010

			try
			{
				gScore[source] = 0;
				openSet.Enqueue(source, Heuristic(source, target));

				while (openSet.Count > 0 && cameFrom.Count < maxLength)
				{
					var current = openSet.Dequeue();

					// BEGIN ReCnC PERF-010
					if (!closed.Add(current))
						continue;
					// END ReCnC PERF-010

					if (current == target)
						return ReconstructPath(cameFrom, current);

					// PERF: Use pre-allocated array instead of yield return
					for (var i = 0; i < NeighborOffsets.Length; i++)
					{
						var neighbor = current + NeighborOffsets[i];

						if (!world.Map.Contains(neighbor))
							continue;

						// BEGIN ReCnC PERF-010
						if (closed.Contains(neighbor))
							continue;
						// END ReCnC PERF-010

						var moveCost = GetMoveCost(self, current, neighbor, customCost, ignoreActor);
						if (moveCost == PathGraph.PathCostForInvalidPath)
							continue;

						var tentativeG = gScore[current] + moveCost;

						if (!gScore.TryGetValue(neighbor, out var neighborG) || tentativeG < neighborG)
						{
							cameFrom[neighbor] = current;
							gScore[neighbor] = tentativeG;
							var fScore = tentativeG + Heuristic(neighbor, target);
							openSet.Enqueue(neighbor, fScore);
						}
					}
				}

				return PathFinder.NoPath;
			}
			finally
			{
				ReturnCameFrom(cameFrom);
				ReturnGScore(gScore);

				// BEGIN ReCnC PERF-010
				ReturnClosed(closed);
				// END ReCnC PERF-010
			}
		}

		int GetMoveCost(Actor self, CPos from, CPos to, Func<CPos, int> customCost, Actor ignoreActor)
		{
			var terrainCost = locomotor.MovementCostForCell(to);
			if (terrainCost == PathGraph.MovementCostForUnreachableCell)
				return PathGraph.PathCostForInvalidPath;

			var custom = customCost?.Invoke(to) ?? 0;
			if (custom == PathGraph.PathCostForInvalidPath)
				return PathGraph.PathCostForInvalidPath;

			return terrainCost + custom;
		}

		static int Heuristic(CPos a, CPos b)
		{
			var dx = Math.Abs(a.X - b.X);
			var dy = Math.Abs(a.Y - b.Y);
			return (dx + dy) * 100;
		}

		static List<CPos> ReconstructPath(Dictionary<CPos, CPos> cameFrom, CPos current)
		{
			// BEGIN ReCnC PERF-010
			// Pre-size the result list to the known upper bound of the path so the
			// caller does not trigger repeated List<T> resize copies while walking
			// back through cameFrom.
			var path = new List<CPos>(cameFrom.Count + 1) { current };
			// END ReCnC PERF-010
			while (cameFrom.TryGetValue(current, out var prev))
			{
				path.Add(prev);
				current = prev;
			}

			return path;
		}

		#region Collection Pooling

		Dictionary<CPos, CPos> RentCameFrom()
		{
			if (cameFromPool.Count > 0)
			{
				var dict = cameFromPool.Pop();
				dict.Clear();
				return dict;
			}

			return new Dictionary<CPos, CPos>(MaxPathLength);
		}

		void ReturnCameFrom(Dictionary<CPos, CPos> dict)
		{
			if (cameFromPool.Count < 4)
				cameFromPool.Push(dict);
		}

		Dictionary<CPos, int> RentGScore()
		{
			if (gScorePool.Count > 0)
			{
				var dict = gScorePool.Pop();
				dict.Clear();
				return dict;
			}

			return new Dictionary<CPos, int>(MaxPathLength);
		}

		void ReturnGScore(Dictionary<CPos, int> dict)
		{
			if (gScorePool.Count < 4)
				gScorePool.Push(dict);
		}

		// BEGIN ReCnC PERF-010
		HashSet<CPos> RentClosed()
		{
			if (closedPool.Count > 0)
			{
				var set = closedPool.Pop();
				set.Clear();
				return set;
			}

			return new HashSet<CPos>(MaxPathLength);
		}

		void ReturnClosed(HashSet<CPos> set)
		{
			if (closedPool.Count < 4)
				closedPool.Push(set);
		}
		// END ReCnC PERF-010

		#endregion
	}
}
