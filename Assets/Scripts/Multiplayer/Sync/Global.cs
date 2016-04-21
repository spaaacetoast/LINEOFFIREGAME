/*
 * Author: Manmax75
 * Copyright 2013
 * More info: http://www.tasharen.com/forum/index.php?topic=5788.0
 * Feel free to upgrade this and do so as you wish,
 * but just leave this little attribution up here.
 * Enjoy.
*/

using UnityEngine;
using System.Collections;

public static class GLOBAL {
	
	public enum RFCs
	{
		RigidbodySync = 75,
		PlayerPrediction = 76,
		CreateEntity = 77,
		RigidbodySyncSetAuthoritative = 78,
		PickedUpObject = 79,
		ObjectMovePrediction = 80,
		NetworkTimeRequest = 81,
		NetworkTimeResponse = 82
	}
	
	private static bool m_NetworkInitalized = false;
	public static bool networkInitialized
	{
		get {return m_NetworkInitalized;}
		set {m_NetworkInitalized = value;}
	}
	
	
	public static void HandleException(string message)
	{
		//TODO:Console shit
		Debug.LogError(message);
	}
}
