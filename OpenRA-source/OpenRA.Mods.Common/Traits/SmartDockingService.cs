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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Provides intelligent dock selection that considers wait times vs travel times.",
		"Answers: 'Is waiting here faster than going somewhere else?'")]
	public class SmartDockingServiceInfo : TraitInfo
	{
		[Desc("Estimated ticks for a unit to complete docking (unload/rearm/repair).")]
		public readonly int EstimatedDockDuration = 100;

		[Desc("Movement speed estimate in cells per 100 ticks for travel time calculation.")]
		public readonly int EstimatedSpeedCellsPer100Ticks = 5;

		[Desc("Bonus multiplier for docks the unit has used before (0-100, higher = prefer familiar docks).")]
		public readonly int FamiliarityBonus = 20;

		[Desc("Extra weight for each unit in queue (in equivalent travel cells).")]
		public readonly int QueuePenaltyCells = 3;

		public override object Create(ActorInitializer init)
		{
			return new SmartDockingService(init.Self, this);
		}
	}

	/// <summary>
	/// Provides intelligent dock selection for harvesters, aircraft, and repair-seeking units.
	/// Calculates whether waiting at the current dock is faster than traveling to an alternate.
	/// </summary>
	public class SmartDockingService : IWorldLoaded
	{
		public readonly SmartDockingServiceInfo Info;
		readonly World world;

		/// <summary>
		/// Tracks estimated completion time for each dock (when it will be free).
		/// </summary>
		readonly Dictionary<IDockHost, int> dockFreeAtTick = new();

		/// <summary>
		/// Tracks queue of clients waiting at each dock.
		/// </summary>
		readonly Dictionary<IDockHost, List<DockQueueEntry>> dockQueues = new();

		/// <summary>
		/// Tracks which dock each client last used (for familiarity bonus).
		/// </summary>
		readonly Dictionary<Actor, IDockHost> lastUsedDock = new();

		public SmartDockingService(Actor self, SmartDockingServiceInfo info)
		{
			Info = info;
			world = self.World;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { }

		struct DockQueueEntry
		{
			public Actor Client;
			public int ArrivalTick;
			public int Priority;
		}

		/// <summary>
		/// Finds the best dock considering travel time + wait time.
		/// Returns null if no suitable dock is found.
		/// </summary>
		public DockDecision FindBestDock(Actor client, IEnumerable<TraitPair<IDockHost>> availableDocks, DockClientManager dockManager)
		{
			if (client == null || availableDocks == null)
				return null;

			var mobile = client.TraitOrDefault<Mobile>();
			var clientPos = client.CenterPosition;

			// BEGIN ReCnC PERF-015
			// Replace OrderBy().ToList() + list allocation with a single-pass scan
			// tracking best + runner-up. Avoids List<DockCandidate> allocation and
			// the O(n log n) sort cost on every dock selection.
			var lastDockForClient = lastUsedDock.TryGetValue(client, out var lastDock) ? lastDock : null;

			var bestValid = false;
			var secondValid = false;
			var candidateCount = 0;
			var best = default(DockCandidate);
			var second = default(DockCandidate);

			foreach (var dock in availableDocks)
			{
				if (dock.Trait == null || !dock.Trait.IsEnabledAndInWorld)
					continue;

				var travelTime = EstimateTravelTime(client, mobile, dock.Trait.DockPosition);
				var waitTime = EstimateWaitTime(dock.Trait);
				var totalTime = travelTime + waitTime;

				var familiarityBonus = lastDockForClient == dock.Trait ? Info.FamiliarityBonus : 0;

				var candidate = new DockCandidate
				{
					Dock = dock,
					TravelTime = travelTime,
					WaitTime = waitTime,
					TotalTime = totalTime,
					FamiliarityBonus = familiarityBonus,
					Score = totalTime - familiarityBonus
				};

				candidateCount++;

				if (!bestValid || candidate.Score < best.Score)
				{
					if (bestValid)
					{
						second = best;
						secondValid = true;
					}

					best = candidate;
					bestValid = true;
				}
				else if (!secondValid || candidate.Score < second.Score)
				{
					second = candidate;
					secondValid = true;
				}
			}

			if (!bestValid)
				return null;

			var decision = new DockDecision
			{
				RecommendedDock = best.Dock,
				TravelTime = best.TravelTime,
				WaitTime = best.WaitTime,
				TotalTime = best.TotalTime,
				AlternativeCount = candidateCount - 1,
				ShouldWait = true
			};

			if (secondValid)
			{
				decision.AlternativeDock = second.Dock;
				decision.AlternativeTotalTime = second.TotalTime;

				if (dockManager?.ReservedHost == best.Dock.Trait)
					decision.ShouldWait = best.TotalTime <= second.TotalTime * 1.2;
			}

			return decision;
			// END ReCnC PERF-015
		}

		/// <summary>
		/// Determines if the client should wait at current dock or go to an alternate.
		/// </summary>
		public bool ShouldWaitAtCurrentDock(Actor client, IDockHost currentDock, IEnumerable<TraitPair<IDockHost>> alternates)
		{
			if (currentDock == null || alternates == null)
				return true;

			var mobile = client.TraitOrDefault<Mobile>();
			var currentWaitTime = EstimateWaitTime(currentDock);

			foreach (var alt in alternates)
			{
				if (alt.Trait == currentDock || !alt.Trait.IsEnabledAndInWorld)
					continue;

				var travelTime = EstimateTravelTime(client, mobile, alt.Trait.DockPosition);
				var altWaitTime = EstimateWaitTime(alt.Trait);
				var altTotalTime = travelTime + altWaitTime;

				if (altTotalTime < currentWaitTime * 0.8)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Estimates travel time in ticks to reach a dock position.
		/// </summary>
		int EstimateTravelTime(Actor client, Mobile mobile, WPos dockPosition)
		{
			var distance = (client.CenterPosition - dockPosition).Length;
			var cellDistance = distance / 1024;

			if (mobile != null)
			{
				var speed = mobile.Info.Speed;
				if (speed > 0)
					return (int)(cellDistance * 100 / speed);
			}

			return cellDistance * 100 / Info.EstimatedSpeedCellsPer100Ticks;
		}

		/// <summary>
		/// Estimates wait time at a dock based on current queue and dock duration.
		/// </summary>
		int EstimateWaitTime(IDockHost dock)
		{
			if (dock == null)
				return 0;

			var baseWait = 0;
			if (dockFreeAtTick.TryGetValue(dock, out var freeAt))
				baseWait = Math.Max(0, freeAt - world.WorldTick);

			var queueCount = dock.ReservationCount;
			if (dockQueues.TryGetValue(dock, out var queue))
				queueCount = Math.Max(queueCount, queue.Count);

			return baseWait + (queueCount * Info.EstimatedDockDuration);
		}

		/// <summary>
		/// Registers that a client has started using a dock.
		/// </summary>
		public void RegisterDockStart(Actor client, IDockHost dock)
		{
			if (client == null || dock == null)
				return;

			lastUsedDock[client] = dock;
			dockFreeAtTick[dock] = world.WorldTick + Info.EstimatedDockDuration;

			if (!dockQueues.TryGetValue(dock, out var queue))
			{
				queue = new List<DockQueueEntry>();
				dockQueues[dock] = queue;
			}

			queue.RemoveAll(e => e.Client == client);
		}

		/// <summary>
		/// Registers that a client has finished using a dock.
		/// </summary>
		public void RegisterDockComplete(Actor client, IDockHost dock)
		{
			if (dock == null)
				return;

			dockFreeAtTick[dock] = world.WorldTick;

			if (dockQueues.TryGetValue(dock, out var queue))
				queue.RemoveAll(e => e.Client == client);
		}

		/// <summary>
		/// Registers that a client is queuing for a dock.
		/// </summary>
		public void RegisterQueueEntry(Actor client, IDockHost dock, int priority = 0)
		{
			if (client == null || dock == null)
				return;

			if (!dockQueues.TryGetValue(dock, out var queue))
			{
				queue = new List<DockQueueEntry>();
				dockQueues[dock] = queue;
			}

			// BEGIN ReCnC PERF-016
			// Maintain the queue in sorted order (priority desc, arrival asc) via
			// insertion sort on enqueue. Replaces the previous
			// OrderByDescending().ThenBy().ToList() allocation on every enqueue.
			var alreadyQueued = false;
			for (var i = 0; i < queue.Count; i++)
			{
				if (queue[i].Client == client)
				{
					alreadyQueued = true;
					break;
				}
			}

			if (!alreadyQueued)
			{
				var entry = new DockQueueEntry
				{
					Client = client,
					ArrivalTick = world.WorldTick,
					Priority = priority
				};

				var insertAt = queue.Count;
				for (var i = 0; i < queue.Count; i++)
				{
					var cur = queue[i];
					if (entry.Priority > cur.Priority
						|| (entry.Priority == cur.Priority && entry.ArrivalTick < cur.ArrivalTick))
					{
						insertAt = i;
						break;
					}
				}

				queue.Insert(insertAt, entry);
			}
			// END ReCnC PERF-016
		}

		/// <summary>
		/// Gets the queue position for a client at a dock (0 = next, -1 = not in queue).
		/// </summary>
		public int GetQueuePosition(Actor client, IDockHost dock)
		{
			if (client == null || dock == null)
				return -1;

			if (!dockQueues.TryGetValue(dock, out var queue))
				return -1;

			for (var i = 0; i < queue.Count; i++)
			{
				if (queue[i].Client == client)
					return i;
			}

			return -1;
		}

		struct DockCandidate
		{
			public TraitPair<IDockHost> Dock;
			public int TravelTime;
			public int WaitTime;
			public int TotalTime;
			public int FamiliarityBonus;
			public int Score;
		}
	}

	/// <summary>
	/// Result of a smart docking decision.
	/// </summary>
	public class DockDecision
	{
		/// <summary>The recommended dock to use.</summary>
		public TraitPair<IDockHost> RecommendedDock;

		/// <summary>Estimated travel time to the recommended dock.</summary>
		public int TravelTime;

		/// <summary>Estimated wait time at the recommended dock.</summary>
		public int WaitTime;

		/// <summary>Total estimated time (travel + wait).</summary>
		public int TotalTime;

		/// <summary>Number of alternative docks considered.</summary>
		public int AlternativeCount;

		/// <summary>The best alternative dock (if any).</summary>
		public TraitPair<IDockHost>? AlternativeDock;

		/// <summary>Total time for the alternative dock.</summary>
		public int AlternativeTotalTime;

		/// <summary>Whether the client should wait at current location vs. travel to alternate.</summary>
		public bool ShouldWait;
	}
}
