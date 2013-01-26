using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;

namespace PlayerCSharpAI.api
{
    public class Company
    {
    	private Company(XElement elemCompany)
    	{
    		Name = elemCompany.Attribute("name").Value;
			BusStop = new Point(Convert.ToInt32(elemCompany.Attribute("bus-stop-x").Value), Convert.ToInt32(elemCompany.Attribute("bus-stop-y").Value));
			Passengers = new List<Passenger>();
    	}

    	/// <summary>
		/// The name of the company.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The tile with the company's bus stop.
		/// </summary>
		public Point BusStop { get; private set; }

		/// <summary>
		/// The name of the passengers waiting at this company's bus stop for a ride.
		/// </summary>
		public IList<Passenger> Passengers { get; private set; }

    	public static List<Company> FromXml(XElement elemCompanies)
    	{
			List<Company> companies = new List<Company>();
			foreach (XElement elemCmpyOn in elemCompanies.Elements("company"))
				companies.Add(new Company(elemCmpyOn));
			return companies;
		}

    	public override string ToString()
    	{
    		return string.Format("{0}; {1}", Name, BusStop);
    	}
    }
}
