using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayerCSharpAI.api;
using System.Drawing;


namespace PlayerCSharpAI.AI
{
  
    class GameStatusInfo
    {
        public List<Player> playersList;
        public List<Passenger> passengerList;
        public List<Limo> limosList;
        PathFinder pFinder;
        Map map;

        public void Setup(Map m, PathFinder pathing)
        {
            pFinder = pathing;
            map = m;
        }

        public void update(List<Player> players, List<Passenger> passenger)
        {
            playersList = players;
            passengerList = passenger;

            foreach (Player player in playersList)
            {
                limosList.Add(player.Limo);

            }
        }

        /*
         * returns list of available passengers. removes passenger that are done there route
         */
        public List<Passenger> availablePassengers()
        {
            List<Passenger> Result = passengerList;
            Result.RemoveAll(DestinationNull);
            return Result;
        }

        /*
         * return list of all places with passengers at them
         */
        public List<Company> activeBusStops()
        {
            List<Company> Result = new List<Company>();

            foreach (Passenger p in availablePassengers())
            {
                if (!Result.Contains(p.Lobby))
                    Result.Add(p.Lobby);
            }
            return Result;
        }

        /*
         * checks if player is finished route
         */
        private static bool DestinationNull(Passenger passenger)
        {
            return passenger.Destination == null;
        }

        /*
         * return ordered list of passengers at locations
         */ 
        public List<Passenger> PassengerAtLocation(Company Location)
        {
            List<Passenger> newPassengerList = new List<Passenger>(Location.Passengers);
            return newPassengerList.OrderByDescending(x => x.PointsDelivered).ToList();
        }

        /*
         * return Player list Ordered by highest to lowest Score 
         */ 
        public List<Player> PlayerHighScore(List<Player> player)
        {
            return player.OrderByDescending(x => x.Score).ToList();

        }

        /*
         * Takes the desired time and the target location. Returns all limos that
         * can make it to that location in the desired time.
         */
        public List<Limo> OtherPlayersWithinTimeToLocation(float time, Point target ) {
            List<Limo> Result = new List<Limo>();

            

            foreach (Limo limo in limosList) {
                // get the path and time needed for limo to get to location
                List<Point> optimal_path = pFinder.computeFastestPath(map,limo.TilePosition, target);
               // float time_Limo = pFinder.GetTime(optimal_path);
                float time_Limo = 0;
                // if path switched, then have to add turning time
                int direction = 0;
                if (optimal_path[0] != limo.Path[0])
                    // the turn time was calculated by James. It should work.
                    time_Limo += 4 * 5;
                    
                // if time is less than the input time, add it to the Result List
                Result.Add(limo);

            }
                 return Result;
        }

      
    }
    
}
