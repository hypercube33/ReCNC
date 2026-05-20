#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>
	/// ReCnC GAME-009: Grants a tech prerequisite when aggregate helipad + airfield count exceeds
	/// capped player aircraft (alive plus queued production), matching classic RA-style 1:1 parking.
	/// </summary>
	[TraitLocation(SystemActors.Player)]
	public sealed class ProvidesAircraftParkingPrerequisiteInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[FieldLoader.Require]
		[Desc("Prerequisite granted while a parking slot is free.")]
		public readonly string Prerequisite = null;

		[Desc("Building actor names that each provide one aircraft parking slot.")]
		public readonly string[] ParkingActors = { "HPAD", "AFLD", "AFLD.Ukraine" };

		[Desc("Aircraft actor names that consume one parking slot (e.g. combat planes and helis; exclude transports).")]
		public readonly string[] CappedActors = { "MIG", "YAK", "HELI", "HIND", "MH60" };

		[Desc("Production queue Type ids (e.g. Aircraft.GDI) whose queued capped actors count toward used slots.")]
		public readonly string[] AircraftQueueTypes = { "Aircraft.GDI", "Aircraft.Nod" };

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			yield return Prerequisite;
		}

		public override object Create(ActorInitializer init)
		{
			return new ProvidesAircraftParkingPrerequisite(this, init);
		}
	}

	public sealed class ProvidesAircraftParkingPrerequisite : ITechTreePrerequisite, INotifyCreated, ITick
	{
		readonly ProvidesAircraftParkingPrerequisiteInfo info;
		readonly HashSet<string> parking;
		readonly HashSet<string> capped;
		readonly HashSet<string> queueTypes;
		readonly Actor self;

		TechTree techTree;
		int lastSignature = int.MinValue;

		public ProvidesAircraftParkingPrerequisite(ProvidesAircraftParkingPrerequisiteInfo info, ActorInitializer init)
		{
			this.info = info;
			self = init.Self;
			parking = new HashSet<string>(info.ParkingActors, StringComparer.OrdinalIgnoreCase);
			capped = new HashSet<string>(info.CappedActors, StringComparer.OrdinalIgnoreCase);
			queueTypes = new HashSet<string>(info.AircraftQueueTypes, StringComparer.Ordinal);
		}

		void INotifyCreated.Created(Actor self)
		{
			techTree = self.Trait<TechTree>();
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (HasFreeSlot(self.Owner))
					yield return info.Prerequisite;
			}
		}

		void ITick.Tick(Actor self)
		{
			if (techTree == null)
				techTree = self.Trait<TechTree>();

			var used = CountUsedSlots(self.Owner);
			var cap = CountParkingCapacity(self.Owner);
			var sig = (used << 16) ^ cap;
			if (sig == lastSignature)
				return;

			lastSignature = sig;
			techTree.Update();
		}

		bool HasFreeSlot(Player player)
		{
			return CountParkingCapacity(player) > CountUsedSlots(player);
		}

		int CountParkingCapacity(Player player)
		{
			var n = 0;
			foreach (var a in player.World.Actors)
			{
				if (a.Owner != player || !a.IsInWorld || a.IsDead)
					continue;

				if (parking.Contains(a.Info.Name))
					n++;
			}

			return n;
		}

		int CountUsedSlots(Player player)
		{
			var n = 0;
			foreach (var a in player.World.Actors)
			{
				if (a.Owner != player || !a.IsInWorld || a.IsDead)
					continue;

				if (capped.Contains(a.Info.Name))
					n++;
			}

			n += CountQueuedCappedActors(player);
			return n;
		}

		int CountQueuedCappedActors(Player player)
		{
			var n = 0;
			foreach (var pq in player.World.ActorsWithTrait<ProductionQueue>())
			{
				if (pq.Actor.Owner != player || pq.Actor.IsDead || !pq.Actor.IsInWorld)
					continue;

				if (!queueTypes.Contains(pq.Trait.Info.Type))
					continue;

				foreach (var item in pq.Trait.AllQueued())
				{
					if (capped.Contains(item.Item))
						n++;
				}
			}

			return n;
		}
	}
}
