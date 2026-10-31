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
	public override void _UnhandledInput(InputEvent @event)
    {
        // Detecta a tecla ESC (ação "ui_cancel") para abrir/fechar o menu de pausa
        if (@event.IsActionPressed("ui_cancel"))
        {
            TogglePause();
        }
    }
	public void TogglePause()
    {
        isPaused = !isPaused;
        Visible = isPaused;
        GetTree().Paused = isPaused; // Pausa/despausa o mundo 3D do jogo

        if (isPaused)
        {
            // Libera o mouse para poder clicar nos botões do menu
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            // Retoma a captura do mouse para o gameplay
            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            // Fecha o painel de configurações se o jogador voltar ao jogo
            if (settingsPanel != null)
                settingsPanel.Visible = false;
        }
    }
	private void OnSettingsButtonPressed()
    {
        if (settingsPanel != null)
        {
            // Alterna a visibilidade do painel 2D de configurações
            settingsPanel.Visible = !settingsPanel.Visible;
        }
        else
        {
            GD.PrintErr("ERRO: Painel de configurações não foi atribuído no PauseMenuController.");
        }
    }
}
