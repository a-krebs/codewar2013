// Created by Windward Studios, Inc. (www.windward.net). No copyright claimed - do anything you want with this code.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using PlayerCSharpAI.api;

namespace PlayerCSharpAI.AI
{
	/// <summary>
	/// The sample C# AI. Start with this project but write your own code as this is a very simplistic implementation of the AI.
	/// </summary>
	public class MyPlayerBrain : IPlayerAI
	{
		// bugbug - put your team name here.
		private const string NAME = "4 Bits";

		// bugbug - put your school name here. Must be 11 letters or less (ie use MIT, not Massachussets Institute of Technology).
		public const string SCHOOL = "UAlberta";

		/// <summary>
		/// The name of the player.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The game map.
		/// </summary>
		public Map GameMap { get; private set; }

		/// <summary>
		/// All of the players, including myself.
		/// </summary>
		public List<Player> Players { get; private set; }

		/// <summary>
		/// All of the companies.
		/// </summary>
		public List<Company> Companies { get; private set; }

		/// <summary>
		/// All of the passengers.
		/// </summary>
		public List<Passenger> Passengers { get; private set; }

		/// <summary>
		/// Me (my player object).
		/// </summary>
		public Player Me { get; private set; }

		private PlayerAIBase.PlayerOrdersEvent sendOrders;

