using System.Collections.Generic;
using System.Drawing;
using PlayerCSharpAI.api;

namespace PlayerCSharpAI.AI
{
	public interface IPlayerAI
	{

		/// <summary>
		/// Called when your robot must be placed on the board. This is called at the start of the game.
		/// </summary>
		/// <param name="map">The game map.</param>
		/// <param name="me">My Player object. This is also in the players list.</param>
		/// <param name="players">All players (including you).</param>
		/// <param name="companies">The companies on the map.</param>
		/// <param name="passengers">The passengers that need a lift.</param>
		/// <param name="ordersEvent">Method to call to send orders to the server.</param>
		void Setup(Map map, Player me, List<Player> players, List<Company> companies, List<Passenger> passengers,
				   PlayerAIBase.PlayerOrdersEvent ordersEvent);

		/// <summary>
		/// Called to send an update message to this A.I. We do NOT have to reply to it.
		/// </summary>
		/// <param name="status">The status message.</param>
		/// <param name="plyrStatus">The status of my player.</param>
		/// <param name="players">The status of all players.</param>
		/// <param name="passengers">The status of all passengers.</param>
		void GameStatus(PlayerAIBase.STATUS status, Player plyrStatus, List<Player> players, List<Passenger> passengers);
	}

	public class PlayerAIBase
	{
		public delegate void PlayerOrdersEvent(string order, List<Point> path, List<Passenger> pickUp);

		public enum STATUS
		{
			/// <summary>
			/// Called ever N ticks to update the AI with the game status.
			/// </summary>
			UPDATE,
			/// <summary>
			/// The car has no path.
			/// </summary>
			NO_PATH,
			/// <summary>
			/// The passenger was abandoned, no passenger was picked up.
			/// </summary>
			PASSENGER_ABANDONED,
			/// <summary>
			/// The passenger was delivered, no passenger was picked up.
			/// </summary>
			PASSENGER_DELIVERED,
			/// <summary>
			/// The passenger was delivered or abandoned, a new passenger was picked up.
			/// </summary>
			PASSENGER_DELIVERED_AND_PICKED_UP,
			/// <summary>
			/// The passenger refused to exit at the bus stop because an enemy was there.
			/// </summary>
			PASSENGER_REFUSED,
			/// <summary>
			/// A passenger was picked up. There was no passenger to deliver.
			/// </summary>
			PASSENGER_PICKED_UP,
			/// <summary>
			/// At a bus stop, nothing happened (no drop off, no pick up).
			/// </summary>
			PASSENGER_NO_ACTION
		}
	}
}
