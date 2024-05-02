#region LICENSE
// The contents of this file are subject to the Common Public Attribution
// License Version 1.0. (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE.
// The License is based on the Mozilla Public License Version 1.1, but Sections 14
// and 15 have been added to cover use of software over a computer network and
// provide for limited attribution for the Original Developer. In addition, Exhibit A has
// been modified to be consistent with Exhibit B.
// 
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
// the specific language governing rights and limitations under the License.
// 
// The Original Code is MiNET.
// 
// The Original Developer is the Initial Developer.  The Initial Developer of
// the Original Code is Niclas Olofsson.
// 
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2024 Niclas Olofsson.
// All Rights Reserved.
#endregion

using MiNET.BlockEntities;
using MiNET.Inventories.BlockEntities;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Inventories;

public static class InventoryManager2
{
	public static void OpenBlockInventory2(Player player, BlockCoordinates coordinates)
	{
		if (player.Level.GetBlockEntity(coordinates) is null) return;
		var blockEntity = player.Level.GetBlockEntity(coordinates);

		// CanOpenInventoryIfNotReset(player);
		
		switch (blockEntity)
		{
			case ChestBlockEntity:
			{
				var chestInventory = new ChestInventory((player, blockEntity));
				player._openInventory2 = chestInventory;
				chestInventory.Open(player);
				break;
			}
		}
	}

	public static void CanOpenInventoryIfNotReset(Player player)
	{
		if (player._openInventory2 is not null) player._openInventory2.Close(player, true);
	}
}