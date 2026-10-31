using Godot;
using System;

public partial class PauseMenuController : Control
{
    [ExportGroup("Referências de Cenas e Painéis")]
    [Export] private PackedScene mainMenuScene; 
    [Export] private Control settingsPanel;     // O painel 2D de configurações

    [ExportGroup("Botões do Menu de Pausa")]
    [Export] private Button resumeButton;
    [Export] private Button settingsButton;
    [Export] private Button mainMenuButton;
    [Export] private Button quitButton;

    private bool isPaused = false;
}
