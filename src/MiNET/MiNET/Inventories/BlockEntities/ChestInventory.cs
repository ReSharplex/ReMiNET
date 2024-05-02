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

using fNbt;
using MiNET.BlockEntities;
using MiNET.Inventories.Types;
using MiNET.Net;
using MiNET.Worlds;

namespace MiNET.Inventories.BlockEntities;

public class ChestInventory : Inventory2
{
	public ChestInventory((Player player, BlockEntity bentity) holder) : base(holder, InventoryType.Container,0, 27, (NbtList) holder.bentity.GetCompound()["Items"])
	{
	}

	public override void Open(Player player)
	{
		if (!IsOpen()) // Chest open animation
		{
			var tileEvent = McpeBlockEvent.CreateObject();
			tileEvent.coordinates = _holder.bentity.Coordinates;
			tileEvent.case1 = 1;
			tileEvent.case2 = 2;
			_holder.player.Level.RelayBroadcast(tileEvent);
		}
		
		var containerOpen = McpeContainerOpen.CreateObject();
		containerOpen.windowId = WindowsId;
		containerOpen.type = (byte) Type;
		containerOpen.coordinates = _holder.bentity.Coordinates;
		containerOpen.runtimeEntityId = -1;
		player.SendPacket(containerOpen);
		
		SetContent(Items);
		
		base.Open(player);
	}

	/// <param name="serverInitiatedClosing">True if the server initiated the closing</param>
	public override void Close(Player player, bool serverInitiatedClosing = false)
	{
		if (!IsOpen()) // Chest close animation
		{
			var tileEvent = McpeBlockEvent.CreateObject();
			tileEvent.coordinates = _holder.bentity.Coordinates;
			tileEvent.case1 = 1;
			tileEvent.case2 = 0;
			_holder.player.Level.RelayBroadcast(tileEvent);
		}
		
		var containerClose = McpeContainerClose.CreateObject();
		containerClose.windowId = WindowsId;
		containerClose.server = serverInitiatedClosing;
		_holder.player.SendPacket(containerClose);
		
		base.Close(player, serverInitiatedClosing);
	}
}