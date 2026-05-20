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
	/// Manages aircraft landing strategy instances and selection based on game settings.
	/// </summary>
	public sealed class AircraftLandingManager
	{
		readonly World world;
		IAircraftLanding openRAStrategy;
		IAircraftLanding classicStrategy;
		IAircraftLanding improvedStrategy;

		string lobbyAlgorithm;

		/// <summary>
		/// Creates a new aircraft landing strategy manager for the given world.
		/// </summary>
		/// <param name="world">The game world.</param>
		public AircraftLandingManager(World world)
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
		/// Gets the currently selected aircraft landing algorithm based on lobby or game settings.
		/// </summary>
		public AircraftLandingAlgorithm CurrentAlgorithm
		{
			get
			{
				var setting = lobbyAlgorithm ?? Game.Settings.Game.AircraftLandingAlgorithm;
				if (setting?.Equals("ClassicCnC", StringComparison.OrdinalIgnoreCase) == true)
					return AircraftLandingAlgorithm.ClassicCnC;
				if (setting?.Equals("Improved", StringComparison.OrdinalIgnoreCase) == true)
					return AircraftLandingAlgorithm.Improved;
				return AircraftLandingAlgorithm.OpenRA;
			}
		}

		/// <summary>
		/// Gets the appropriate aircraft landing strategy based on current settings.
		/// </summary>
		/// <returns>The aircraft landing strategy to use.</returns>
		public IAircraftLanding GetStrategy()
		{
			var algorithm = CurrentAlgorithm;

			if (algorithm == AircraftLandingAlgorithm.ClassicCnC)
			{
				classicStrategy ??= new ClassicAircraftLanding(world);
				return classicStrategy;
			}
			else if (algorithm == AircraftLandingAlgorithm.Improved)
			{
				improvedStrategy ??= new ImprovedAircraftLanding(world);
				return improvedStrategy;
			}
			else
			{
				openRAStrategy ??= new OpenRAAircraftLanding(world);
				return openRAStrategy;
			}
		}

		/// <summary>
		/// Clears all cached strategies. Call when settings change.
		/// </summary>
		public void ClearCache()
		{
			openRAStrategy = null;
			classicStrategy = null;
			improvedStrategy = null;
		}
	}
}
