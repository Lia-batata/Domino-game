using Godot;
using System;

namespace Domino3D;

public enum CameraState
{
	Ambiente,  // 3ª pessoa: andando livremente pelo boteco
	Transicao, // Transicionando suavemente entre os estados
	Partida    // 1ª pessoa: sentado na mesa de dominó
}

public partial class CameraController : Camera3D
{
	// --- Sinais (Events) ---
	[Signal] public delegate void TransitionToPartidaCompletedEventHandler();
	[Signal] public delegate void TransitionToAmbienteCompletedEventHandler();
	[Signal] public delegate void CameraStateChangedEventHandler(int newState);

	// --- Estado Atual ---
	[ExportGroup("Estado")]
	[Export] public CameraState CurrentState { get; private set; } = CameraState.Ambiente;

	// --- Configurações de 3ª Pessoa (Ambiente) ---
	[ExportGroup("3ª Pessoa (Ambiente)")]
	[Export] public Node3D ThirdPersonTarget { get; set; }
	[Export] public Vector3 ThirdPersonOffset { get; set; } = new Vector3(0, 1.8f, 2.5f);
	[Export] public float MouseSensitivity3D { get; set; } = 0.003f;

	// --- Configurações de 1ª Pessoa Sentado (Partida) ---
	[ExportGroup("1ª Pessoa Sentado (Partida)")]
	[Export] public Node3D FirstPersonSeatTransform { get; set; }
	[Export] public float MouseSensitivity1D { get; set; } = 0.002f;
	[Export] public float MaxYawDegrees { get; set; } = 50f;     // Limite horizontal (Esquerda / Direita)
	[Export] public float MinPitchDegrees { get; set; } = -35f;  // Limite vertical inferior (Olhar para a mesa)
	[Export] public float MaxPitchDegrees { get; set; } = 25f;   // Limite vertical superior (Olhar para oponentes)

	// --- Configurações de Juice / Feedback ---
	[ExportGroup("Juice / Efeitos")]
	[Export] public float DefaultFov { get; set; } = 75f;
	[Export] public float EmphasisFov { get; set; } = 55f;
	[Export] public float ShakeIntensity { get; set; } = 0.08f;
	[Export] public float ShakeDuration { get; set; } = 0.25f;

	// --- Ângulos do Olhar em 1ª Pessoa (Radianos) ---
	private float _currentYaw = 0f;
	private float _currentPitch = 0f;

	// --- Ângulos da Câmera em 3ª Pessoa (Radianos) ---
	private float _orbitYaw = 0f;
	private float _orbitPitch = 0f;

	// --- Variáveis Internas para Tweens / Offsets ---
	private Tween _transitionTween;
	private Tween _emphasisTween;
	private Tween _shakeTween;

	private Vector3 ShakeOffset { get; set; } = Vector3.Zero;
	private Vector3 EmphasisLookOffset { get; set; } = Vector3.Zero;
	private float EmphasisFovOffset { get; set; } = 0f;
	private bool _isEmphasisActive = false;

