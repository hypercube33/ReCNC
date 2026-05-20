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

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// Classic Command &amp; Conquer aircraft landing strategy using Is_LZ_Clear + New_LZ.
	/// Ported from the original 1995 C&amp;C source code (AIRCRAFT.CPP).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The classic landing system is much simpler than OpenRA's Reservable system:
	/// - Simple Is_LZ_Clear() check: is the pad occupied or in radio contact with us?
	/// - New_LZ() fallback: scan outward in concentric rings to find an alternate pad
	/// - Uses "radio contact" model instead of complex reservation system
	/// </para>
	/// <para>
	/// Key differences from OpenRA's Reservable system:
	/// - No MayYieldReservation complexity
	/// - No repair/rearm reservation conflicts
	/// - Simple fallback to alternate landing zones
	/// </para>
	/// </remarks>
	public sealed class ClassicAircraftLanding : IAircraftLanding
	{
		const int MaxSearchRadius = 16;

		readonly World world;

		/// <summary>
		/// Tracks which aircraft are currently "in radio contact" with which landing zones.
		/// This replaces the complex Reservable system with a simple contact model.
		/// </summary>
		readonly Dictionary<Actor, Actor> radioContacts = new();

		public string Name => "Classic C&C";

		/// <summary>
		/// Creates a new classic C&amp;C aircraft landing strategy.
		/// </summary>
		/// <param name="world">The game world.</param>
		public ClassicAircraftLanding(World world)
		{
			this.world = world ?? throw new ArgumentNullException(nameof(world));
		}

		/// <inheritdoc/>
		/// <remarks>
		/// Implements classic Is_LZ_Clear logic from AIRCRAFT.CPP lines 1274-1294:
		/// - If nothing on cell, check if cell is generally clear
		/// - If aircraft itself is on cell, it's clear
		/// - If in radio contact with object on cell, it's clear
		/// - Otherwise, not clear
		/// </remarks>
		public bool IsLandingZoneClear(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead || !landingActor.IsInWorld)
				return false;

			var landingCell = world.Map.CellContaining(landingActor.CenterPosition);

			var actorsOnCell = world.ActorMap.GetActorsAt(landingCell);
			foreach (var actor in actorsOnCell)
			{
				if (actor == aircraft)
					continue;

				if (actor == landingActor)
					continue;

				if (radioContacts.TryGetValue(aircraft, out var contact) && contact == actor)
					continue;

				if (actor.TraitOrDefault<Aircraft>() != null)
					return false;

				if (actor.TraitOrDefault<Mobile>() != null)
					return false;
			}

			return IsCellGenerallyClear(landingCell, aircraft, landingActor);
		}

		/// <inheritdoc/>
		/// <remarks>
		/// Implements classic New_LZ logic from AIRCRAFT.CPP lines 2391-2423:
		/// Scans outward in concentric rings to find a clear landing zone.
		/// </remarks>
		public Actor FindAlternateLandingZone(Actor aircraft, Actor preferredLandingActor)
		{
			if (aircraft == null)
				return null;

			var aircraftTrait = aircraft.TraitOrDefault<Aircraft>();
			if (aircraftTrait == null)
				return null;

			var rearmable = aircraft.TraitOrDefault<Rearmable>();
			var validActorNames = rearmable?.Info.RearmActors;

			var searchCenter = preferredLandingActor != null
				? preferredLandingActor.CenterPosition
				: aircraft.CenterPosition;

			var searchCenterCell = world.Map.CellContaining(searchCenter);

			var potentialLandingZones = world.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead
					&& a.IsInWorld
					&& a.Owner == aircraft.Owner
					&& (validActorNames == null || validActorNames.Contains(a.Info.Name)));

			for (var radius = 0; radius < MaxSearchRadius; radius++)
			{
				foreach (var direction in CVec.Directions)
				{
					var checkCell = searchCenterCell + direction * radius;

					if (!world.Map.Contains(checkCell))
						continue;

					foreach (var landingActor in potentialLandingZones)
					{
						var landingCell = world.Map.CellContaining(landingActor.CenterPosition);
						if ((landingCell - checkCell).LengthSquared > radius * radius)
							continue;

						if (IsLandingZoneClear(aircraft, landingActor))
							return landingActor;
					}
				}
			}

			return null;
		}

		/// <inheritdoc/>
		/// <remarks>
		/// In classic C&amp;C, "reservation" is actually just establishing radio contact.
		/// The aircraft calls In_Radio_Contact() to coordinate with the helipad.
		/// </remarks>
		public IDisposable ReserveLandingZone(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null)
				return null;

			radioContacts[aircraft] = landingActor;

			return new DisposableAction(
				() => { radioContacts.Remove(aircraft); },
				() => { });
		}

		/// <inheritdoc/>
		public bool IsLandingZoneReserved(Actor landingActor)
		{
			if (landingActor == null || landingActor.IsDead)
				return true;

			return radioContacts.ContainsValue(landingActor);
		}

		/// <inheritdoc/>
		public bool IsLandingZoneAvailableFor(Actor aircraft, Actor landingActor)
		{
			if (aircraft == null || landingActor == null || landingActor.IsDead)
				return false;

			if (radioContacts.TryGetValue(aircraft, out var contact) && contact == landingActor)
				return true;

			return IsLandingZoneClear(aircraft, landingActor);
		}

		/// <summary>
		/// Checks if a cell is generally clear for landing.
		/// Based on classic C&amp;C Map[cell].Is_Generally_Clear() check.
		/// </summary>
		bool IsCellGenerallyClear(CPos cell, Actor aircraft, Actor landingActor)
		{
			if (!world.Map.Contains(cell))
				return false;

			var actorsOnCell = world.ActorMap.GetActorsAt(cell);
			foreach (var actor in actorsOnCell)
			{
				if (actor == aircraft || actor == landingActor)
					continue;

				if (actor.IsDead || !actor.IsInWorld)
					continue;

				var mobile = actor.TraitOrDefault<Mobile>();
				if (mobile != null && !mobile.IsTraitDisabled)
					return false;

				var aircraftOnCell = actor.TraitOrDefault<Aircraft>();
				if (aircraftOnCell != null && !aircraftOnCell.IsTraitDisabled)
				{
					if (aircraftOnCell.GetPosition().Z <= 0)
						return false;
				}
			}

			return true;
		}
	}
}
