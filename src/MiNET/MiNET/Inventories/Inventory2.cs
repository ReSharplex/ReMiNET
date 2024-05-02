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

using System;
using System.Collections.Generic;
using System.Drawing;
using fNbt;
using log4net;
using MiNET.BlockEntities;
using MiNET.Inventories.Types;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Inventories;

public class Inventory2 : IInventory2
{
	private static readonly ILog Log = LogManager.GetLogger(typeof(Inventory2));
	
	public readonly int Id;
	public byte WindowsId { get; set; } // ToDo ???
	public readonly InventoryType Type;
	public readonly short Size;
	public IList<Player> Viewers;
	public ItemStacks Items { get; set; }
	protected readonly (Player player, BlockEntity bentity) _holder;
	public event Action<Player, Inventory2, byte, Item> InventoryChange;
	
	private readonly object _lockObject = new object();
	private static int _inventoryId { get; set; }

	public Inventory2((Player player, BlockEntity bentity) holder, InventoryType type, byte windowsId, short size, NbtList slots) // ToDo slots ???
	{
		Id = GetInventoryId();
		Type = type;
		WindowsId = windowsId;
		Size = size;
		Viewers = new List<Player>();
		_holder = holder;
		
		Items = new ItemStacks();
		for (byte i = 0; i < Size; i++)
		{
			Items.Add(new ItemAir());
		}

		for (byte i = 0; i < slots.Count; i++)
		{
			var nbtItem = (NbtCompound) slots[i];

			Item item = ItemFactory.GetItem(nbtItem["id"].ShortValue, nbtItem["Damage"].ShortValue, nbtItem["Count"].ByteValue);
			byte slotIdx = nbtItem["Slot"].ByteValue;
			Log.Debug($"Chest item {slotIdx}: {item}");
			Items[slotIdx] = item;
		}
	}

	public void SetItem(int slot, Item item)
	{
		Items[slot] = item;

		McpeInventorySlot inventorySlot = McpeInventorySlot.CreateObject();
		inventorySlot.inventoryId = WindowsId;
		inventorySlot.slot = (uint) slot;
		inventorySlot.item = item;

		foreach (var viewer in Viewers)
		{
			viewer.SendPacket(inventorySlot);
		}

		if (_holder.bentity is not null)
		{
			NbtCompound compound = _holder.bentity.GetCompound();
			compound["Items"] = GetSlots();
		}
	}

	public Item GetItem(int slot)
	{
		return Items[slot];
	}

	public void SetContent(ItemStacks items)
	{
		Items = items;
		
		McpeInventoryContent inventoryContent = McpeInventoryContent.CreateObject();
		inventoryContent.inventoryId = WindowsId;
		inventoryContent.input = items;

		foreach (var viewer in Viewers)
		{
			viewer.SendPacket(inventoryContent);
		}
	}

	public ItemStacks GetContent()
	{
		return Items;
	}

	public virtual void Open(Player player)
	{
		player._openInventory2 = this;
		Viewers.Add(player);
		InventoryChange += OnInventoryChange;
	}

	public virtual void Close(Player player, bool serverInitiatedClosing = false)
	{
		player._openInventory2 = null;
		Viewers.Remove(player);
		InventoryChange -= OnInventoryChange;
	}

	protected bool IsOpen()
	{
		return Viewers.Count > 0;
	}
	
	protected virtual void OnInventoryChange(Player player, byte slot, Item itemStack)
	{
		InventoryChange?.Invoke(player, this, slot, itemStack);
	}
	
	private void OnInventoryChange(Player player, Inventory2 inventory, byte slot, Item item)
	{
		SetItem(slot, item);
	}
	
	private NbtList GetSlots()
	{
		NbtList slots = new NbtList("Items");
		for (byte i = 0; i < Size; i++)
		{
			var slot = Items[i];
			slots.Add(new NbtCompound
			{
				new NbtByte("Count", slot.Count),
				new NbtByte("Slot", i),
				new NbtShort("id", slot.Id),
				new NbtShort("Damage", slot.Metadata),
			});
		}

		return slots;
	}

	private int GetInventoryId()
	{
		lock (_lockObject)
		{
			_inventoryId++;
			if (_inventoryId == 0x78)
				_inventoryId++;
			if (_inventoryId == 0x79)
				_inventoryId++;

			return _inventoryId;
		}
	}
}