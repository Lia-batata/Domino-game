using Godot;
using System;

public partial class MenuTab : TextureRect
{
	// Called when the node enters the scene tree for the first time.
	private MainMenuManager mainMenu;

	public override void _Ready(){
		if(GetParent()is WelcomeMenu) {
			mainMenu = GetParent() as WelcomeMenu; 
		}
	}

	public void OnMenuSwapButtonPressed(int swapIndex){
		mainMenu.SwapMenu(swapIndex, GetIndex());
		Visible = false;
	}

	public void OnMenuReturnButtonPressed(){
		mainMenu.SwapMenuToPrevious();
		Visible = false;
	}

	public void LoadSceneRequest(PackedScene loadScene){
		mainMenu.OnSwapScene(loadScene);
	}

}
