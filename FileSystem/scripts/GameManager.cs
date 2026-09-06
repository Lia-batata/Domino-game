using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domino3D;

public partial class GameManager : Node3D
{
	// --- Sinais (Events) para UI e Câmera ---
	[Signal] public delegate void GameStartedEventHandler(int targetPoints, string stockMode);
	[Signal] public delegate void RoundStartedEventHandler(int roundNumber, int startingPlayer);
	[Signal] public delegate void TurnChangedEventHandler(int playerIndex);
	[Signal] public delegate void PiecePlayedEventHandler(int playerIndex, int pieceId, int sideA, int sideB, int endpointValue);
	[Signal] public delegate void PieceDrawnEventHandler(int playerIndex, int remainingStockCount);
	[Signal] public delegate void PlayerPassedEventHandler(int playerIndex);
	[Signal] public delegate void RoundEndedEventHandler(int winnerIndex, int pointsEarned, bool isBlocked);
	[Signal] public delegate void GameOverEventHandler(int matchWinnerIndex, Godot.Collections.Dictionary finalScores);

	// --- Configurações da Partida ---
	public StockMode CurrentStockMode { get; private set; } = StockMode.Fechado;
	public int TargetPoints { get; private set; } = 100;

	// --- Estado do Jogo ---
	public int CurrentPlayerIndex { get; private set; } = 0;
	public int RoundNumber { get; private set; } = 0;
	public bool IsGameActive { get; private set; } = false;

	private readonly int[] _playerScores = new int[4];
	private readonly List<DominoPieceData>[] _playerHands = new List<DominoPieceData>[4];
	private readonly List<DominoPieceData> _stock = new();
	
	// Armazena as pontas abertas na mesa (ex: [3, 5]). Vazio no primeiro turno.
	public List<int> ActiveEndpoints { get; private set; } = new();

	public override void _Ready()
	{
		for (int i = 0; i < 4; i++)
		{
			_playerHands[i] = new List<DominoPieceData>();
		}
	}

	/// <summary>
	/// Inicializa uma nova partida do zero.
	/// </summary>
	public void StartMatch(StockMode stockMode, int targetPoints)
	{
		CurrentStockMode = stockMode;
		TargetPoints = targetPoints;
		RoundNumber = 0;
		IsGameActive = true;

		Array.Clear(_playerScores, 0, _playerScores.Length);

		EmitSignal(SignalName.GameStarted, TargetPoints, CurrentStockMode.ToString());
		StartNewRound();
	}

