// Created by Windward Studios, Inc. (www.windward.net). No copyright claimed - do anything you want with this code.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using PlayerCSharpAI.AI;
using PlayerCSharpAI.api;
using log4net.Config;

namespace PlayerCSharpAI
{
	public class Framework : IPlayerCallback
	{
		private TcpClient tcpClient;
		private readonly MyPlayerBrain brain;
		private readonly string ipAddress = "127.0.0.1";

		private string myGuid;

		// this is used to make sure we don't have multiple threads updating the Player/Passenger lists, sending
		// back multiple orders, etc. This is a lousy way to handle this - but it keeps the example simple and
		// leaves room for easy improvement.
		private int signal;

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Framework)); 

		/// <summary>
		/// Run the A.I. player. All parameters are optional.
		/// </summary>
		/// <param name="args">I.P. address of server, name</param>
		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			if (log.IsInfoEnabled)
				log.Info("***** Windwardopolis starting *****");
			
			Framework framework = new Framework(args);
			framework.Run();
		}

		private Framework(IList<string> args)
		{
			brain = new MyPlayerBrain(args.Count >= 2 ? args[1] : null);
			if (args.Count >= 1)
				ipAddress = args[0];
			string msg = string.Format("Connecting to server {0} for user: {1}", ipAddress, brain.Name);
			if (log.IsInfoEnabled)
				log.Info(msg);
			Console.Out.WriteLine(msg);
		}

		private void Run()
		{
			Console.Out.WriteLine("starting...");

			tcpClient = new TcpClient();
			tcpClient.Start(this, ipAddress);
			ConnectToServer();

			// It's all messages to us now.
			Console.Out.WriteLine("enter \"exit\" to exit program");
			while (true)
			{
				string line = Console.ReadLine();
				if (line == "exit")
					break;
			}
		}

		public void StatusMessage(string message)
		{
			Console.Out.WriteLine(message);
		}

		public void IncomingMessage(string message)
		{
			try
			{
				DateTime startTime = DateTime.Now;
				// get the xml - we assume we always get a valid message from the server.
				XDocument xml = XDocument.Parse(message);

				switch (xml.Root.Name.LocalName)
				{
					case "setup":
						Console.Out.WriteLine("Received setup message");
						if (log.IsInfoEnabled)
							log.Info("Received setup message");

						List<Player> players = Player.FromXml(xml.Root.Element("players"));
						List<Company> companies = Company.FromXml(xml.Root.Element("companies"));
						List<Passenger> passengers = Passenger.FromXml(xml.Root.Element("passengers"), companies);
						Map map = new Map(xml.Root.Element("map"), companies);
						myGuid = xml.Root.Attribute("my-guid").Value;
						Player me2 = players.Find(plyr => plyr.Guid == myGuid);

						brain.Setup(map, me2, players, companies, passengers, PlayerOrdersEvent);
						break;

					case "status":
						// may be here because re-started and got this message before the re-send of setup.
						if (string.IsNullOrEmpty(myGuid))
						{
							Trap.trap();
							return;
						}

						PlayerAIBase.STATUS status = (PlayerAIBase.STATUS)Enum.Parse(typeof(PlayerAIBase.STATUS), xml.Root.Attribute("status").Value);
						XAttribute attr = xml.Root.Attribute("player-guid");
						string guid = attr != null ? attr.Value : myGuid;

						lock (this)
						{
							if (signal > 0)
							{
								// bad news - we're throwing this message away.
								Trap.trap();
								return;
							}
							signal++;
						}

						Player.UpdateFromXml(brain.Players, brain.Passengers, xml.Root.Element("players"));
						Passenger.UpdateFromXml(brain.Passengers, brain.Companies, xml.Root.Element("passengers"));

						// update my path & pick-up.
						Player plyrStatus = brain.Players.Find(plyr => plyr.Guid == guid);
						XElement elem = xml.Root.Element("path");
						if (elem != null)
						{
							string [] path = elem.Value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
							plyrStatus.Limo.Path.Clear();
							foreach (string stepOn in path)
							{
								int pos = stepOn.IndexOf(',');
								plyrStatus.Limo.Path.Add(new Point(Convert.ToInt32(stepOn.Substring(0, pos)), Convert.ToInt32(stepOn.Substring(0, pos))));
							}
						}

						elem = xml.Root.Element("pick-up");
						if (elem != null)
						{
							string [] names = elem.Value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
							plyrStatus.PickUp.Clear();
							foreach (Passenger psngrOn in names.Select(pickupOn => brain.Passengers.Find(ps => ps.Name == pickupOn)))
								plyrStatus.PickUp.Add(psngrOn);
						}

						// pass in to generate new orders
						brain.GameStatus(status, plyrStatus, brain.Players, brain.Passengers);

						lock (this)
						{
							signal--;
						}
						break;

					case "exit":
						Console.Out.WriteLine("Received exit message");
						if (log.IsInfoEnabled)
							log.Info("Received exit message");
						Environment.Exit(0);
						break;

					default:
						Trap.trap();
						string msg = string.Format("ERROR: bad message (XML) from server - root node {0}", xml.Root.Name.LocalName);
						log.Warn(msg);
						Trace.WriteLine(msg);
						break;
				}

				TimeSpan turnTime = DateTime.Now.Subtract(startTime);
				if (turnTime.TotalMilliseconds > 800)
					Console.Out.WriteLine("WARNING - turn took {0} seconds", turnTime.TotalMilliseconds/1000);
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(string.Format("Error on incoming message. Exception: {0}", ex));
				log.Error("Error on incoming message.", ex);
			}
		}

		private void PlayerOrdersEvent(string order, List<Point> path, List<Passenger> pickUp)
		{

			// update our info
			if (path.Count > 0)
			{
				brain.Me.Limo.Path.Clear();
				brain.Me.Limo.Path.AddRange(path);
			}
			if (pickUp.Count > 0)
			{
				brain.Me.PickUp.Clear();
				brain.Me.PickUp.AddRange(pickUp);
			}

			XDocument xml = new XDocument();
			XElement elem = new XElement(order);
			xml.Add(elem);
			if (path.Count > 0)
			{
				StringBuilder buf = new StringBuilder();
				foreach (Point ptOn in path)
					buf.Append(Convert.ToString(ptOn.X) + ',' + Convert.ToString(ptOn.Y) + ';');
				elem.Add(new XElement("path", buf));
			}
			if (pickUp.Count > 0)
			{
				StringBuilder buf = new StringBuilder();
				foreach (Passenger psngrOn in pickUp)
					buf.Append(psngrOn.Name + ';');
				elem.Add(new XElement("pick-up", buf));
			}
			tcpClient.SendMessage(xml.ToString());
		}

		public void ConnectionLost(Exception ex)
		{

			Console.Out.WriteLine("Lost our connection! Exception: " + ex.Message);
			log.Warn("Lost our connection!", ex);

			int delay = 500;
			while (true)
				try
				{
					if (tcpClient != null)
						tcpClient.Close();
					tcpClient = new TcpClient();
					tcpClient.Start(this, ipAddress);

					ConnectToServer();
					Console.Out.WriteLine("Re-connected");
					log.Warn("Re-connected");
					return;
				}
				catch (Exception e)
				{
					log.Warn("Re-connection fails!", e);
					Console.Out.WriteLine("Re-connection fails! Exception: " + e.Message);
					Thread.Sleep(delay);
					delay += 500;
				}
		}

		private void ConnectToServer()
		{
			XDocument doc = new XDocument();
			XElement root = new XElement("join", new XAttribute("name", brain.Name), new XAttribute("school", MyPlayerBrain.SCHOOL), new XAttribute("language", "C#"));
			byte[] data = brain.Avatar;
			if (data != null)
				root.Add(new XElement("avatar", Convert.ToBase64String(data)));
			doc.Add(root);
			tcpClient.SendMessage(doc.ToString());
		}
	}
}
