using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PlayerCSharpAI.api
{
    public class Passenger
    {
		private Passenger(XElement elemPassenger, List<Company> companies)
    	{
			Name = elemPassenger.Attribute("name").Value;
			PointsDelivered = Convert.ToInt32(elemPassenger.Attribute("points-delivered").Value);
			XAttribute attr = elemPassenger.Attribute("lobby");
			if (attr != null)
				Lobby = companies.Find(cpy => cpy.Name == attr.Value);
			attr = elemPassenger.Attribute("destination");
			if (attr != null)
				Destination = companies.Find(cpy => cpy.Name == attr.Value);
			Route = new List<Company>();
			foreach (XElement elemRoute in elemPassenger.Elements("route"))
				Route.Add(companies.Find(cpy => cpy.Name == elemRoute.Value));
			Enemies = new List<Passenger>();
		}

    	/// <summary>
        /// The name of this passenger.
        /// </summary>
        public string Name { get; private set; }

		/// <summary>
		/// The number of points a player gets for delivering this passenger.
		/// </summary>
		public int PointsDelivered { get; private set; }

        /// <summary>
        /// The limo the passenger is presently in. null if not in a limo.
        /// </summary>
        public Limo Car { get; set; }

        /// <summary>
        /// The bus stop the passenger is presently waiting in. null if in a limo or has arrived at final destination.
        /// </summary>
        public Company Lobby { get; private set; }

        /// <summary>
        /// The company the passenger wishes to go to. This is valid both at a bus stop and in a car. It is null if
        /// they have been delivered to their final destination.
        /// </summary>
        public Company Destination { get; private set; }

        /// <summary>
        /// The remaining companies the passenger wishes to go to after destination, in order. This does not include
        /// the Destination company.
        /// </summary>
        public IList<Company> Route { get; private set; }

		/// <summary>
		/// If any of these passengers are at a bus stop, this passenger will not exit the car at the bus stop.
		/// If a passenger at the bus stop has this passenger as an enemy, the passenger can still exit the car.
		/// </summary>
		public IList<Passenger> Enemies { get; private set; }

		public static List<Passenger> FromXml(XElement elemPassengers, List<Company> companies)
		{
			List<Passenger> passengers = new List<Passenger>();
			foreach (XElement elemPsngrOn in elemPassengers.Elements("passenger"))
				passengers.Add(new Passenger(elemPsngrOn, companies));

			// need to now assign enemies - needed all Passenger objects created first.
			foreach (XElement elemPsngrOn in elemPassengers.Elements("passenger"))
			{
				Passenger psngrOn = passengers.Find(psngr => psngr.Name == elemPsngrOn.Attribute("name").Value);
				foreach (XElement elemEnemyOn in elemPsngrOn.Elements("enemy"))
					psngrOn.Enemies.Add(passengers.Find(psngr => psngr.Name == elemEnemyOn.Value));
			}

			// set if they're in a lobby
			foreach (Passenger psngrOn in passengers)
			{
				if (psngrOn.Lobby == null)
					continue;
				Company cmpnyOn = companies.Find(cmpny => cmpny == psngrOn.Lobby);
				cmpnyOn.Passengers.Add(psngrOn);
			}

			return passengers;
		}

		public static void UpdateFromXml(List<Passenger> passengers, List<Company> companies, XElement elemPassengers)
		{
			foreach (XElement elemPsngrOn in elemPassengers.Elements("passenger"))
			{
				Passenger psngrOn = passengers.Find(ps => ps.Name == elemPsngrOn.Attribute("name").Value);
				XAttribute attr = elemPsngrOn.Attribute("destination");
				if (attr != null)
				{
					psngrOn.Destination = companies.Find(cmpy => cmpy.Name == attr.Value);
					// remove from the route
					if (psngrOn.Route.Contains(psngrOn.Destination))
						psngrOn.Route.Remove(psngrOn.Destination);
				}

				// set props based on waiting, travelling, done
				switch (elemPsngrOn.Attribute("status").Value)
				{
					case "lobby":
						psngrOn.Lobby = companies.Find(cmpy => cmpy.Name == elemPsngrOn.Attribute("lobby").Value);
						psngrOn.Car = null;
						break;
					case "travelling":
						psngrOn.Lobby = null;
						// psngrOn.Car set in Player update.
						break;
					case "done":
						Trap.trap();
						psngrOn.Destination = null;
						psngrOn.Lobby = null;
						psngrOn.Car = null;
						break;
				}
			}
		}

    	public override string ToString()
    	{
    		return Name;
    	}
     }
}