		private static readonly Random rand = new Random();
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MyPlayerBrain));

        private QLearner Learner;

		public MyPlayerBrain(string name)
		{
			Name = !string.IsNullOrEmpty(name) ? name : NAME;
            Learner = new QLearner();
		}

		/// <summary>
		/// The avatar of the player. Must be 32 x 32.
		/// </summary>
		public byte[] Avatar
		{
			get {
				// bugbug replace the file MyAvatar.png in the solution and this will load it. If you add it with a different
				// filename, make sure the Build Action for the file is Embedded Resource.
				using (Stream avatar = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayerCSharpAI.AI.MyAvatar.png"))
				{
					if (avatar == null)
						return null;
					byte[] data = new byte[avatar.Length];
					avatar.Read(data, 0, data.Length);
					return data;
				}
			}
		}

		/// <summary>
		/// Called at the start of the game.
		/// </summary>
		/// <param name="map">The game map.</param>
		/// <param name="me">You. This is also in the players list.</param>
		/// <param name="players">All players (including you).</param>
		/// <param name="companies">The companies on the map.</param>
		/// <param name="passengers">The passengers that need a lift.</param>
		/// <param name="ordersEvent">Method to call to send orders to the server.</param>
		public void Setup(Map map, Player me, List<Player> players, List<Company> companies, List<Passenger> passengers,
				   PlayerAIBase.PlayerOrdersEvent ordersEvent)
		{

			try
			{
				GameMap = map;
				Players = players;
				Me = me;
				Companies = companies;
				Passengers = passengers;
				sendOrders = ordersEvent;

				List<Passenger> pickup = AllPickups(me, passengers);

                Learner.Initialize(passengers, players, companies);

				// get the path from where we are to the dest.
				List<Point> path = CalculatePathPlus1(me, pickup[0].Lobby.BusStop);
				sendOrders("ready", path, pickup);
			}
			catch (Exception ex)
			{
				log.Fatal(string.Format("Setup({0}, ...", me == null ? "{null}" : me.Name), ex);
			}
		}

		/// <summary>
		/// Called to send an update message to this A.I. We do NOT have to send orders in response. 
		/// </summary>
		/// <param name="status">The status message.</param>
		/// <param name="plyrStatus">The player this status is about. THIS MAY NOT BE YOU.</param>
		/// <param name="players">The status of all players.</param>
		/// <param name="passengers">The status of all passengers.</param>
		public void GameStatus(PlayerAIBase.STATUS status, Player plyrStatus, List<Player> players, List<Passenger> passengers)
		{

			// bugbug - Framework.cs updates the object's in this object's Players, Passengers, and Companies lists. This works fine as long
			// as this app is single threaded. However, if you create worker thread(s) or respond to multiple status messages simultaneously
			// then you need to split these out and synchronize access to the saved list objects.

			try
			{
				// bugbug - we return if not us because the below code is only for when we need a new path or our limo hit a bus stop.
				// if you want to act on other players arriving at bus stops, you need to remove this. But make sure you use Me, not
				// plyrStatus for the Player you are updatiing (particularly to determine what tile to start your path from).
				if (plyrStatus != Me)
					return;

				Point ptDest;
				List<Passenger> pickup = new List<Passenger>();
				switch (status)
				{
					case PlayerAIBase.STATUS.UPDATE:
						return;
					case PlayerAIBase.STATUS.NO_PATH:
					case PlayerAIBase.STATUS.PASSENGER_NO_ACTION:
						if (plyrStatus.Limo.Passenger == null)
						{
							pickup = AllPickups(plyrStatus, passengers);
							ptDest = pickup[0].Lobby.BusStop;
						}
						else
							ptDest = plyrStatus.Limo.Passenger.Destination.BusStop;
						break;
					case PlayerAIBase.STATUS.PASSENGER_DELIVERED:
					case PlayerAIBase.STATUS.PASSENGER_ABANDONED:
						pickup = AllPickups(plyrStatus, passengers);
						ptDest = pickup[0].Lobby.BusStop;
						break;
					case PlayerAIBase.STATUS.PASSENGER_REFUSED:
						ptDest = Companies.Where(cpy => cpy != plyrStatus.Limo.Passenger.Destination).OrderBy(cpy => rand.Next()).First().BusStop;
						break;
					case PlayerAIBase.STATUS.PASSENGER_DELIVERED_AND_PICKED_UP:
					case PlayerAIBase.STATUS.PASSENGER_PICKED_UP:
						pickup = AllPickups(plyrStatus, passengers);
						ptDest = plyrStatus.Limo.Passenger.Destination.BusStop;
						break;
					default:
						throw new ApplicationException("unknown status");
				}

				// get the path from where we are to the dest.
				List<Point> path = CalculatePathPlus1(plyrStatus, ptDest);

				if (log.IsDebugEnabled)
					log.Debug(string.Format("{0}; Path:{1}-{2}, {3} steps; Pickup:{4}, {5} total",
						status,
						path.Count > 0 ? path[0].ToString() : "{n/a}",
						path.Count > 0 ? path[path.Count - 1].ToString() : "{n/a}",
						path.Count,
						pickup.Count == 0 ? "{none}" : pickup[0].Name,
						pickup.Count));

				// update our saved Player to match new settings
				if (path.Count > 0)
				{
					Me.Limo.Path.Clear();
					Me.Limo.Path.AddRange(path);
				}
				if (pickup.Count > 0)
				{
					Me.PickUp.Clear();
					Me.PickUp.AddRange(pickup);
				}

				sendOrders("move", path, pickup);
			}
			catch (Exception ex)
			{
				log.Error(string.Format("GameStatus({0}, {1}, ...", status, plyrStatus == null ? "{null}" : plyrStatus.Name), ex);
			}
		}

		private List<Point> CalculatePathPlus1(Player me, Point ptDest)
		{
			List<Point> path = SimpleAStar.CalculatePath(GameMap, me.Limo.TilePosition, ptDest);
			// add in leaving the bus stop so it has orders while we get the message saying it got there and are deciding what to do next.
			if (path.Count > 1)
				path.Add(path[path.Count - 2]);
			return path;
		}

		private static List<Passenger> AllPickups(Player me, IEnumerable<Passenger> passengers)
		{
			List<Passenger> pickup = new List<Passenger>();
			pickup.AddRange(passengers.Where(
					psngr =>
					(!me.PassengersDelivered.Contains(psngr)) && (psngr != me.Limo.Passenger) && (psngr.Car == null) &&
					(psngr.Lobby != null) && (psngr.Destination != null)).OrderBy(psngr => rand.Next()));
			return pickup;
		}
	}
}
