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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	/// <summary>
	/// Enhanced MoveToDock that uses SmartDockingService for intelligent wait-vs-travel decisions.
	/// This version continuously re-evaluates whether to wait or find an alternate dock.
	/// </summary>
	public class ImprovedMoveToDock : Activity
	{
		readonly DockClientManager dockClient;
		readonly SmartDockingService smartDocking;
		readonly INotifyDockClientMoving[] notifyDockClientMoving;
		readonly MoveCooldownHelper moveCooldownHelper;
		readonly Color? dockLineColor;
		readonly bool forceEnter;
		readonly bool ignoreOccupancy;
		readonly int reevaluateInterval;

		Actor dockHostActor;
		IDockHost dockHost;
		bool dockingCancelled;
		int ticksSinceLastEvaluation;
		bool hasRegisteredQueue;

		public ImprovedMoveToDock(
			Actor self,
			Actor dockHostActor = null,
			IDockHost dockHost = null,
			bool forceEnter = false,
			bool ignoreOccupancy = false,
			Color? dockLineColor = null,
			int reevaluateInterval = 50)
		{
			dockClient = self.Trait<DockClientManager>();
			smartDocking = self.World.WorldActor.TraitOrDefault<SmartDockingService>();
			this.dockHostActor = dockHostActor;
			this.dockHost = dockHost;
			this.forceEnter = forceEnter;
			this.ignoreOccupancy = ignoreOccupancy;
			this.dockLineColor = dockLineColor;
			this.reevaluateInterval = reevaluateInterval;
			notifyDockClientMoving = self.TraitsImplementing<INotifyDockClientMoving>().ToArray();
			moveCooldownHelper = new MoveCooldownHelper(self.World, self.Trait<IMove>() as Mobile) { RetryIfDestinationBlocked = true };
		}

		protected override void OnFirstRun(Actor self)
		{
			if (dockClient.IsTraitDisabled)
				return;

			if (dockHostActor != null && dockHost == null)
			{
				if (dockHostActor.IsDead || !dockHostActor.IsInWorld)
				{
					dockingCancelled = true;
					return;
				}

				var link = dockClient.AvailableDockHosts(dockHostActor, default, forceEnter, ignoreOccupancy)
					.ClosestDock(self, dockClient);

				if (link.HasValue)
					dockHost = link.Value.Trait;
				else
					dockingCancelled = true;
			}
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (dockingCancelled || dockClient.IsTraitDisabled)
			{
				Cancel(self, true);
				return true;
			}

			ticksSinceLastEvaluation++;

			if (dockHost == null || !dockHost.IsEnabledAndInWorld)
			{
				var best = FindBestDock(self);
				if (best.HasValue)
				{
					dockHost = best.Value.Trait;
					dockHostActor = best.Value.Actor;
				}
				else
				{
					QueueChild(new Wait(dockClient.Info.SearchForDockDelay));
					return false;
				}
			}

			if (smartDocking != null && ticksSinceLastEvaluation >= reevaluateInterval)
			{
				ticksSinceLastEvaluation = 0;
				var alternates = GetAlternativeDocks(self);

				if (alternates.Any() && !smartDocking.ShouldWaitAtCurrentDock(self, dockHost, alternates))
				{
					var best = smartDocking.FindBestDock(self, alternates, dockClient);
					if (best != null && best.RecommendedDock.Trait != dockHost)
					{
						if (hasRegisteredQueue && smartDocking != null)
							smartDocking.RegisterDockComplete(self, dockHost);

						dockClient.UnreserveHost();
						dockHost = best.RecommendedDock.Trait;
						dockHostActor = best.RecommendedDock.Actor;
						hasRegisteredQueue = false;
					}
				}
			}

			var result = moveCooldownHelper.Tick(false);
			if (result != null)
				return result.Value;

			if (dockClient.ReserveHost(dockHostActor, dockHost))
			{
				if (smartDocking != null && !hasRegisteredQueue)
				{
					smartDocking.RegisterQueueEntry(self, dockHost, GetPriority(self));
					hasRegisteredQueue = true;
				}

				if (dockHost.QueueMoveActivity(this, dockHostActor, self, dockClient, moveCooldownHelper))
				{
					foreach (var ndcm in notifyDockClientMoving)
						ndcm.MovingToDock(self, dockHostActor, dockHost);

					return false;
				}

				if (smartDocking != null)
					smartDocking.RegisterDockStart(self, dockHost);

				dockHost.QueueDockActivity(this, dockHostActor, self, dockClient);
				return true;
			}
			else
			{
				if (smartDocking != null)
				{
					var alternates = GetAlternativeDocks(self);
					if (alternates.Any())
					{
						var best = smartDocking.FindBestDock(self, alternates, dockClient);
						if (best != null && !best.ShouldWait)
						{
							dockHost = best.RecommendedDock.Trait;
							dockHostActor = best.RecommendedDock.Actor;
							return false;
						}
					}
				}

				QueueChild(new Wait(dockClient.Info.SearchForDockDelay));
				return false;
			}
		}

		TraitPair<IDockHost>? FindBestDock(Actor self)
		{
			if (smartDocking != null)
			{
				var allDocks = self.World.ActorsWithTrait<IDockHost>()
					.Where(h => h.Trait.IsEnabledAndInWorld &&
						dockClient.CanDockAt(h.Actor, h.Trait, forceEnter, ignoreOccupancy));

				var decision = smartDocking.FindBestDock(self, allDocks, dockClient);
				if (decision != null)
					return decision.RecommendedDock;
			}

			return dockClient.ClosestDock(null, default, forceEnter, ignoreOccupancy);
		}

		IEnumerable<TraitPair<IDockHost>> GetAlternativeDocks(Actor self)
		{
			return self.World.ActorsWithTrait<IDockHost>()
				.Where(h =>
					h.Trait != dockHost &&
					h.Trait.IsEnabledAndInWorld &&
					dockClient.CanDockAt(h.Actor, h.Trait, forceEnter, ignoreOccupancy));
		}

		int GetPriority(Actor self)
		{
			var health = self.TraitOrDefault<IHealth>();
			if (health != null)
			{
				var healthPercent = health.HP * 100 / health.MaxHP;
				if (healthPercent < 25)
					return 100;
				if (healthPercent < 50)
					return 50;
			}

			var harvester = self.TraitOrDefault<Harvester>();
			if (harvester != null && harvester.IsFull)
				return 25;

			return 0;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			if (hasRegisteredQueue && smartDocking != null)
				smartDocking.RegisterDockComplete(self, dockHost);

			dockClient.UnreserveHost();

			foreach (var ndcm in notifyDockClientMoving)
				ndcm.MovementCancelled(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (!dockLineColor.HasValue)
				yield break;

			if (dockHostActor != null)
				yield return new TargetLineNode(Target.FromActor(dockHostActor), dockLineColor.Value);
			else if (dockClient.ReservedHostActor != null)
				yield return new TargetLineNode(Target.FromActor(dockClient.ReservedHostActor), dockLineColor.Value);
		}
	}
}
