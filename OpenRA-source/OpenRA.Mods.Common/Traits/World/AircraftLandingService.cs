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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Provides aircraft landing zone management services. Supports switching between OpenRA and Classic C&C algorithms.")]
	public class AircraftLandingServiceInfo : TraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new AircraftLandingService(init.Self);
		}
	}

	/// <summary>
	/// World trait that provides aircraft landing zone management services.
	/// Allows switching between OpenRA's Reservable system and classic C&amp;C Is_LZ_Clear logic.
	/// </summary>
	public class AircraftLandingService : IWorldLoaded
	{
		readonly World world;
		AircraftLandingManager landingManager;

		// BEGIN ReCnC PERF-020
		// Cache the active IAircraftLanding so hot callers (IsLandingZoneClear /
		// IsLandingZoneAvailableFor / FindAlternateLandingZone) skip the
		// CurrentAlgorithm string-compare + branch dispatch inside the manager on
		// every call. Invalidated when the algorithm selection changes.
		IAircraftLanding cachedStrategy;
		AircraftLandingAlgorithm cachedAlgorithm;
		// END ReCnC PERF-020

		public AircraftLandingService(Actor self)
		{
			world = self.World;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			landingManager = new AircraftLandingManager(world);
		}

		/// <summary>
		/// Gets the current aircraft landing algorithm being used.
		/// </summary>
		public AircraftLandingAlgorithm CurrentAlgorithm => landingManager?.CurrentAlgorithm ?? AircraftLandingAlgorithm.OpenRA;

		/// <summary>
		/// Gets the current landing strategy based on game settings.
		/// </summary>
		public IAircraftLanding GetStrategy()
		{
			// BEGIN ReCnC PERF-020
			if (landingManager == null)
				return cachedStrategy ??= new OpenRAAircraftLanding(world);

			var current = landingManager.CurrentAlgorithm;
			if (cachedStrategy != null && current == cachedAlgorithm)
				return cachedStrategy;

			cachedStrategy = landingManager.GetStrategy();
			cachedAlgorithm = current;
			return cachedStrategy;
			// END ReCnC PERF-020
		}

		/// <summary>
		/// Checks if a landing zone is clear for an aircraft.
		/// </summary>
		public bool IsLandingZoneClear(Actor aircraft, Actor landingActor)
		{
			return GetStrategy().IsLandingZoneClear(aircraft, landingActor);
		}

		/// <summary>
		/// Finds an alternate landing zone for an aircraft.
		/// </summary>
		public Actor FindAlternateLandingZone(Actor aircraft, Actor preferredLandingActor)
		{
			return GetStrategy().FindAlternateLandingZone(aircraft, preferredLandingActor);
		}

		/// <summary>
		/// Checks if a landing zone is available for a specific aircraft.
		/// </summary>
		public bool IsLandingZoneAvailableFor(Actor aircraft, Actor landingActor)
		{
			return GetStrategy().IsLandingZoneAvailableFor(aircraft, landingActor);
		}
	}
}
