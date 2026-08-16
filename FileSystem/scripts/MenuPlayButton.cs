using Godot;
using System;

public partial class MenuPlayButton : Button
{
	[Export]
	public PackedScene SceneToSwitchTo { get; set; }

	public override void _Ready()
	{
		Pressed += OnPlayButtonPressed;
	}

	private void OnPlayButtonPressed()
	{
		if (GetParent().GetParent() is MenuTab menuTab)
		{
			menuTab.LoadSceneRequest(SceneToSwitchTo);
		}
	}
}
