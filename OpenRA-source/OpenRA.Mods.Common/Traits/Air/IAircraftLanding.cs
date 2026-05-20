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

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// Defines an aircraft landing zone management strategy that can be swapped at runtime.
	/// This allows switching between OpenRA's default Reservable system and classic C&amp;C landing logic.
	/// </summary>
	public interface IAircraftLanding
	{
		/// <summary>
		/// Display name for this landing strategy (e.g., "OpenRA", "Classic C&amp;C").
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Checks if a landing zone (helipad, airstrip, etc.) is clear for the given aircraft.
		/// </summary>
		/// <param name="aircraft">The aircraft attempting to land.</param>
		/// <param name="landingActor">The actor representing the landing zone (helipad, airstrip).</param>
		/// <returns>True if the landing zone is clear and available for this aircraft.</returns>
		bool IsLandingZoneClear(Actor aircraft, Actor landingActor);

		/// <summary>
		/// Finds an alternate landing zone when the preferred one is occupied.
		/// </summary>
		/// <param name="aircraft">The aircraft needing a landing zone.</param>
		/// <param name="preferredLandingActor">The preferred landing zone actor, or null to find any available.</param>
		/// <returns>An available landing zone actor, or null if none available.</returns>
		Actor FindAlternateLandingZone(Actor aircraft, Actor preferredLandingActor);

		/// <summary>
		/// Reserves a landing zone for an aircraft. The reservation should be released when landing is complete.
		/// </summary>
		/// <param name="aircraft">The aircraft making the reservation.</param>
		/// <param name="landingActor">The landing zone actor to reserve.</param>
		/// <returns>A disposable that releases the reservation when disposed.</returns>
		IDisposable ReserveLandingZone(Actor aircraft, Actor landingActor);

		/// <summary>
		/// Checks if a landing zone is currently reserved by any aircraft.
		/// </summary>
		/// <param name="landingActor">The landing zone actor to check.</param>
		/// <returns>True if the landing zone is reserved.</returns>
		bool IsLandingZoneReserved(Actor landingActor);

		/// <summary>
		/// Checks if a landing zone is available for a specific aircraft.
		/// This may differ from IsLandingZoneClear if the aircraft already has a reservation.
		/// </summary>
		/// <param name="aircraft">The aircraft to check availability for.</param>
		/// <param name="landingActor">The landing zone actor to check.</param>
		/// <returns>True if the landing zone is available for this aircraft.</returns>
		bool IsLandingZoneAvailableFor(Actor aircraft, Actor landingActor);
	}

	/// <summary>
	/// Enumerates the available aircraft landing algorithm types.
	/// </summary>
	public enum AircraftLandingAlgorithm
	{
		/// <summary>
		/// OpenRA's default Reservable system.
		/// Uses complex reservation logic with MayYieldReservation flag.
		/// Known issues: single reservation blocks all aircraft, repair conflicts with reservation.
		/// </summary>
		OpenRA,

		/// <summary>
		/// Classic Command &amp; Conquer aircraft landing from the original 1995 game.
		/// Uses simple Is_LZ_Clear check + New_LZ fallback for finding alternate pads.
		/// Uses radio contact model for helipad coordination.
		/// </summary>
		ClassicCnC,

		/// <summary>
		/// Improved aircraft landing combining the best of OpenRA and classic C&amp;C.
		/// Features queue-based landing with ETA prediction, priority queuing for damaged aircraft,
		/// and load balancing across multiple pads.
		/// </summary>
		Improved
	}
}
