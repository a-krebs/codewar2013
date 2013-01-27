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
        GameStatusInfo GameInfo;
        

		// bugbug - put your school name here. Must be 11 letters or less (ie use MIT, not Massachussets Institute of Technology).
		public const string SCHOOL = "UAlberta";

        private PathFinder pFinder;

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

		public MyPlayerBrain(string name)
		{
			Name = !string.IsNullOrEmpty(name) ? name : NAME;
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
                pFinder = new PathFinder();
				GameMap = map;
                GameInfo = new GameStatusInfo();
                GameInfo.Setup(map, pFinder);
				Players = players;
				Me = me;
				Companies = companies;
				Passengers = passengers;
				sendOrders = ordersEvent;

				List<Passenger> pickup = AllPickups(me, passengers);
                pFinder.computeAllPaths(map);
                pFinder.generateAdjacencyMatrix();

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

                GameInfo.update(players, passengers);

				Point ptDest;
				List<Passenger> pickup = new List<Passenger>();

   				// determine where we want to go and who we want to pick up
                switch (status)
                {
                    case PlayerAIBase.STATUS.UPDATE:
                        return;
                    case PlayerAIBase.STATUS.NO_PATH:
                    case PlayerAIBase.STATUS.PASSENGER_NO_ACTION:
                        if (plyrStatus.Limo.Passenger == null)
                        {
                            pickup = MakeDecision(status, plyrStatus, players, passengers);
                            ptDest = pickup[0].Lobby.BusStop;
                        }
                        else
                            ptDest = plyrStatus.Limo.Passenger.Destination.BusStop;
                        break;
                    case PlayerAIBase.STATUS.PASSENGER_DELIVERED:
                    case PlayerAIBase.STATUS.PASSENGER_ABANDONED:
                        pickup = MakeDecision(status, plyrStatus, players, passengers);
                        ptDest = pickup[0].Lobby.BusStop;
                        break;
                    case PlayerAIBase.STATUS.PASSENGER_REFUSED:
                        pickup = MakeDecision(status, plyrStatus, players, passengers);
                        ptDest = pickup[0].Lobby.BusStop;
                        break;
                    case PlayerAIBase.STATUS.PASSENGER_DELIVERED_AND_PICKED_UP:
                    case PlayerAIBase.STATUS.PASSENGER_PICKED_UP:
                        pickup = MakeDecision(status, plyrStatus, players, passengers);
                        ptDest = pickup[0].Lobby.BusStop;
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

        /// <summary>
        /// Returns the time it will take for a player to get to destination point
        /// </summary>
        /// <param name="me">Player object</param>
        /// <param name="ptDest">Destination Point</param>
        /// <returns></returns>
        private int CalculateDestTime(Player me, Point ptDest)
        {
            return -1;
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

        private List<Passenger> MakeDecision(PlayerAIBase.STATUS status, Player plyrStatus, List<Player> players, List<Passenger> passengers)
        {
            if (Me.Limo.Passenger != null)
                return Me.PickUp;
            switch (status)
            {
                // action should always be :
                // make sure action is still the desired one
                // if not, compare the scores of top actions
                // if there's not a big difference, stay on current action
                case PlayerAIBase.STATUS.UPDATE:
                // default action
                case PlayerAIBase.STATUS.NO_PATH:
                // that's fine, default action
                case PlayerAIBase.STATUS.PASSENGER_NO_ACTION:
                // if we have no passenger, decide who we want.
                // if we have a passenger, make sure we still want them.
                // that's the default action
                case PlayerAIBase.STATUS.PASSENGER_DELIVERED:
                // default action
                case PlayerAIBase.STATUS.PASSENGER_ABANDONED:
                // default action
                case PlayerAIBase.STATUS.PASSENGER_REFUSED:
                // default action
                case PlayerAIBase.STATUS.PASSENGER_DELIVERED_AND_PICKED_UP:
                //default action
                case PlayerAIBase.STATUS.PASSENGER_PICKED_UP:
                // default action
                default:
                    {

                        // Go to the best-expected company.
                        SortedDictionary<float, Company> bestBusStop = new SortedDictionary<float, Company>();
                        foreach (Company company in GameInfo.activeBusStops())
                        {
                            // Sort passengers at this stop.
                            SortedDictionary<float, Passenger> passengersAtStop = GetPassengerWeights(Me, GameInfo.PassengerAtLocation(company));
                            
                            // TODO: Ensure descending order.
                            // Time for us to get there
                            float our_time = (float)pFinder.getTimeForPath(pFinder.computeFastestPath(Me.Limo.TilePosition, company.BusStop));
                            // Number of other players closer/same than us. Buffer time of 100sec.
                            int other_players = GameInfo.OtherPlayersWithinTimeToLocation(our_time+100f, company.BusStop).Count;

                            // If there's one enemy within range, the 2nd highest has to be good enough. If 3rd, they have to be high enough. Etc.
                            int minCount = Math.Min(other_players, passengersAtStop.Count);
                            float average = 0;
                            // have to go in descending order for this.
                            for (int i = passengersAtStop.Count-1; i >= passengersAtStop.Count-minCount; i--) {
                                average += passengersAtStop.Keys.ToList()[i];
                            }
                           /* for (int i = 0; i < minCount; i++)
                            {
                                average += passengersAtStop.Keys.ToList()[i];
                            }*/
                            average /= minCount;

                            // Add the average value & company to result
                            bestBusStop.Add(average, company);
                        }
                        // go to the best bus stop. Set the desired player_list to the top at the bus stop
                        int numberOfResults = bestBusStop.Count;
                        Company winningCompany = bestBusStop.Values.ToList()[numberOfResults-1];

                        //List<Point> path = pFinder.computeFastestPath(Me.Limo.TilePosition, winningCompany.BusStop);
                        List<Passenger> passengerList = GetPassengerWeights(Me, GameInfo.PassengerAtLocation(winningCompany)).Values.ToList();
                        passengerList.Reverse();

                        return passengerList;

                    }
                    
            }
        }

        /// <summary>
        /// A dictionary containing values of all passengers that are not in limos
        /// </summary>
        /// <param name="passengers"></param>
        /// <returns></returns>
        private SortedDictionary<float, Passenger> GetPassengerWeights(Player player, List<Passenger> passengers)
        {
            SortedDictionary<float, Passenger> weights = new SortedDictionary<float, Passenger>();
            for (int i = 0; i < passengers.Count; i++)
            {
                if (passengers[i].Lobby != null) {
                    weights.Add((float)SinglePassengerWeight(player, passengers[i]), passengers[i]);
                }
            }
            return weights;
        }

        /// <summary>
        /// alpha * (score + 0.5*distance_to_dest)) - beta * (time_to_location + pickup_time + time_to_dest)
        /// </summary>
        /// <param name="passenger"></param>
        /// <returns></returns>
        private double SinglePassengerWeight(Player player, Passenger passenger)
        {
            // get the wiehgts from the configuration file
            double ALPHA = 7.0;
            double BETA = 1.0;
            double DIST_CONTSTANT = 0.5;
            // time to perform pickup of player
            int PICKUP_TIME = 130;

            if (passenger.Lobby != null ) {
                return ALPHA * (passenger.PointsDelivered + DIST_CONTSTANT
                    * pFinder.DistanceSrcDest(passenger.Lobby.BusStop, passenger.Destination.BusStop))
                    - BETA * (pFinder.getTimeForPath(pFinder.computeFastestPath(player.Limo.TilePosition, passenger.Lobby.BusStop)) + PICKUP_TIME + pFinder.getTimeForPath(pFinder.computeFastestPath(passenger.Lobby.BusStop, passenger.Route[0].BusStop))
                );
            } else {
                return -1.0F;
            }
        }

        private Point GetDestination(Passenger passenger)
        {
 	        throw new NotImplementedException();
        }

	}
}
