namespace Domino3D;

public readonly struct DominoPieceData
{
	public int Id { get; }
	public int SideA { get; }
	public int SideB { get; }

	public DominoPieceData(int id, int sideA, int sideB)
	{
		Id = id;
		SideA = sideA;
		SideB = sideB;
	}

	public bool IsDouble => SideA == SideB;
	public int TotalValue => SideA + SideB;

	public bool Matches(int value) => SideA == value || SideB == value;

	/// <summary>
	/// Retorna o peso da peça para determinação do primeiro a jogar.
	/// Carroças recebem um bônus para garantir prioridade total sobre peças normais.
	/// </summary>
	public int GetStartingPriority()
	{
		return IsDouble ? 100 + TotalValue : TotalValue;
	}

	public override string ToString() => $"[{SideA}|{SideB}]";
}

public enum StockMode
{
	Fechado,
	Aberto
}
