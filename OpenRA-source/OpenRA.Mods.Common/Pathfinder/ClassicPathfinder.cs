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
	/// Classic Command &amp; Conquer pathfinding strategy using LOS + Edge-following.
	/// Ported from the original 1995 C&amp;C source code (FINDPATH.CPP).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The algorithm works by following a line-of-sight path to the target. If it
	/// collides with an impassable spot, it uses an edge-following routine to
	/// get around it. The edge follower moves along the edge in a clockwise or
	/// counter-clockwise fashion until finding the destination spot.
	/// </para>
	/// <para>
	/// Key differences from OpenRA's HPA*:
	/// - Uses cost-based blocking (MOVE_MOVING_BLOCK = cost 3) instead of binary blocking
	/// - Moving units are passable with a penalty, not complete blockers
	/// - Simpler algorithm (~1500 lines vs 2000+)
	/// - No hierarchical abstract graph
	/// </para>
	/// </remarks>
	public sealed class ClassicPathfinder : IPathfindingStrategy
	{
		const int MaxPathLength = 300;
		const int ClockwiseRotation = 1;
		const int CounterClockwiseRotation = -1;

		readonly World world;
		readonly Locomotor locomotor;
		readonly IActorMap actorMap;

		// PERF: Pooled collections to reduce allocations during pathfinding
		readonly Stack<List<CPos>> pathPool = new();
		readonly Stack<HashSet<CPos>> visitedPool = new();

		public string Name => "Classic C&C";

		/// <summary>
		/// Creates a new classic C&amp;C pathfinder strategy.
		/// </summary>
		/// <param name="world">The game world.</param>
		/// <param name="locomotor">The locomotor for movement costs.</param>
		/// <param name="actorMap">The actor map for blocking actor checks.</param>
		public ClassicPathfinder(World world, Locomotor locomotor, IActorMap actorMap)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
			this.locomotor = locomotor ?? throw new ArgumentNullException(nameof(locomotor));
			this.actorMap = actorMap ?? throw new ArgumentNullException(nameof(actorMap));
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

			var threshold = ConvertBlockedByActorToThreshold(check);
			var path = FindPathClassic(self, source, target, threshold, customCost, ignoreActor, MaxPathLength);

			if (path == null || path.Count == 0)
				return PathFinder.NoPath;

			return path;
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

			var threshold = ConvertBlockedByActorToThreshold(check);
			List<CPos> bestPath = null;

			foreach (var source in sources)
			{
				if (!world.Map.Contains(source))
					continue;

				var path = FindPathClassic(self, source, target, threshold, customCost, ignoreActor, MaxPathLength);
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
			if (!world.Map.Contains(source) || !world.Map.Contains(target))
				return false;

			if (source == target)
				return true;

			var path = FindPathClassic(null, source, target, MoveType.MovingBlock, null, null, MaxPathLength / 2);
			return path != null && path.Count > 0;
		}

		/// <summary>
		/// Converts OpenRA's BlockedByActor enum to classic C&amp;C MoveType threshold.
		/// </summary>
		static MoveType ConvertBlockedByActorToThreshold(BlockedByActor check)
		{
			return check switch
			{
				BlockedByActor.None => MoveType.No,
				BlockedByActor.Immovable => MoveType.Temp,
				BlockedByActor.Stationary => MoveType.MovingBlock,
				BlockedByActor.All => MoveType.Ok,
				_ => MoveType.MovingBlock
			};
		}

		/// <summary>
		/// Classic C&amp;C pathfinding algorithm: LOS + Edge-following.
		/// </summary>
		List<CPos> FindPathClassic(
			Actor self,
			CPos source,
			CPos target,
			MoveType threshold,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			int maxLength)
		{
			// PERF: Use pooled collections to reduce allocations
			var path = RentPath();
			var visited = RentVisited();

			try
			{
				path.Add(source);
				visited.Add(source);
				var current = source;

				while (path.Count < maxLength)
				{
					if (current == target)
						break;

					var direction = GetDirectionToward(current, target);
					var next = GetAdjacentCell(current, direction);

					if (!world.Map.Contains(next))
						break;

					var moveType = GetCellMoveType(self, next, ignoreActor, customCost);

					if (moveType.CanEnter(threshold) && !visited.Contains(next))
					{
						path.Add(next);
						visited.Add(next);
						current = next;
					}
					else
					{
						if (next == target)
							break;

						var detour = FindDetour(self, current, target, direction, threshold, customCost, ignoreActor, visited, maxLength - path.Count);
						if (detour == null || detour.Count == 0)
							break;

						foreach (var cell in detour)
						{
							if (!visited.Contains(cell))
							{
								path.Add(cell);
								visited.Add(cell);
							}
						}

						current = detour.Count > 0 ? detour[detour.Count - 1] : default;
						if (current == default)
							break;
					}
				}

				if (path.Count <= 1)
					return null;

				path.Reverse();

				// BEGIN ReCnC PERF-009
				// Transfer ownership of the pooled list to the caller instead of
				// allocating + copying a fresh List<CPos>. Setting path = null
				// prevents the finally block from recycling it.
				var result = path;
				path = null;
				return result;
				// END ReCnC PERF-009
			}
			finally
			{
				// BEGIN ReCnC PERF-009
				if (path != null)
					ReturnPath(path);
				// END ReCnC PERF-009
				ReturnVisited(visited);
			}
		}

		/// <summary>
		/// Finds a detour around an obstacle using edge-following.
		/// Tries both clockwise and counter-clockwise, returns the shorter path.
		/// </summary>
		List<CPos> FindDetour(
			Actor self,
			CPos start,
			CPos target,
			int startDirection,
			MoveType threshold,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			HashSet<CPos> globalVisited,
			int maxLength)
		{
			var nextPassable = FindNextPassable(self, start, target, threshold, customCost, ignoreActor, globalVisited);
			if (nextPassable == null)
				return null;

			var leftPath = FollowEdge(self, start, nextPassable.Value, startDirection, CounterClockwiseRotation, threshold, customCost, ignoreActor, globalVisited, maxLength);
			var rightPath = FollowEdge(self, start, nextPassable.Value, startDirection, ClockwiseRotation, threshold, customCost, ignoreActor, globalVisited, maxLength);

			if (leftPath == null && rightPath == null)
				return null;

			if (leftPath == null)
				return rightPath;
			if (rightPath == null)
				return leftPath;

			return leftPath.Count <= rightPath.Count ? leftPath : rightPath;
		}

		/// <summary>
		/// Scans forward from an impassable cell to find the next passable cell toward the target.
		/// </summary>
		CPos? FindNextPassable(
			Actor self,
			CPos start,
			CPos target,
			MoveType threshold,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			HashSet<CPos> globalVisited)
		{
			var current = start;
			var maxIterations = MaxPathLength / 2;

			for (var i = 0; i < maxIterations; i++)
			{
				var direction = GetDirectionToward(current, target);
				var next = GetAdjacentCell(current, direction);

				if (!world.Map.Contains(next))
					return null;

				if (next == target)
					return target;

				var moveType = GetCellMoveType(self, next, ignoreActor, customCost);
				if (moveType.CanEnter(threshold) && !globalVisited.Contains(next))
					return next;

				current = next;
			}

			return null;
		}

		/// <summary>
		/// Follows the edge of an obstacle in the specified rotation direction.
		/// </summary>
		List<CPos> FollowEdge(
			Actor self,
			CPos start,
			CPos target,
			int startDirection,
			int rotation,
			MoveType threshold,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			HashSet<CPos> globalVisited,
			int maxLength)
		{
			var path = new List<CPos>();
			var localVisited = new HashSet<CPos>(globalVisited);
			var current = start;
			var direction = RotateDirection(startDirection, rotation);

			while (path.Count < maxLength)
			{
				var foundNext = false;
				var checkDirection = direction;

				for (var i = 0; i < 8; i++)
				{
					var next = GetAdjacentCell(current, checkDirection);

					if (world.Map.Contains(next) && !localVisited.Contains(next))
					{
						var moveType = GetCellMoveType(self, next, ignoreActor, customCost);
						if (moveType.CanEnter(threshold))
						{
							path.Add(next);
							localVisited.Add(next);
							current = next;
							direction = RotateDirection(checkDirection, -rotation * 2);
							foundNext = true;

							if (current == target)
								return path;

							break;
						}
					}

					checkDirection = RotateDirection(checkDirection, rotation);
				}

				if (!foundNext)
					break;

				if (current == start)
					break;
			}

			if (path.Count > 0 && current != target)
			{
				var directPath = TryDirectPath(self, current, target, threshold, customCost, ignoreActor, localVisited, maxLength - path.Count);
				if (directPath != null)
					path.AddRange(directPath);
			}

			return path.Count > 0 ? path : null;
		}

		/// <summary>
		/// Attempts a direct line-of-sight path from current position to target.
		/// </summary>
		List<CPos> TryDirectPath(
			Actor self,
			CPos start,
			CPos target,
			MoveType threshold,
			Func<CPos, int> customCost,
			Actor ignoreActor,
			HashSet<CPos> visited,
			int maxLength)
		{
			var path = new List<CPos>();
			var current = start;

			while (path.Count < maxLength)
			{
				if (current == target)
					break;

				var direction = GetDirectionToward(current, target);
				var next = GetAdjacentCell(current, direction);

				if (!world.Map.Contains(next) || visited.Contains(next))
					return null;

				var moveType = GetCellMoveType(self, next, ignoreActor, customCost);
				if (!moveType.CanEnter(threshold))
					return null;

				path.Add(next);
				visited.Add(next);
				current = next;
			}

			return path;
		}

		/// <summary>
		/// Gets the MoveType for a cell, combining terrain and actor blocking.
		/// </summary>
		MoveType GetCellMoveType(Actor self, CPos cell, Actor ignoreActor, Func<CPos, int> customCost)
		{
			if (customCost != null)
			{
				var cost = customCost(cell);
				if (cost == PathGraph.PathCostForInvalidPath)
					return MoveType.No;
			}

			var terrainCost = locomotor.MovementCostForCell(cell);
			if (terrainCost == PathGraph.MovementCostForUnreachableCell)
				return MoveType.No;

			var actorsAt = actorMap.GetActorsAt(cell);
			var hasBlockingActor = false;
			var hasMovingActor = false;
			var hasFriendlyActor = false;
			var hasEnemyActor = false;

			foreach (var actor in actorsAt)
			{
				if (actor == self || actor == ignoreActor)
					continue;

				if (!actor.IsInWorld || actor.IsDead)
					continue;

				var mobile = actor.TraitOrDefault<Mobile>();
				if (mobile != null && !mobile.IsTraitDisabled && !mobile.IsTraitPaused)
				{
					if (mobile.CurrentMovementTypes != MovementType.None)
						hasMovingActor = true;
					else
						hasBlockingActor = true;
				}
				else
				{
					hasBlockingActor = true;
				}

				if (self != null && actor.Owner != null)
				{
					if (self.Owner.RelationshipWith(actor.Owner).HasFlag(PlayerRelationship.Ally))
						hasFriendlyActor = true;
					else
						hasEnemyActor = true;
				}
			}

			if (hasMovingActor && !hasBlockingActor)
				return MoveType.MovingBlock;

			if (hasEnemyActor)
				return MoveType.Destroyable;

			if (hasFriendlyActor || hasBlockingActor)
				return MoveType.Temp;

			return MoveType.Ok;
		}

		/// <summary>
		/// Gets the direction (0-7) from source to target cell.
		/// 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
		/// </summary>
		static int GetDirectionToward(CPos source, CPos target)
		{
			var dx = target.X - source.X;
			var dy = target.Y - source.Y;

			if (dx == 0 && dy < 0) return 0;
			if (dx > 0 && dy < 0) return 1;
			if (dx > 0 && dy == 0) return 2;
			if (dx > 0 && dy > 0) return 3;
			if (dx == 0 && dy > 0) return 4;
			if (dx < 0 && dy > 0) return 5;
			if (dx < 0 && dy == 0) return 6;
			if (dx < 0 && dy < 0) return 7;

			return 0;
		}

		/// <summary>
		/// Gets the adjacent cell in the given direction.
		/// </summary>
		static CPos GetAdjacentCell(CPos cell, int direction)
		{
			var offset = direction switch
			{
				0 => new CVec(0, -1),
				1 => new CVec(1, -1),
				2 => new CVec(1, 0),
				3 => new CVec(1, 1),
				4 => new CVec(0, 1),
				5 => new CVec(-1, 1),
				6 => new CVec(-1, 0),
				7 => new CVec(-1, -1),
				_ => CVec.Zero
			};

			return cell + offset;
		}

		/// <summary>
		/// Rotates a direction by the given amount (positive = clockwise).
		/// </summary>
		static int RotateDirection(int direction, int rotation)
		{
			return ((direction + rotation) % 8 + 8) % 8;
		}

		#region Collection Pooling

		List<CPos> RentPath()
		{
			if (pathPool.Count > 0)
			{
				var list = pathPool.Pop();
				list.Clear();
				return list;
			}

			return new List<CPos>(MaxPathLength);
		}

		void ReturnPath(List<CPos> list)
		{
			if (pathPool.Count < 4)
				pathPool.Push(list);
		}

		HashSet<CPos> RentVisited()
		{
			if (visitedPool.Count > 0)
			{
				var set = visitedPool.Pop();
				set.Clear();
				return set;
			}

			return new HashSet<CPos>(MaxPathLength);
		}

		void ReturnVisited(HashSet<CPos> set)
		{
			if (visitedPool.Count < 4)
				visitedPool.Push(set);
		}

		#endregion
	}
}
