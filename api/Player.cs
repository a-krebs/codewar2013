using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;

namespace PlayerCSharpAI.api
{
    public class Player
    {
    	private Player(XElement elemPlayer)
    	{
    		Guid = elemPlayer.Attribute("guid").Value;
			Name = elemPlayer.Attribute("name").Value;
			Limo = new Limo(new Point(Convert.ToInt32(elemPlayer.Attribute("limo-x").Value), Convert.ToInt32(elemPlayer.Attribute("limo-y").Value)), Convert.ToInt32(elemPlayer.Attribute("limo-angle").Value));
			PickUp = new List<Passenger>();
			PassengersDelivered = new List<Passenger>();
    	}

    	/// <summary>
        /// The unique identifier for this player. This will remain constant for the length of the game (while the Player objects passed will
        /// change on every call).
        /// </summary>
        public string Guid { get; private set; }

		/// <summary>
		/// The name of the player.
		/// </summary>
		public string Name { get; private set; }

        /// <summary>
        /// Who to pick up at the next bus stop. Can be empty and can also only list people not there.
        /// This may be wrong after a pick-up occurs as all we get is a count. This is updated with the
        /// most recent list sent to the server.
        /// </summary>
        public List<Passenger> PickUp { get; private set; }

        /// <summary>
        /// The passengers delivered - this game.
        /// </summary>
        public List<Passenger> PassengersDelivered { get; private set; }

        /// <summary>
        /// The player's limo.
        /// </summary>
        public Limo Limo { get; private set; }

		/// <summary>
		/// The score for this player (this game, not across all games so far).
		/// </summary>
    	public float Score { get; private set; }

    	/// <summary>
		/// Called on setup to create initial list of players.
		/// </summary>
		/// <param name="elemPlayers">The xml with all the players.</param>
		/// <returns>The created list of players.</returns>
    	public static List<Player> FromXml(XElement elemPlayers)
    	{
			List<Player> players = new List<Player>();
    		foreach (XElement elemPlyrOn in elemPlayers.Elements("player"))
				players.Add(new Player(elemPlyrOn));
    		return players;
    	}

		public static void UpdateFromXml(List<Player> players, List<Passenger> passengers, XElement elemPlayers)
		{
			foreach (XElement elemPlyrOn in elemPlayers.Elements("player"))
			{
				Player plyrOn = players.Find(pl => pl.Guid == elemPlyrOn.Attribute("guid").Value);

				plyrOn.Score = Convert.ToSingle(elemPlyrOn.Attribute("score").Value);

				// car location
				plyrOn.Limo.TilePosition = new Point(Convert.ToInt32(elemPlyrOn.Attribute("limo-x").Value),
				                                    Convert.ToInt32(elemPlyrOn.Attribute("limo-y").Value));
				plyrOn.Limo.Angle = Convert.ToInt32(elemPlyrOn.Attribute("limo-angle").Value);
				
				// see if we now have a passenger.
				XAttribute attrPassenger = elemPlyrOn.Attribute("passenger");
				if (attrPassenger != null)
				{
					Passenger passenger = passengers.Find(ps => ps.Name == attrPassenger.Value);
					plyrOn.Limo.Passenger = passenger;
					passenger.Car = plyrOn.Limo;
				} else
					plyrOn.Limo.Passenger = null;

				// add most recent delivery if we this is the first time we're told.
				attrPassenger = elemPlyrOn.Attribute("last-delivered");
				if (attrPassenger != null)
				{
					Passenger passenger = passengers.Find(ps => ps.Name == attrPassenger.Value);
					if (! plyrOn.PassengersDelivered.Contains(passenger))
						plyrOn.PassengersDelivered.Add(passenger);
				}
			}
		}

    	public override string ToString()
    	{
    		return string.Format("{0}; NumDelivered:{1}", Name, PassengersDelivered.Count);
    	}
    }
}
