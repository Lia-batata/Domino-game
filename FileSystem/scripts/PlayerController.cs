using Godot;

namespace Domino3D;

public partial class PlayerController : CharacterBody3D
{
	[ExportGroup("Movimento")]
	[Export] public float MoveSpeed { get; set; } = 5.0f;
	[Export] public float JumpVelocity { get; set; } = 4.5f;

	[ExportGroup("Câmera / Mouse")]
	[Export] public SpringArm3D SpringArm { get; set; }
	[Export] public float MouseSensitivity { get; set; } = 0.003f;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		// Trava o mouse no centro da tela para girar a câmera livremente
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Movimento do mouse para orbitar a câmera ao redor do jogador
		if (@event is InputEventMouseMotion mouseMotion && SpringArm != null)
		{
			Vector3 rot = SpringArm.Rotation;
			rot.X = Mathf.Clamp(rot.X - mouseMotion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-75f), Mathf.DegToRad(30f));
			rot.Y -= mouseMotion.Relative.X * MouseSensitivity;
			SpringArm.Rotation = rot;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Adiciona a gravidade
		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}

		// Pulo básico (Espaço)
		if (Input.IsKeyPressed(Key.Space) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Leitura direta de WASD e Setas
		float inputX = 0f;
		float inputZ = 0f;

		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) inputZ -= 1f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) inputZ += 1f;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) inputX -= 1f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) inputX += 1f;

		Vector2 inputDir = new Vector2(inputX, inputZ);
		if (inputDir.LengthSquared() > 0)
		{
			inputDir = inputDir.Normalized();
		}

		// Direção baseada na rotação da câmera (SpringArm)
		Vector3 direction = Vector3.Zero;
		if (SpringArm != null)
		{
			float camRotY = SpringArm.GlobalRotation.Y;
			Vector3 forward = new Vector3(-Mathf.Sin(camRotY), 0, -Mathf.Cos(camRotY));
			Vector3 right = new Vector3(Mathf.Cos(camRotY), 0, -Mathf.Sin(camRotY));

			direction = (forward * -inputDir.Y + right * inputDir.X).Normalized();
		}
		else
		{
			direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
		}

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * MoveSpeed;
			velocity.Z = direction.Z * MoveSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
