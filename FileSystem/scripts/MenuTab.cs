using Godot;
using System;

public partial class MenuTab : TextureRect
{
	private WelcomeMenu mainMenu;

	public override void _Ready()
	{
		mainMenu = GetParent() as WelcomeMenu;

		if (mainMenu == null)
		{
			GD.PrintErr(
                "ERRO: MenuTab não encontrou WelcomeMenu no nó pai."
			);
		}
	}

	public void OnMenuSwapButtonPressed(int swapIndex)
	{
		if (mainMenu == null)
			return;

		mainMenu.SwapMenu(swapIndex, GetIndex());
		Visible = false;
	}

	public void OnMenuReturnButtonPressed()
	{
		if (mainMenu == null)
			return;

		mainMenu.SwapMenuToPrevious();
		Visible = false;
	}

	public void LoadSceneRequest(PackedScene loadScene)
	{
		if (mainMenu == null)
		{
			GD.PrintErr(
                "ERRO: MenuTab não possui referência ao WelcomeMenu."
			);
			return;
		}

		if (loadScene == null)
		{
			GD.PrintErr(
                "ERRO: Nenhuma cena foi configurada no botão Play."
			);
			return;
		}

		mainMenu.OnSwapScene(loadScene);
	}
}
