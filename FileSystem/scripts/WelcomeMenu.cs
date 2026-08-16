using Godot;
using System;
using System.Collections.Generic;

public partial class WelcomeMenu : CanvasLayer
{
	private List<int> goBackList = new();

	public void SwapMenu(int menuIndex, int returnIndex)
	{
		if (GetChild(menuIndex) is MenuTab menuTab)
		{
			menuTab.Visible = true;
		}

		if (returnIndex < 0)
			return;

		goBackList.Add(returnIndex);
	}

	public void SwapMenuToPrevious()
	{
		if (goBackList.Count == 0)
			return;

		SwapMenu(goBackList[goBackList.Count - 1], -1);

		goBackList.RemoveAt(goBackList.Count - 1);
	}

	public void OnSwapScene(PackedScene loadScene)
	{
		if (loadScene == null)
		{
			GD.PrintErr("ERRO: Nenhuma cena foi configurada no botão Play.");
			return;
		}

		Node newScene = loadScene.Instantiate();

		GetTree().Root.AddChild(newScene);

		QueueFree();
	}
}
