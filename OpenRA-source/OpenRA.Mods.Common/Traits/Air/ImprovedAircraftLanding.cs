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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// Improved aircraft landing strategy that combines the best of OpenRA and classic C&amp;C.
	/// </summary>
	/// <remarks>
	/// <para>Key improvements over OpenRA's Reservable system:</para>
	/// <list type="bullet">
	/// <item>Queue-based landing with ETA prediction</item>
	/// <item>Smarter alternate pad selection based on distance and queue length</item>
	/// <item>No reservation conflicts with repair logic</item>
	/// <item>Multiple aircraft can queue for same pad</item>
	/// </list>
	/// <para>Key improvements over classic C&amp;C:</para>
	/// <list type="bullet">
	/// <item>Predictive pad selection (knows when a pad will be free)</item>
	/// <item>Priority-based queuing (damaged aircraft get priority)</item>
	/// <item>Load balancing across multiple pads</item>
	/// </list>
	/// </remarks>
	public sealed class ImprovedAircraftLanding : IAircraftLanding
	{
		const int MaxQueuePerPad = 3;
		const int TicksToLand = 50;

		readonly World world;

		/// <summary>
		/// Landing queue for each pad. Aircraft queue up and land in order.
		/// </summary>
		// BEGIN ReCnC PERF-014
		// Use List<LandingRequest> kept in priority-desc / arrival-asc order via
		// insertion-sort. Replaces Queue<T> + ToList().OrderBy().ThenBy().ToList()
		// re-sort on every reserve/release (MaxQueuePerPad is only 3 but these
		// run on every helipad contention).
		readonly Dictionary<Actor, List<LandingRequest>> landingQueues = new();
		// END ReCnC PERF-014

		/// <summary>
		/// Estimated time when each pad will be free.
		/// </summary>
		readonly Dictionary<Actor, int> padFreeAtTick = new();

		/// <summary>
		/// Current occupant of each pad (if any).
		/// </summary>
		readonly Dictionary<Actor, Actor> padOccupants = new();

		public string Name => "Improved";

		struct LandingRequest
		{
			public Actor Aircraft;
			public int RequestTick;
			public int Priority;
		}

		public ImprovedAircraftLanding(World world)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
		}

		/// <inheritdoc/>
		public bool IsLandingZoneClear(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead || !landingActor.IsInWorld)
				return false;

			if (padOccupants.TryGetValue(landingActor, out var occupant))
			{
				if (occupant == aircraft)
					return true;

				if (occupant != null && occupant.IsInWorld && !occupant.IsDead)
					return false;

				padOccupants.Remove(landingActor);
			}

			if (landingQueues.TryGetValue(landingActor, out var queue) && queue.Count > 0)
			{
				// BEGIN ReCnC PERF-014
				var next = queue[0];
				// END ReCnC PERF-014
				return next.Aircraft == aircraft;
			}

			return IsCellClear(aircraft, landingActor);
		}

		/// <inheritdoc/>
		public Actor FindAlternateLandingZone(Actor aircraft, Actor preferredLandingActor)
		{
			if (aircraft == null)
				return null;

			var aircraftTrait = aircraft.TraitOrDefault<Aircraft>();
			if (aircraftTrait == null)
				return null;

			var rearmable = aircraft.TraitOrDefault<Rearmable>();
			var validActorNames = rearmable?.Info.RearmActors;

			var candidates = world.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead
					&& a.IsInWorld
					&& a.Owner == aircraft.Owner
					&& (validActorNames == null || validActorNames.Contains(a.Info.Name)))
				.ToList();

			if (candidates.Count == 0)
				return null;

			var isDamaged = aircraft.TraitOrDefault<IHealth>()?.DamageState >= DamageState.Medium;
			var priority = isDamaged ? 2 : 1;

			Actor bestPad = null;
			var bestScore = int.MaxValue;

			foreach (var pad in candidates)
			{
				var score = CalculatePadScore(aircraft, pad, priority);
				if (score < bestScore)
				{
					bestScore = score;
					bestPad = pad;
				}
			}

			return bestPad;
		}

		/// <summary>
		/// Calculates a score for a landing pad. Lower is better.
		/// Considers distance, queue length, and estimated wait time.
		/// </summary>
		int CalculatePadScore(Actor aircraft, Actor pad, int priority)
		{
			var distance = (aircraft.CenterPosition - pad.CenterPosition).Length;
			var distanceScore = distance / 1024;

			var queueLength = 0;
			if (landingQueues.TryGetValue(pad, out var queue))
				queueLength = queue.Count;

			var queueScore = queueLength * 100;

			var waitTime = 0;
			if (padFreeAtTick.TryGetValue(pad, out var freeAt))
				waitTime = Math.Max(0, freeAt - world.WorldTick);

			var waitScore = waitTime;

			if (padOccupants.TryGetValue(pad, out var occupant) && occupant == aircraft)
				return 0;

			// BEGIN ReCnC PERF-014
			if (queue != null)
			{
				for (var i = 0; i < queue.Count; i++)
				{
					if (queue[i].Aircraft == aircraft)
						return distanceScore;
				}
			}
			// END ReCnC PERF-014

			return distanceScore + queueScore + waitScore - (priority * 50);
		}

		/// <inheritdoc/>
		public IDisposable ReserveLandingZone(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null)
				return null;

			var isDamaged = aircraft.TraitOrDefault<IHealth>()?.DamageState >= DamageState.Medium;
			var priority = isDamaged ? 2 : 1;

			// BEGIN ReCnC PERF-014
			if (!landingQueues.TryGetValue(landingActor, out var queue))
			{
				queue = new List<LandingRequest>(MaxQueuePerPad);
				landingQueues[landingActor] = queue;
			}

			var alreadyQueued = false;
			for (var i = 0; i < queue.Count; i++)
			{
				if (queue[i].Aircraft == aircraft)
				{
					alreadyQueued = true;
					break;
				}
			}

			if (!alreadyQueued && queue.Count < MaxQueuePerPad)
			{
				var request = new LandingRequest
				{
					Aircraft = aircraft,
					RequestTick = world.WorldTick,
					Priority = priority
				};

				// Insertion-sorted: priority desc, then arrival asc.
				var insertAt = queue.Count;
				for (var i = 0; i < queue.Count; i++)
				{
					var cur = queue[i];
					if (request.Priority > cur.Priority
						|| (request.Priority == cur.Priority && request.RequestTick < cur.RequestTick))
					{
						insertAt = i;
						break;
					}
				}

				queue.Insert(insertAt, request);
			}
			// END ReCnC PERF-014

			return new DisposableAction(
				() => ReleaseLandingZoneInternal(aircraft, landingActor),
				() => { });
		}

		void ReleaseLandingZoneInternal(Actor aircraft, Actor landingActor)
		{
			if (padOccupants.TryGetValue(landingActor, out var occupant) && occupant == aircraft)
			{
				padOccupants.Remove(landingActor);
				padFreeAtTick[landingActor] = world.WorldTick;
			}

			// BEGIN ReCnC PERF-014
			if (landingQueues.TryGetValue(landingActor, out var queue))
				queue.RemoveAll(r => r.Aircraft == aircraft);
			// END ReCnC PERF-014
		}

		/// <inheritdoc/>
		public bool IsLandingZoneReserved(Actor landingActor)
		{
			if (landingActor == null || landingActor.IsDead)
				return true;

			if (padOccupants.TryGetValue(landingActor, out var occupant))
			{
				if (occupant != null && occupant.IsInWorld && !occupant.IsDead)
					return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public bool IsLandingZoneAvailableFor(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead)
				return false;

			if (padOccupants.TryGetValue(landingActor, out var occupant) && occupant == aircraft)
				return true;

			// BEGIN ReCnC PERF-014
			if (landingQueues.TryGetValue(landingActor, out var queue))
			{
				var containsAircraft = false;
				for (var i = 0; i < queue.Count; i++)
				{
					if (queue[i].Aircraft == aircraft)
					{
						containsAircraft = true;
						break;
					}
				}

				if (queue.Count >= MaxQueuePerPad && !containsAircraft)
					return false;

				if (queue.Count > 0)
				{
					var next = queue[0];
					if (next.Aircraft == aircraft)
						return !IsLandingZoneReserved(landingActor) || padOccupants.GetValueOrDefault(landingActor) == aircraft;
				}
			}
			// END ReCnC PERF-014

			return true;
		}

		bool IsCellClear(Actor aircraft, Actor landingActor)
		{
			var landingCell = world.Map.CellContaining(landingActor.CenterPosition);
			var actorsOnCell = world.ActorMap.GetActorsAt(landingCell);

			foreach (var actor in actorsOnCell)
			{
				if (actor == aircraft || actor == landingActor)
					continue;

				if (!actor.IsInWorld || actor.IsDead)
					continue;

				var otherAircraft = actor.TraitOrDefault<Aircraft>();
				if (otherAircraft != null && otherAircraft.GetPosition().Z <= 0)
					return false;

				var mobile = actor.TraitOrDefault<Mobile>();
				if (mobile != null && !mobile.IsTraitDisabled)
					return false;
			}

			return true;
		}
	}
}
