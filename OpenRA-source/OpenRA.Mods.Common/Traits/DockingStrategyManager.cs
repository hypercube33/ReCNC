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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Manages docking strategy selection for harvesters, aircraft, and repair-seeking units.",
		"Allows runtime selection between OpenRA, Classic C&C, and Improved algorithms.")]
	public class DockingStrategyManagerInfo : TraitInfo
	{
		[Desc("Default docking algorithm if not specified in settings.",
			"Options: OpenRA, ClassicCnC, Improved")]
		public readonly string DefaultAlgorithm = "OpenRA";

		public override object Create(ActorInitializer init)
		{
			return new DockingStrategyManager(init.Self, this);
		}
	}

	/// <summary>
	/// Provides centralized docking strategy selection based on game settings.
	/// Reads from Game.Settings.Game.DockingAlgorithm and returns the appropriate strategy.
	/// </summary>
	public class DockingStrategyManager : IWorldLoaded
	{
		public readonly DockingStrategyManagerInfo Info;
		readonly World world;

		IDockingStrategy openRAStrategy;
		IDockingStrategy classicStrategy;
		IDockingStrategy improvedStrategy;

		DockingAlgorithm currentAlgorithm;
		IDockingStrategy currentStrategy;

		string lobbyAlgorithm;

		public DockingStrategyManager(Actor self, DockingStrategyManagerInfo info)
		{
			Info = info;
			world = self.World;
		}

		/// <summary>
		/// Sets the algorithm from lobby selection. Called by AlgorithmLobbyOptions.
		/// </summary>
		public void SetAlgorithmFromLobby(string algorithm)
		{
			lobbyAlgorithm = algorithm;
			currentAlgorithm = ParseAlgorithm(algorithm);
			currentStrategy = GetStrategyForAlgorithm(currentAlgorithm);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			openRAStrategy = new OpenRADockingStrategy();
			classicStrategy = new ClassicDockingStrategy();
			improvedStrategy = new ImprovedDockingStrategy(world);

			currentAlgorithm = ParseAlgorithm(GetSettingValue());
			currentStrategy = GetStrategyForAlgorithm(currentAlgorithm);
		}

		string GetSettingValue()
		{
			if (!string.IsNullOrEmpty(lobbyAlgorithm))
				return lobbyAlgorithm;

			try
			{
				var settings = Game.Settings;
				if (settings?.Game != null)
				{
					var prop = settings.Game.GetType().GetProperty("DockingAlgorithm");
					if (prop != null)
						return prop.GetValue(settings.Game) as string ?? Info.DefaultAlgorithm;
				}
			}
			catch { }

			return Info.DefaultAlgorithm;
		}

		DockingAlgorithm ParseAlgorithm(string value)
		{
			if (string.IsNullOrEmpty(value))
				return DockingAlgorithm.OpenRA;

			if (value.Equals("ClassicCnC", StringComparison.OrdinalIgnoreCase) ||
				value.Equals("Classic", StringComparison.OrdinalIgnoreCase))
				return DockingAlgorithm.ClassicCnC;

			if (value.Equals("Improved", StringComparison.OrdinalIgnoreCase))
				return DockingAlgorithm.Improved;

			return DockingAlgorithm.OpenRA;
		}

		IDockingStrategy GetStrategyForAlgorithm(DockingAlgorithm algorithm)
		{
			return algorithm switch
			{
				DockingAlgorithm.ClassicCnC => classicStrategy,
				DockingAlgorithm.Improved => improvedStrategy,
				_ => openRAStrategy
			};
		}

		/// <summary>
		/// Gets the currently active docking strategy.
		/// </summary>
		public IDockingStrategy CurrentStrategy
		{
			get
			{
				var newAlgorithm = ParseAlgorithm(GetSettingValue());
				if (newAlgorithm != currentAlgorithm)
				{
					currentAlgorithm = newAlgorithm;
					currentStrategy = GetStrategyForAlgorithm(currentAlgorithm);
				}

				return currentStrategy;
			}
		}

		/// <summary>
		/// Gets the currently selected algorithm.
		/// </summary>
		public DockingAlgorithm CurrentAlgorithm => currentAlgorithm;

		/// <summary>
		/// Gets a specific strategy by algorithm type.
		/// </summary>
		public IDockingStrategy GetStrategy(DockingAlgorithm algorithm)
		{
			return GetStrategyForAlgorithm(algorithm);
		}
	}
}
