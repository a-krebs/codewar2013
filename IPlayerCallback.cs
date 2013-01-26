	// Created by Windward Studios, Inc. (www.windward.net). No copyright claimed - do anything you want with this code.


using System;

namespace PlayerCSharpAI
{
	public interface IPlayerCallback
	{

		/// <summary>
		/// Adds a message to the status window.
		/// </summary>
		/// <param name="message">The message to add.</param>
		void StatusMessage(string message);

		void IncomingMessage(string message);

		void ConnectionLost(Exception ex);
	}
}
