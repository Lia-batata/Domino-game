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
	
	public override void _Ready()
    {
        // Garante que o menu começa invisível e que ele continua processando mesmo com o jogo pausado
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        if (settingsPanel != null)
            settingsPanel.Visible = false;

        // Conecta os botões via código automaticamente
        if (resumeButton != null) resumeButton.Pressed += TogglePause;
        if (settingsButton != null) settingsButton.Pressed += OnSettingsButtonPressed;
        if (mainMenuButton != null) mainMenuButton.Pressed += OnMainMenuButtonPressed;
        if (quitButton != null) quitButton.Pressed += OnQuitButtonPressed;
    }
}
