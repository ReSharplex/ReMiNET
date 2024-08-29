﻿#region LICENSE

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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MiNET.Crafting;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET
{
	public class ItemStackInventoryManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ItemStackInventoryManager));

		private readonly Player _player;

		public ItemStackInventoryManager(Player player)
		{
			_player = player;
		}

		public virtual List<StackResponseContainerInfo> HandleItemStackActions(int requestId, ItemStackActionList actions)
		{
			var stackResponses = new List<StackResponseContainerInfo>();
			byte TimesCrafted = 1;
			foreach (ItemStackAction stackAction in actions)
			{
				switch (stackAction)
				{
					case CraftAction craftAction:
					{
						TimesCrafted = ProcessCraftAction(craftAction);
						break;
					}
					case CraftAutoAction CraftAuto:
					{
						ProcessCraftAuto(CraftAuto);
						break;
					}
					case CraftCreativeAction craftCreativeAction:
					{
						ProcessCraftCreativeAction(craftCreativeAction);
						break;
					}
					case CraftNotImplementedDeprecatedAction craftNotImplementedDeprecatedAction:
					{
						// Do nothing democrafts
						ProcessCraftNotImplementedDeprecatedAction(craftNotImplementedDeprecatedAction);
						break;
					}
					case CraftRecipeOptionalAction craftRecipeOptionalAction:
					{
						ProcessCraftRecipeOptionalAction(craftRecipeOptionalAction);
						break;
					}
					case CraftResultDeprecatedAction craftResultDeprecatedAction:
					{
						ProcessCraftResultDeprecatedAction(craftResultDeprecatedAction, TimesCrafted);
						break;
					}
					case TakeAction takeAction:
					{
						ProcessTakeAction(takeAction, stackResponses);
						break;
					}
					case PlaceAction placeAction:
					{
						ProcessPlaceAction(placeAction, stackResponses);
						break;
					}
					case SwapAction swapAction:
					{
						ProcessSwapAction(swapAction, stackResponses);
						break;
					}
					case DestroyAction destroyAction:
					{
						ProcessDestroyAction(destroyAction, stackResponses);
						break;
					}
					case DropAction dropAction:
					{
						ProcessDropAction(dropAction, stackResponses);

						break;
					}
					case ConsumeAction consumeAction:
					{
						ProcessConsumeAction(consumeAction, stackResponses);
						break;
					}
					case LoomStackRequestAction loomStackRequestAction:
					{
						ProcessLoomStackRequestAction(loomStackRequestAction, stackResponses);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(stackAction));
				}
			}

			foreach (IGrouping<byte, StackResponseContainerInfo> stackResponseGroup in stackResponses.GroupBy(r => r.ContainerId))
			{
				if (stackResponseGroup.Count() > 1)
				{
					byte containerId = stackResponseGroup.Key;
					StackResponseSlotInfo slotToKeep = null;
					foreach (IGrouping<byte, StackResponseSlotInfo> slotGroup in stackResponseGroup.SelectMany(d => d.Slots).GroupBy(s => s.Slot))
					{
						byte slot = slotGroup.Key;
						if (slotGroup.Count() > 1)
						{
							slotToKeep = slotGroup.ToList().Last();
						}
					}
					if (slotToKeep != null)
					{
						foreach (StackResponseContainerInfo containerInfo in stackResponseGroup)
						{
							if (!containerInfo.Slots.Contains(slotToKeep))
							{
								stackResponses.Remove(containerInfo);
							}
						}
					}
				}
			}

			return stackResponses;
		}

		protected virtual void ProcessConsumeAction(ConsumeAction action, List<StackResponseContainerInfo> stackResponses)
		{
			byte count = action.Count;
			StackRequestSlotInfo source = action.Source;

			Item sourceItem = GetContainerItem(source.ContainerId, source.Slot);
			if (source.ContainerId == 0) //anvil
			{
				ProcessAnvilAction(action);
			}
			sourceItem.Count -= count;
			if (sourceItem.Count <= 0)
			{
				sourceItem = new ItemAir();
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
		}

		protected virtual void ProcessLoomStackRequestAction(LoomStackRequestAction action, List<StackResponseContainerInfo> stackResponses)
		{
		}

		protected virtual void ProcessDropAction(DropAction action, List<StackResponseContainerInfo> stackResponses)
		{
			byte count = action.Count;
			Item dropItem;
			StackRequestSlotInfo source = action.Source;

			Item sourceItem = GetContainerItem(source.ContainerId, source.Slot);

			if (sourceItem.Count == count || sourceItem.Count - count <= 0)
			{
				dropItem = sourceItem;
				sourceItem = new ItemAir();
				sourceItem.UniqueId = 0;
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}
			else
			{
				dropItem = (Item) sourceItem.Clone();
				sourceItem.Count -= count;
				dropItem.Count = count;
				dropItem.UniqueId = Environment.TickCount;
			}

			_player.DropItem(dropItem);

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
		}

		protected virtual void ProcessDestroyAction(DestroyAction action, List<StackResponseContainerInfo> stackResponses)
		{
			byte count = action.Count;
			StackRequestSlotInfo source = action.Source;

			Item sourceItem = GetContainerItem(source.ContainerId, source.Slot);
			sourceItem.Count -= count;
			if (sourceItem.Count <= 0)
			{
				sourceItem = new ItemAir();
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
		}

		protected virtual void ProcessSwapAction(SwapAction action, List<StackResponseContainerInfo> stackResponses)
		{
			StackRequestSlotInfo source = action.Source;
			StackRequestSlotInfo destination = action.Destination;

			Item sourceItem = GetContainerItem(source.ContainerId, source.Slot);
			Item destItem = GetContainerItem(destination.ContainerId, destination.Slot);

			SetContainerItem(source.ContainerId, source.Slot, destItem);
			SetContainerItem(destination.ContainerId, destination.Slot, sourceItem);

			if (source.ContainerId == 21 || source.ContainerId == 22 || destination.ContainerId == 21 || destination.ContainerId == 22)
			{
				if (!(GetContainerItem(21, 14) is ItemAir) && !(GetContainerItem(22, 15) is ItemAir)) Enchantment.SendEnchantments(_player, GetContainerItem(21, 14));
				else Enchantment.SendEmptyEnchantments(_player);
			}

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = destItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = destItem.UniqueId
					}
				}
			});
			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = destination.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = destination.Slot,
						HotbarSlot = destination.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
		}

		protected virtual void ProcessPlaceAction(PlaceAction action, List<StackResponseContainerInfo> stackResponses)
		{
			byte count = action.Count;
			Item sourceItem;
			Item destItem;
			StackRequestSlotInfo source = action.Source;
			StackRequestSlotInfo destination = action.Destination;

			sourceItem = GetContainerItem(source.ContainerId, source.Slot);

			if (sourceItem.Count == count || sourceItem.Count - count <= 0)
			{
				destItem = sourceItem;
				sourceItem = new ItemAir();
				sourceItem.UniqueId = 0;
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}
			else
			{
				destItem = (Item) sourceItem.Clone();
				sourceItem.Count -= count;
				destItem.Count = count;
				destItem.UniqueId = Environment.TickCount;
			}

			Item existingItem = GetContainerItem(destination.ContainerId, destination.Slot);
			if (existingItem.UniqueId > 0) // is empty/air is what this means
			{
				existingItem.Count += count;
				destItem = existingItem;
			}
			else
			{
				SetContainerItem(destination.ContainerId, destination.Slot, destItem);
			}
			if (destination.ContainerId == 6 || source.ContainerId == 6)
			{
				_player.SendArmorForPlayer(_player.Level.GetSpawnedPlayers());
			}
			else if (destination.ContainerId == 22)
			{
				if (Enum.IsDefined(typeof(ItemType), GetContainerItem(21, 14).ItemType) && !(GetContainerItem(22, 15) is ItemAir))
				{
					Enchantment.SendEnchantments(_player, GetContainerItem(21, 14));
				}
				else
				{
					Enchantment.SendEmptyEnchantments(_player);
				}
			}
			else if (destination.ContainerId == 23)
			{
				if (Enum.IsDefined(typeof(ItemType), GetContainerItem(21, 14).ItemType) && !(GetContainerItem(21, 14) is ItemAir) && !(GetContainerItem(22, 15) is ItemAir))
				{
					Enchantment.SendEnchantments(_player, GetContainerItem(21, 14));
				}
				else
				{
					Enchantment.SendEmptyEnchantments(_player);
				}
			}

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = destination.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = destItem.Count,
						Slot = destination.Slot,
						HotbarSlot = destination.Slot,
						StackNetworkId = destItem.UniqueId
					}
				}
			});
			_player.Inventory.SendSetSlot(destination.Slot);
		}

		protected virtual void ProcessTakeAction(TakeAction action, List<StackResponseContainerInfo> stackResponses)
		{
			byte count = action.Count;
			Item sourceItem;
			Item destinationItem;
			Item destItem;
			StackRequestSlotInfo source = action.Source;
			StackRequestSlotInfo destination = action.Destination;

			sourceItem = GetContainerItem(source.ContainerId, source.Slot);
			destinationItem = GetContainerItem(destination.ContainerId, destination.Slot);


			if (source.ContainerId == 60 && sourceItem.Id == destinationItem.Id)
			{
				destItem = (Item) destinationItem.Clone();
				destItem.Count += count;
				destItem.UniqueId = Environment.TickCount;
			}
			else if (source.ContainerId != 60 && sourceItem.Count == count)
			{
				destItem = (Item) sourceItem.Clone();
				destItem.Count = (byte)(destinationItem.Count + count);
				destItem.UniqueId = Environment.TickCount;
				sourceItem = new ItemAir();
				sourceItem.UniqueId = 0;
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}
			else
			{
				destItem = (Item) sourceItem.Clone();
				destItem.Count = count;
				destItem.UniqueId = Environment.TickCount;
				sourceItem.Count -= count;
				SetContainerItem(source.ContainerId, source.Slot, sourceItem);
			}

			SetContainerItem(destination.ContainerId, destination.Slot, destItem);

			if (source.ContainerId == 22 || source.ContainerId == 23)
			{
				Enchantment.SendEmptyEnchantments(_player);
			}

			if (source.ContainerId == 6)
			{
				_player.SendArmorForPlayer(_player.Level.GetSpawnedPlayers());
			}

			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = source.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = sourceItem.Count,
						Slot = source.Slot,
						HotbarSlot = source.Slot,
						StackNetworkId = sourceItem.UniqueId
					}
				}
			});
			stackResponses.Add(new StackResponseContainerInfo
			{
				ContainerId = destination.ContainerId,
				Slots = new List<StackResponseSlotInfo>
				{
					new StackResponseSlotInfo()
					{
						Count = destItem.Count,
						Slot = destination.Slot,
						HotbarSlot = destination.Slot,
						StackNetworkId = destItem.UniqueId
					}
				}
			});
		}

		protected virtual void ProcessCraftResultDeprecatedAction(CraftResultDeprecatedAction action, byte TimesCrafted)
		{
			if (GetContainerItem(59, 50).UniqueId > 0) return;
			Item craftingResult = action.ResultItems.FirstOrDefault();
			if (craftingResult == null) return;
			craftingResult.Count = (byte)(craftingResult.Count * TimesCrafted);
			craftingResult.UniqueId = Environment.TickCount;
			SetContainerItem(59, 50, craftingResult);
		}

		protected virtual void ProcessCraftNotImplementedDeprecatedAction(CraftNotImplementedDeprecatedAction action)
		{
		}

		protected virtual byte ProcessCraftAction(CraftAction action)
		{
			return action.TimesCrafted;
			/*RecipeManager.resultMap.TryGetValue((int) action.RecipeNetworkId, out Item item);
			Item craftedItem = ItemFactory.GetItem(item.Id, item.Metadata, item.Count);
			craftedItem.Count = (byte) ((item.Count + GetContainerItem(59, 0).Count) * action.TimesCrafted);
			Log.Error(GetContainerItem(59, 0).Id);
			Log.Error(GetContainerItem(59, 0).Count);
			SetContainerItem(59, 0, craftedItem);*/
		}
		protected virtual void ProcessCraftAuto(CraftAutoAction action)
		{
			RecipeManager.resultMap.TryGetValue((int)action.RecipeNetworkId, out Item item);
			_player.Inventory.UiInventory.Slots[50] = ItemFactory.GetItem(item.Id, item.Metadata, item.Count * action.TimesCrafted);
		}

		protected virtual void ProcessCraftCreativeAction(CraftCreativeAction action)
		{
			Item creativeItem = InventoryUtils.CreativeInventoryItems[(int)action.CreativeItemNetworkId];
			if (creativeItem == null) throw new Exception($"Failed to find inventory item with unique id: {action.CreativeItemNetworkId}");
			creativeItem = ItemFactory.GetItem(creativeItem.Id, creativeItem.Metadata);
			creativeItem.Count = (byte) creativeItem.MaxStackSize;
			creativeItem.UniqueId = Environment.TickCount;
			creativeItem.ExtraData = InventoryUtils.CreativeInventoryItems[(int) action.CreativeItemNetworkId].ExtraData;
			Log.Debug($"Creating {creativeItem}");
			_player.Inventory.UiInventory.Slots[50] = creativeItem;
		}

		protected virtual void ProcessCraftRecipeOptionalAction(CraftRecipeOptionalAction action)
		{
		}

		protected virtual void ProcessAnvilAction(ConsumeAction action)
		{
			var sourceItem = GetContainerItem(13, 1);
			_player.Inventory.UiInventory.Slots[50] = new Item(sourceItem.Id, 0, 1);
		}

		private Item GetContainerItem(int containerId, int slot)
		{
			if (_player.UsingAnvil && containerId < 3) containerId = 13;

			Item item = null;
			switch (containerId)
			{
				case 13: // crafting
				case 21: // enchanting
				case 22: // enchanting
				case 23: // enchanting
				case 41: // loom
				case 42: // loom
				case 43: // loom
				case 59: // cursor
				case 60: // creative
					item = _player.Inventory.UiInventory.Slots[slot];
					break;
				case 12: // auto
				case 28: // hotbar
				case 29: // player inventory
					item = _player.Inventory.Slots[slot];
					break;
				case 34: // off-hand
					item = _player.Inventory.OffHand;
					break;
				case 6: // armor
					item = slot switch
					{
						0 => _player.Inventory.Helmet,
						1 => _player.Inventory.Chest,
						2 => _player.Inventory.Leggings,
						3 => _player.Inventory.Boots,
						_ => null
					};
					break;
				case 7: // chest/container
				case 24: // furnace
				case 25: // furnace
				case 26: // furnace
				case 45: // blast furnace
					if (_player._openInventory is Inventory inventory) item = inventory.GetSlot((byte) slot);
					break;
				default:
					Log.Warn($"Unknown containerId: {containerId}");
					break;
			}

			return item;
		}

		private void SetContainerItem(int containerId, int slot, Item item)
		{
			if (_player.UsingAnvil && containerId < 3) containerId = 13;

			switch (containerId)
			{
				case 13: // crafting
				case 21: // enchanting
				case 22: // enchanting
				case 23: // enchanting
				case 41: // loom
				case 42: // loom
				case 43: // loom
				case 59: // cursor
				case 60: // creative
					_player.Inventory.UiInventory.Slots[slot] = item;
					break;
				case 12: // auto
				case 28: // hotbar
				case 29: // player inventory
					_player.Inventory.Slots[slot] = item;
					break;
				case 34: // off-hand
					_player.Inventory.OffHand = item;
					break;
				case 6: // armor
					switch (slot)
					{
						case 0:
							_player.Inventory.Helmet = item;
							break;
						case 1:
							_player.Inventory.Chest = item;
							break;
						case 2:
							_player.Inventory.Leggings = item;
							break;
						case 3:
							_player.Inventory.Boots = item;
							break;
					}
					break;
				case 7: // chest/container
				case 24: // furnace
				case 25: // furnace
				case 26: // furnace
				case 45: // blast furnace
					if (_player._openInventory is Inventory inventory) inventory.SetSlot(_player, (byte) slot, item);
					break;
				default:
					Log.Warn($"Unknown containerId: {containerId}");
					break;
			}
		}
	}
}