﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static ConnectBot.BitBoardHelpers;
using static ConnectBot.LogicalBoardHelpers;

namespace ConnectBot
{
    public class ConnectAI
    {
        protected BitBoard GameBoard { get; set; }

        protected DiscColor AiColor { get; set; }
        protected DiscColor OpponentColor { get; set; }

        /// <summary>
        /// Used for counting the total number of nodes that were explored
        /// during a given search iteration. Used for diagnostics and performance measuring.
        /// </summary>
        class NodeCounter
        {
            public int TotalNodes { get; private set; }

            public NodeCounter()
            {
                TotalNodes = 0;
            }

            public void Increment()
            {
                TotalNodes++;
            }
        }

        /// <summary>
        /// Used to store the window edges Alpha and Beta
        /// when performing an Alpha Beta search.
        /// </summary>
        class AlphaBeta
        {
            public decimal Alpha { get; set; }
            public decimal Beta { get; set; }

            public AlphaBeta(
                decimal alpha = decimal.MinValue, 
                decimal beta = decimal.MaxValue)
            {
                Alpha = alpha;
                Beta = beta;
            }
        }

        /// <summary>
        /// Holds a killer (winning) move column and the
        /// disc color that move would benefit.
        /// </summary>
        class KillerMove
        {
            public bool HasWinner => Column != -1;
            public int Column { get; set; }
            public DiscColor Color { get; set; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="color">Color for AI to play.</param>
        public ConnectAI(DiscColor color)
        {
            AiColor = color;
            OpponentColor = ChangeTurnColor(AiColor);
        }
        
        public void UpdateBoard(BitBoard board)
        {
            GameBoard = board;
        }

        public async Task<int> Move()
        {
            // Look for wins before performing in depth searches
            var aiWinningMove = FindKillerMove(GameBoard, AiColor);

            if (aiWinningMove.HasWinner)
                return aiWinningMove.Column;

            // Ensure we block the opponents winning moves
            var opponentWinningMove = FindKillerMove(GameBoard, OpponentColor);

            if (opponentWinningMove.HasWinner)
                return opponentWinningMove.Column;

            int retMove = MinimaxCutoffSearch(GameBoard);

            return retMove;
        }

        // TODO Killer move needs to be used during the search to ensure that the opponent
        // cannot win after a given move
        KillerMove FindKillerMove(BitBoard board, DiscColor disc)
        {
            foreach (var openColumn in GetOpenColumns(board))
            {
                var movedBoard = BitBoardMove(in board, openColumn, disc);

                if (CheckVictory(movedBoard) == disc)
                    return new KillerMove()
                    {
                        Column = openColumn,
                        Color = disc
                    };
            }

            return new KillerMove()
            {
                Column = -1,
                Color = DiscColor.None
            };
        }

        /// <summary>
        /// Min max searching algorithm with defined cutoff depth.
        /// </summary>
        /// <returns>The column that will be moved played in.</returns>
        private int MinimaxCutoffSearch(BitBoard board)
        {
            int maxDepth = 10;

            // TODO change this based on the AI's color when color selection menu is used
            // for all actions return min value of the result of the action
            var minimumMoveValue = decimal.MaxValue;
            var movedColumn = -1;
            var alphaBeta = new AlphaBeta();
            var nodeCounter = new NodeCounter();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (int openMove in GetOpenColumns(in board))
            {
                var newState = BitBoardMove(in board, openMove, AiColor);
                
                // prompt for opponents first move in the searching
                var openMoveValue = MaxValue(newState, maxDepth, alphaBeta, nodeCounter /*, ChangeTurnColor(AiColor)*/);
                Console.WriteLine($"Column {openMove} had a score of {openMoveValue}.");
                
                if (openMoveValue < minimumMoveValue)
                {
                    minimumMoveValue = openMoveValue;
                    movedColumn = openMove;
                }
            }

            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed;

            var elapsedTime = String.Format("Searched for {0:00}.{1:0000} seconds",
                elapsed.Seconds, elapsed.Milliseconds);
            Console.WriteLine(elapsedTime);

            Console.WriteLine($"Column {movedColumn} was chosen.");
            Console.WriteLine($"{nodeCounter.TotalNodes} were explored.");

            return movedColumn;
        }

        private decimal MaxValue(
            BitBoard board, 
            int depth,
            AlphaBeta alphaBeta,
            NodeCounter nodeCounter)
        {
            nodeCounter.Increment();

            var openColumns = GetOpenColumns(in board);

            if (depth <= 0 ||
                // TODO this should represent a drawn game, we may want to
                // return something other than an evaluation here
                openColumns.Count == 0)
            {
                //Console.WriteLine($"Evaluated at depth {depth} for board:");
                //Console.WriteLine(GetPrettyPrint(in board));
                return EvaluateBoardState(in board);
            }

            if (CheckVictory(in board) != DiscColor.None)
                return EvaluateBoardState(in board);

            decimal maximumMoveValue = decimal.MinValue;

            foreach (int openMove in openColumns)
            {
                var newState = BitBoardMove(in board, openMove, OpponentColor);

                maximumMoveValue = Math.Max(
                    maximumMoveValue,
                    MinValue(newState, depth - 1, alphaBeta, nodeCounter /*AiColor*/));

                if (maximumMoveValue >= alphaBeta.Beta)
                    return maximumMoveValue;

                alphaBeta.Alpha = Math.Max(alphaBeta.Alpha, maximumMoveValue);
            }

            return maximumMoveValue;
        }

        private decimal MinValue(
            BitBoard board, 
            int depth,
            AlphaBeta alphaBeta,
            NodeCounter nodeCounter)
        {
            nodeCounter.Increment();

            var openColumns = GetOpenColumns(in board);

            if (depth <= 0 ||
                openColumns.Count == 0)
            {
                //Console.WriteLine($"Evaluated at depth {depth} for board:");
                //Console.WriteLine(GetPrettyPrint(in board));
                return EvaluateBoardState(in board);
            }

            if (CheckVictory(in board) != DiscColor.None)
                return EvaluateBoardState(in board);

            decimal minimumMoveValue = decimal.MaxValue;

            foreach (int openMove in openColumns)
            {
                var newState = BitBoardMove(in board, openMove, AiColor);

                minimumMoveValue = Math.Min(
                    minimumMoveValue,
                    MaxValue(newState, depth - 1, alphaBeta, nodeCounter /*OpponentColor*/));

                if (minimumMoveValue <= alphaBeta.Alpha)
                    return minimumMoveValue;

                alphaBeta.Beta = Math.Min(alphaBeta.Beta, minimumMoveValue);
            }

            return minimumMoveValue;
        }
    }
}
