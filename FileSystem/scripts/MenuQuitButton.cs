using Godot;
using System;

public partial class MenuQuitButton : Button
{
	public override void _Ready()
	{
		Pressed += OnQuitButtonPressed;
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
