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
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// OpenRA's default aircraft landing strategy using the Reservable system.
	/// This is a wrapper around the existing landing logic that implements
	/// <see cref="IAircraftLanding"/> to allow runtime algorithm switching.
	/// </summary>
	/// <remarks>
	/// This wrapper preserves all existing OpenRA landing behavior, including known issues:
	/// - Single reservation blocks all other aircraft from landing
	/// - Repair logic conflicts with reservation system (HACK in Resupply.cs)
	/// - MayYieldReservation flag complexity
	/// </remarks>
	public sealed class OpenRAAircraftLanding : IAircraftLanding
	{
		readonly World world;

		public string Name => "OpenRA";

		/// <summary>
		/// Creates a new OpenRA aircraft landing strategy.
		/// </summary>
		/// <param name="world">The game world.</param>
		public OpenRAAircraftLanding(World world)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
		}

		/// <inheritdoc/>
		public bool IsLandingZoneClear(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead)
				return false;

			return !Reservable.IsReserved(landingActor);
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
			if (rearmable == null)
				return null;

			var rearmInfo = rearmable.Info;

			// Find closest available resupply point
			return world.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead
					&& a.IsInWorld
					&& a.Owner == aircraft.Owner
					&& rearmInfo.RearmActors.Contains(a.Info.Name)
					&& IsLandingZoneAvailableFor(aircraft, a))
				.ClosestToWithPathFrom(aircraft);
		}

		/// <inheritdoc/>
		public IDisposable ReserveLandingZone(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null)
				return null;

			var aircraftTrait = aircraft.TraitOrDefault<Aircraft>();
			if (aircraftTrait == null)
				return null;

			var reservable = landingActor.TraitOrDefault<Reservable>();
			if (reservable == null)
				return null;

			return reservable.Reserve(landingActor, aircraft, aircraftTrait);
		}

		/// <inheritdoc/>
		public bool IsLandingZoneReserved(Actor landingActor)
		{
			if (landingActor == null || landingActor.IsDead)
				return true;

			return Reservable.IsReserved(landingActor);
		}

		/// <inheritdoc/>
		public bool IsLandingZoneAvailableFor(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead)
				return false;

			return Reservable.IsAvailableFor(landingActor, aircraft);
		}
	}
}
