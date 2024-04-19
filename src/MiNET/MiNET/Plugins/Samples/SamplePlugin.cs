using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Plugins.Attributes;
using MiNET.Worlds;

namespace MiNET.Plugins.Samples
{
	[Plugin]
	internal class SamplePlugin : Plugin
	{
		[Command]
		public void gm(Player player, GameMode gamemode, string nickname = null)
		{
			if (String.IsNullOrEmpty(nickname))
				player.SetGameMode(gamemode);
			else
			{
				var selectedPlayer = player.Level.GetAllPlayers().Where(p => p.Username.Equals(nickname)).FirstOrDefault();
				if (selectedPlayer != null)
					selectedPlayer.SetGameMode(gamemode);
			}
		}

		[Command]
		public void attr(Player player, string name = "", int value = 0)
		{
			if (String.IsNullOrEmpty(name))
			{
				EntityAttributes attributes = player.GetEntityAttributes();
				foreach (var attr in attributes)
				{
					player.SendMessage(attr.ToString());
				}
			} else
			{
			}

		}

		[Command]
		public void seth(Player player, int health)
		{
			player.SetHealth(health);
		}
	}
}