	public override void _Ready()
	{
		Fov = DefaultFov;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (CurrentState == CameraState.Ambiente)
			{
				Handle3rdPersonMouseLook(mouseMotion.Relative);
			}
			else if (CurrentState == CameraState.Partida && !_isEmphasisActive)
			{
				Handle1stPersonMouseLook(mouseMotion.Relative);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (CurrentState == CameraState.Ambiente)
		{
			Update3rdPersonTransform();
		}
		else if (CurrentState == CameraState.Partida)
		{
			Update1stPersonTransform();
		}
	}

	#region Estado Ambiente (3ª Pessoa)
	private void Handle3rdPersonMouseLook(Vector2 relative)
	{
		_orbitYaw -= relative.X * MouseSensitivity3D;
		_orbitPitch -= relative.Y * MouseSensitivity3D;
		_orbitPitch = Mathf.Clamp(_orbitPitch, Mathf.DegToRad(-30f), Mathf.DegToRad(60f));
	}

	private void Update3rdPersonTransform()
	{
		if (ThirdPersonTarget == null) return;

		Basis orbitBasis = Basis.FromEuler(new Vector3(_orbitPitch, _orbitYaw, 0f));
		GlobalPosition = ThirdPersonTarget.GlobalPosition + orbitBasis * ThirdPersonOffset;
		LookAt(ThirdPersonTarget.GlobalPosition + Vector3.Up * 1.2f);
	}
	#endregion

	#region Estado Partida (1ª Pessoa)
	private void Handle1stPersonMouseLook(Vector2 relative)
	{
		_currentYaw -= relative.X * MouseSensitivity1D;
		_currentPitch -= relative.Y * MouseSensitivity1D;

		// Limita o olhar (Clamp) nos eixos Horizontal (Yaw) e Vertical (Pitch)
		_currentYaw = Mathf.Clamp(_currentYaw, Mathf.DegToRad(-MaxYawDegrees), Mathf.DegToRad(MaxYawDegrees));
		_currentPitch = Mathf.Clamp(_currentPitch, Mathf.DegToRad(MinPitchDegrees), Mathf.DegToRad(MaxPitchDegrees));
	}

	private void Update1stPersonTransform()
	{
		if (FirstPersonSeatTransform == null) return;

		// Transform base da cadeira/posição do jogador sentado
		Transform3D seatTransform = FirstPersonSeatTransform.GlobalTransform;

		// Rotação local do olhar do jogador
		Basis headRotation = Basis.FromEuler(new Vector3(_currentPitch, _currentYaw, 0f));

		// Aplica a rotação do olhar e adiciona offsets de Shake e Emphasis
		GlobalTransform = new Transform3D(
			seatTransform.Basis * headRotation,
			seatTransform.Origin + ShakeOffset + EmphasisLookOffset
		);

		Fov = DefaultFov - EmphasisFovOffset;
	}
	#endregion

	#region Métodos Públicos e Transições
	/// <summary>
	/// Inicia a transição suave de 3ª Pessoa (Ambiente) para 1ª Pessoa (Mesa de Dominó).
	/// </summary>
	public void TransitionToPartida(float duration = 1.5f)
	{
		if (FirstPersonSeatTransform == null)
		{
			GD.PushError("CameraController: 'FirstPersonSeatTransform' não foi atribuído no Inspector!");
			return;
		}

		CurrentState = CameraState.Transicao;
		EmitSignal(SignalName.CameraStateChanged, (int)CurrentState);

		// Reseta o olhar centralizado na mesa
		_currentYaw = 0f;
		_currentPitch = 0f;

		Transform3D targetTransform = FirstPersonSeatTransform.GlobalTransform;

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();

		_transitionTween = CreateTween()
			.SetParallel(true)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		_transitionTween.TweenProperty(this, "global_transform", targetTransform, duration);
		_transitionTween.TweenProperty(this, nameof(Fov), DefaultFov, duration);

		_transitionTween.Chain().TweenCallback(Callable.From(() =>
		{
			CurrentState = CameraState.Partida;
			EmitSignal(SignalName.CameraStateChanged, (int)CurrentState);
			EmitSignal(SignalName.TransitionToPartidaCompleted);
		}));
	}

	/// <summary>
	/// Transição suave de volta para o estado Ambiente (3ª Pessoa).
	/// </summary>
	public void TransitionToAmbiente(float duration = 1.5f)
	{
		CurrentState = CameraState.Transicao;
		EmitSignal(SignalName.CameraStateChanged, (int)CurrentState);

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();

		_transitionTween = CreateTween()
			.SetParallel(true)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		Basis orbitBasis = Basis.FromEuler(new Vector3(_orbitPitch, _orbitYaw, 0f));
		Vector3 targetPos = ThirdPersonTarget != null 
			? ThirdPersonTarget.GlobalPosition + orbitBasis * ThirdPersonOffset 
			: GlobalPosition;

		_transitionTween.TweenProperty(this, "global_position", targetPos, duration);

		_transitionTween.Chain().TweenCallback(Callable.From(() =>
		{
			CurrentState = CameraState.Ambiente;
			EmitSignal(SignalName.CameraStateChanged, (int)CurrentState);
			EmitSignal(SignalName.TransitionToAmbienteCompleted);
		}));
	}

	/// <summary>
	/// Executa um leve zoom/pan em direção a uma posição de interesse (jogada, habilidade ativa, etc).
	/// </summary>
	public void PlayEmphasis(Vector3 targetPosition, float duration = 1.2f)
	{
		if (CurrentState != CameraState.Partida) return;

		_isEmphasisActive = true;

		if (_emphasisTween != null && _emphasisTween.IsValid())
			_emphasisTween.Kill();

		float halfDuration = duration * 0.5f;
		float fovDelta = DefaultFov - EmphasisFov;

		// Leve deslocamento (pan) em direção ao alvo
		Vector3 dirToTarget = (targetPosition - GlobalPosition).Normalized() * 0.15f;

		_emphasisTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

		// 1. Zoom In & Pan em direção ao alvo
		_emphasisTween.Parallel().TweenProperty(this, nameof(EmphasisFovOffset), fovDelta, halfDuration);
		_emphasisTween.Parallel().TweenProperty(this, nameof(EmphasisLookOffset), dirToTarget, halfDuration);

		// 2. Retorno suave ao foco e FOV original
		_emphasisTween.Chain().TweenProperty(this, nameof(EmphasisFovOffset), 0f, halfDuration);
		_emphasisTween.Parallel().TweenProperty(this, nameof(EmphasisLookOffset), Vector3.Zero, halfDuration);

		_emphasisTween.Chain().TweenCallback(Callable.From(() =>
		{
			_isEmphasisActive = false;
		}));
	}

	/// <summary>
	/// Executa um tremor leve e rápido na câmera ao tentar uma jogada inválida.
	/// </summary>
	public void PlayInvalidShake()
	{
		if (_shakeTween != null && _shakeTween.IsValid())
			_shakeTween.Kill();

		_shakeTween = CreateTween();
		int steps = 6;
		float stepDuration = ShakeDuration / steps;

		RandomNumberGenerator rng = new();
		rng.Randomize();

		for (int i = 0; i < steps; i++)
		{
			Vector3 offset = new Vector3(
				rng.RandfRange(-ShakeIntensity, ShakeIntensity),
				rng.RandfRange(-ShakeIntensity, ShakeIntensity),
				0f
			);
			_shakeTween.TweenProperty(this, nameof(ShakeOffset), offset, stepDuration);
		}

		_shakeTween.TweenProperty(this, nameof(ShakeOffset), Vector3.Zero, stepDuration);
	}
	#endregion
}