	/// <summary>
	/// Inicia uma nova rodada (embaralha, distribui e define o primeiro jogador).
	/// </summary>
	private void StartNewRound()
	{
		RoundNumber++;
		ActiveEndpoints.Clear();

		// 1. Criar e embaralhar o dominó (28 peças)
		List<DominoPieceData> fullDeck = GenerateDoubleSixDeck();
		ShuffleDeck(fullDeck);

		// 2. Limpar mãos e estoque
		for (int i = 0; i < 4; i++)
		{
			_playerHands[i].Clear();
		}
		_stock.Clear();

		// 3. Distribuir 6 peças por jogador
		int deckIndex = 0;
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				_playerHands[i].Add(fullDeck[deckIndex++]);
			}
		}

		// 4. Guardar as 4 peças restantes no estoque
		while (deckIndex < fullDeck.Count)
		{
			_stock.Add(fullDeck[deckIndex++]);
		}

		// 5. Determinar quem começa
		CurrentPlayerIndex = DetermineStartingPlayer();

		EmitSignal(SignalName.RoundStarted, RoundNumber, CurrentPlayerIndex);
		EmitSignal(SignalName.TurnChanged, CurrentPlayerIndex);
	}

	/// <summary>
	/// Avalia quem tem a peça de maior valor (priorizando carroças) para começar a rodada.
	/// </summary>
	private int DetermineStartingPlayer()
	{
		int startingPlayer = 0;
		int highestPriority = -1;

		for (int i = 0; i < 4; i++)
		{
			foreach (var piece in _playerHands[i])
			{
				int priority = piece.GetStartingPriority();
				if (priority > highestPriority)
				{
					highestPriority = priority;
					startingPlayer = i;
				}
			}
		}

		return startingPlayer;
	}

	/// <summary>
	/// Executa a jogada de uma peça no turno atual.
	/// </summary>
	public bool TryPlayPiece(int playerIndex, DominoPieceData piece, int targetEndpointValue)
	{
		if (!IsGameActive || playerIndex != CurrentPlayerIndex) return false;
		if (!_playerHands[playerIndex].Contains(piece)) return false;

		// Validação da jogada
		if (ActiveEndpoints.Count > 0)
		{
			if (!ActiveEndpoints.Contains(targetEndpointValue) || !piece.Matches(targetEndpointValue))
			{
				return false; // Peça incompatível com a ponta escolhida
			}

			// Atualiza as pontas da mesa
			ActiveEndpoints.Remove(targetEndpointValue);
			int newEndpoint = (piece.SideA == targetEndpointValue) ? piece.SideB : piece.SideA;
			ActiveEndpoints.Add(newEndpoint);
		}
		else
		{
			// Primeiro lance da rodada define as duas pontas iniciais
			ActiveEndpoints.Add(piece.SideA);
			ActiveEndpoints.Add(piece.SideB);
		}

		_playerHands[playerIndex].Remove(piece);
		EmitSignal(SignalName.PiecePlayed, playerIndex, piece.Id, piece.SideA, piece.SideB, targetEndpointValue);

		if (CheckRoundEnd(playerIndex)) return true;

		AdvanceTurn();
		return true;
	}

	/// <summary>
	/// Tenta comprar uma peça do estoque (Modo Aberto).
	/// </summary>
	public bool TryDrawFromStock(int playerIndex)
	{
		if (!IsGameActive || playerIndex != CurrentPlayerIndex) return false;
		if (CurrentStockMode != StockMode.Aberto || _stock.Count == 0) return false;

		DominoPieceData drawnPiece = _stock[0];
		_stock.RemoveAt(0);
		_playerHands[playerIndex].Add(drawnPiece);

		EmitSignal(SignalName.PieceDrawn, playerIndex, _stock.Count);
		return true;
	}

	/// <summary>
	/// Passa o turno do jogador atual caso ele não tenha jogadas válidas.
	/// </summary>
	public bool TryPassTurn(int playerIndex)
	{
		if (!IsGameActive || playerIndex != CurrentPlayerIndex) return false;

		// Impede passar a vez se ainda puder jogar ou se puder comprar no modo aberto
		if (HasValidMove(playerIndex)) return false;
		if (CurrentStockMode == StockMode.Aberto && _stock.Count > 0) return false;

		EmitSignal(SignalName.PlayerPassed, playerIndex);

		if (CheckRoundEnd(playerIndex)) return true;

		AdvanceTurn();
		return true;
	}

	/// <summary>
	/// Avança o turno para o próximo jogador na ordem do jogo.
	/// </summary>
	private void AdvanceTurn()
	{
		CurrentPlayerIndex = (CurrentPlayerIndex + 1) % 4;
		EmitSignal(SignalName.TurnChanged, CurrentPlayerIndex);
	}

	/// <summary>
	/// Verifica se o jogador possui alguma peça jogável na mão.
	/// </summary>
	public bool HasValidMove(int playerIndex)
	{
		if (ActiveEndpoints.Count == 0) return true; // Primeira jogada sempre é válida

		return _playerHands[playerIndex].Any(piece => 
			ActiveEndpoints.Any(endpoint => piece.Matches(endpoint)));
	}

	/// <summary>
	/// Verifica condições de fim de rodada: Bater ou Jogo Travado.
	/// </summary>
	private bool CheckRoundEnd(int lastPlayerIndex)
	{
		// 1. Alguém Bateu (ficou sem peças)
		if (_playerHands[lastPlayerIndex].Count == 0)
		{
			HandleRoundEnd(lastPlayerIndex, isBlocked: false);
			return true;
		}

		// 2. Verificar se o jogo travou (ninguém consegue jogar e estoque acabou/fechado)
		bool canAnyonePlay = false;
		for (int i = 0; i < 4; i++)
		{
			if (HasValidMove(i))
			{
				canAnyonePlay = true;
				break;
			}
		}

		bool stockEmptyOrClosed = CurrentStockMode == StockMode.Fechado || _stock.Count == 0;

		if (!canAnyonePlay && stockEmptyOrClosed)
		{
			int blockedWinner = DetermineBlockedRoundWinner();
			HandleRoundEnd(blockedWinner, isBlocked: true);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Em caso de fechamento/trava, vence quem tem a menor soma de pontos na mão.
	/// </summary>
	private int DetermineBlockedRoundWinner()
	{
		int winner = 0;
		int lowestScore = int.MaxValue;

		for (int i = 0; i < 4; i++)
		{
			int handSum = _playerHands[i].Sum(p => p.TotalValue);
			if (handSum < lowestScore)
			{
				lowestScore = handSum;
				winner = i;
			}
		}

		return winner;
	}

	/// <summary>
	/// Consolida a pontuação da rodada e verifica se a partida acabou.
	/// </summary>
	private void HandleRoundEnd(int winnerIndex, bool isBlocked)
	{
		// Calcula pontos da vitória (soma das peças restantes das mãos dos oponentes)
		int pointsEarned = 0;
		for (int i = 0; i < 4; i++)
		{
			if (i != winnerIndex)
			{
				pointsEarned += _playerHands[i].Sum(p => p.TotalValue);
			}
		}

		_playerScores[winnerIndex] += pointsEarned;
		EmitSignal(SignalName.RoundEnded, winnerIndex, pointsEarned, isBlocked);

		// Verifica se atingiu a meta de pontos da partida
		if (_playerScores[winnerIndex] >= TargetPoints)
		{
			IsGameActive = false;
			
			var scoresDict = new Godot.Collections.Dictionary();
			for (int i = 0; i < 4; i++) scoresDict[i] = _playerScores[i];

			EmitSignal(SignalName.GameOver, winnerIndex, scoresDict);
		}
		else
		{
			// Inicia nova rodada em seguida
			StartNewRound();
		}
	}

	#region Utilitários
	private List<DominoPieceData> GenerateDoubleSixDeck()
	{
		var deck = new List<DominoPieceData>();
		int id = 0;
		for (int i = 0; i <= 6; i++)
		{
			for (int j = i; j <= 6; j++)
			{
				deck.Add(new DominoPieceData(id++, i, j));
			}
		}
		return deck;
	}

	private void ShuffleDeck(List<DominoPieceData> deck)
	{
		Random rng = new Random();
		int n = deck.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			(deck[k], deck[n]) = (deck[n], deck[k]);
		}
	}

	public List<DominoPieceData> GetPlayerHand(int playerIndex) => new(_playerHands[playerIndex]);
	public int GetPlayerScore(int playerIndex) => _playerScores[playerIndex];
	public int GetStockCount() => _stock.Count;
	#endregion
}
