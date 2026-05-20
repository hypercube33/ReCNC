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

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Classic Command &amp; Conquer movement cost types.
	/// Ported from the original 1995 C&amp;C source code (DEFINES.H).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The key difference from OpenRA's BlockedByActor system is that classic C&amp;C uses
	/// cost-based blocking rather than binary blocking. This means moving units are NOT
	/// complete blockers - they just have a higher cost to path through.
	/// </para>
	/// <para>
	/// This is why classic C&amp;C doesn't have the "bridge re-pathing problem" where units
	/// path around other moving units on chokepoints instead of waiting for them.
	/// </para>
	/// </remarks>
	public enum MoveType
	{
		/// <summary>
		/// No blockage, clear path. Cost = 1.
		/// </summary>
		Ok = 0,

		/// <summary>
		/// A cloaked blocking enemy object. Cost = 1.
		/// </summary>
		Cloak = 1,

		/// <summary>
		/// Blocked by a moving unit - but only temporarily.
		/// This is PASSABLE with a penalty! Cost = 3.
		/// </summary>
		/// <remarks>
		/// This is the critical difference from OpenRA's BlockedByActor.All which treats
		/// moving units as complete blockers. Classic C&amp;C allows pathing THROUGH moving
		/// units with a cost penalty, avoiding the bridge/chokepoint re-pathing problem.
		/// </remarks>
		MovingBlock = 2,

		/// <summary>
		/// Enemy unit or building is blocking. Cost = 8.
		/// </summary>
		Destroyable = 3,

		/// <summary>
		/// Blocked by friendly unit (temporary). Cost = 10.
		/// </summary>
		Temp = 4,

		/// <summary>
		/// Strictly prohibited terrain. Cost = 0 (impassable).
		/// </summary>
		No = 5
	}

	/// <summary>
	/// Helper class for working with classic C&amp;C MoveType costs.
	/// </summary>
	public static class MoveTypeExtensions
	{
		/// <summary>
		/// Cost values for each MoveType, matching original C&amp;C FINDPATH.CPP.
		/// </summary>
		static readonly int[] Costs = { 1, 1, 3, 8, 10, 0 };

		/// <summary>
		/// Gets the movement cost for a MoveType.
		/// </summary>
		/// <param name="moveType">The move type to get cost for.</param>
		/// <returns>The cost value (0 means impassable).</returns>
		public static int GetCost(this MoveType moveType)
		{
			var index = (int)moveType;
			return index >= 0 && index < Costs.Length ? Costs[index] : 0;
		}

		/// <summary>
		/// Checks if a MoveType is passable (cost > 0).
		/// </summary>
		/// <param name="moveType">The move type to check.</param>
		/// <returns>True if passable, false if blocked.</returns>
		public static bool IsPassable(this MoveType moveType)
		{
			return moveType != MoveType.No && GetCost(moveType) > 0;
		}

		/// <summary>
		/// Checks if the cell can be entered given the threshold.
		/// In classic C&amp;C, a cell is passable if its MoveType is less than or equal to the threshold.
		/// </summary>
		/// <param name="moveType">The move type of the cell.</param>
		/// <param name="threshold">The maximum allowed MoveType.</param>
		/// <returns>True if the cell can be entered.</returns>
		public static bool CanEnter(this MoveType moveType, MoveType threshold)
		{
			return moveType <= threshold && moveType != MoveType.No;
		}
	}
}
