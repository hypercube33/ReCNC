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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds lobby dropdown options for ReCnC algorithm selection (pathfinding, aircraft landing, docking).")]
	[TraitLocation(SystemActors.World)]
	public class AlgorithmLobbyOptionsInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Display order for the pathfinding option in the lobby.")]
		public readonly int PathfindingDisplayOrder = 50;

		[Desc("Display order for the aircraft landing option in the lobby.")]
		public readonly int AircraftDisplayOrder = 51;

		[Desc("Display order for the docking option in the lobby.")]
		public readonly int DockingDisplayOrder = 52;

		[Desc("Default pathfinding algorithm.")]
		public readonly string DefaultPathfinding = "OpenRA";

		[Desc("Default aircraft landing algorithm.")]
		public readonly string DefaultAircraft = "OpenRA";

		[Desc("Default docking algorithm.")]
		public readonly string DefaultDocking = "OpenRA";

		[Desc("Lock the options (prevent changes).")]
		public readonly bool Locked = false;

		[Desc("Show the options in the lobby.")]
		public readonly bool Visible = true;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			var algorithmValues = new Dictionary<string, string>
			{
				{ "OpenRA", "dropdown-algorithm-openra.label" },
				{ "ClassicCnC", "dropdown-algorithm-classic.label" },
				{ "Improved", "dropdown-algorithm-improved.label" }
			};

			yield return new LobbyOption(
				map,
				"pathfinding-algorithm",
				"dropdown-pathfinding.label",
				"dropdown-pathfinding.description",
				Visible,
				PathfindingDisplayOrder,
				algorithmValues,
				DefaultPathfinding,
				Locked);

			yield return new LobbyOption(
				map,
				"aircraft-algorithm",
				"dropdown-aircraft.label",
				"dropdown-aircraft.description",
				Visible,
				AircraftDisplayOrder,
				algorithmValues,
				DefaultAircraft,
				Locked);

			yield return new LobbyOption(
				map,
				"docking-algorithm",
				"dropdown-docking.label",
				"dropdown-docking.description",
				Visible,
				DockingDisplayOrder,
				algorithmValues,
				DefaultDocking,
				Locked);
		}

		public override object Create(ActorInitializer init)
		{
			return new AlgorithmLobbyOptions(init.Self, this);
		}
	}

	public class AlgorithmLobbyOptions : INotifyCreated, IWorldLoaded
	{
		readonly AlgorithmLobbyOptionsInfo info;

		public string PathfindingAlgorithm { get; private set; }
		public string AircraftAlgorithm { get; private set; }
		public string DockingAlgorithm { get; private set; }

		public AlgorithmLobbyOptions(Actor self, AlgorithmLobbyOptionsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var lobbyInfo = self.World.LobbyInfo;

			PathfindingAlgorithm = lobbyInfo.GlobalSettings.OptionOrDefault(
				"pathfinding-algorithm", info.DefaultPathfinding);

			AircraftAlgorithm = lobbyInfo.GlobalSettings.OptionOrDefault(
				"aircraft-algorithm", info.DefaultAircraft);

			DockingAlgorithm = lobbyInfo.GlobalSettings.OptionOrDefault(
				"docking-algorithm", info.DefaultDocking);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			// Update the strategy managers with the lobby-selected algorithms
			var pathfindingManager = w.WorldActor.TraitOrDefault<PathfindingStrategyManager>();
			if (pathfindingManager != null)
				pathfindingManager.SetAlgorithmFromLobby(PathfindingAlgorithm);

			var aircraftManager = w.WorldActor.TraitOrDefault<AircraftLandingManager>();
			if (aircraftManager != null)
				aircraftManager.SetAlgorithmFromLobby(AircraftAlgorithm);

			var dockingManager = w.WorldActor.TraitOrDefault<DockingStrategyManager>();
			if (dockingManager != null)
				dockingManager.SetAlgorithmFromLobby(DockingAlgorithm);
		}
	}
}
