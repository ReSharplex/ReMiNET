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
		public void gm(Player player, GameMode gamemode)
		{
			player.SetGameMode(gamemode);
		}
	}
}
