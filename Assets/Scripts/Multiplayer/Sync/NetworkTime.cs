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
using TNet;
using System;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Network Time singleton class, used for syncing accurate time across the network
/// </summary>
public class NetworkTime : TNBehaviour {
	
	// This class is a singleton instance for its global access
	private static NetworkTime instance;
	public static NetworkTime Instance
	{
		get {return instance;}
	}
	
	// High-Precison Timer
	private Stopwatch timer;
	
	// Properties for accessing the time, exists in both float and double types
	private double m_time;
	public float time
	{
		get {return (float)m_time;}
		set {}
	}
	public double dtime
	{
		get {return m_time;}
		set {}
	}
	
	private double m_offset;
	public double offset
	{
		get {return m_offset;}
		set {}
	}
	
	private double m_approxTimeInaccuracy;
	public double approxTimeInaccuracy
	{
		get {return m_approxTimeInaccuracy;}
		set {}
	}
	
	private bool readjustingTime = false;
	
	// How many times should the client ping the server during synchronization
	// NOTE: Higher the iteration count, more accurate the time will be
	// NOTE: Never set below 5 and generally never above 10 - otherwise synchronization becomes to long
	private uint iterations = 5;
	
	// The array of latencies used for getting an average ping
	private System.Collections.Generic.List<double> latencies;
	
	// How often to check for clock drift (in seconds)
	private float clockDriftInterval = 1800.0f;
	
	// The threshold before the clocks have become too inaccurate and need re-adjusting
	private double clockDriftThreshold = 0.09D;
	
	
	void Awake()
	{	
		instance = this;
		
		Flush();
		StartCoroutine("NetworkTimeUpdate");
		StartCoroutine("MonitorClockDrift");
	}
	
	// Update the current time ticks, we want the time to be updated independently of the framerate
	IEnumerator NetworkTimeUpdate()
	{
		timer = new Stopwatch();
		timer.Start();
		
		while(true)
		{
			// Update network clock by using current tick count + the network time offset
			
			TimeSpan delta = timer.Elapsed;
			m_time = delta.TotalSeconds + m_offset;
			
			yield return null;
		}
	}
	
	// Perodically check to make sure the clocks are still synchronized
	IEnumerator MonitorClockDrift()
	{
		while (true)
		{
			if (GLOBAL.networkInitialized && !readjustingTime && !TNManager.isHosting)
			{
				if (approxTimeInaccuracy >= clockDriftThreshold | approxTimeInaccuracy <= (0 - clockDriftThreshold))
				{
					Resync();
				}
				else
				{
					latencies = new System.Collections.Generic.List<double>();
					m_approxTimeInaccuracy = 0.0f;
					
					tno.SendQuickly((int)GLOBAL.RFCs.NetworkTimeRequest, Target.Host, ToBytes(m_time), TNManager.playerID);
				}
				yield return new WaitForSeconds(clockDriftInterval);
			}
			yield return null;
		}
	}
	
	// When we leave a game or simply want to completely resync time, make sure that
	// Old latencies are flushed
	// NOTE: This should only ever happen on the client, as the server doesn't need this
	void Flush()
	{
		instance = this;
		
		if (!TNManager.isInChannel || !TNManager.isHosting)
		{
			// Ensure that other scripts won't begin sending packets before we're ready
			GLOBAL.networkInitialized = false;
			readjustingTime = true;
			
			latencies = new System.Collections.Generic.List<double>();
			m_offset = 0;
			m_approxTimeInaccuracy = 0.0f;
		}
	}
	
	// Sync the time, this is generally called when you first join a server
	// NOTE: It should never be called during gameplay, as it would cause temporary noticeable glitches
	//       Use the Resync() method instead
	public void Sync()
	{
		if (!TNManager.isHosting)
		{
			Flush();
			tno.SendQuickly((int)GLOBAL.RFCs.NetworkTimeRequest, Target.Host, ToBytes(m_time), TNManager.playerID);
		}
		else
		{
			// If we're the server then we don't need to do anything
			GLOBAL.networkInitialized = true;
		}
	}
	
	// Resync the time with the server. This should be called if the clocks start to drift
	// NOTE: Can be used during gameplay to resync times, however may cause a temporary hiccup in any time-dependent
	//		 code as it resyncs
	public void Resync()
	{
		if (!TNManager.isHosting)
		{
			readjustingTime = true;
			
			latencies = new System.Collections.Generic.List<double>();
			m_approxTimeInaccuracy = 0.0f;
			tno.SendQuickly((int)GLOBAL.RFCs.NetworkTimeRequest, Target.Host, ToBytes(m_time), TNManager.playerID);
		}
	}
	
	// When we join a new server, make sure to sync time with it
	void OnNetworkJoinChannel(bool success, string msg)
	{
		instance = this;
		
		Sync();
	}
	
	// If we leave a server, make sure to flush out our latency list and
	void OnNetworkDisconnect()
	{
		Flush();
	}
	
	// Recieve packet from client, send current time (plus the client time) back to the client
	[RFC((int)GLOBAL.RFCs.NetworkTimeRequest)]
	void SERVERNetworkTimeRequest(byte[] clientTime, int playerID)
	{
		tno.SendQuickly((int)GLOBAL.RFCs.NetworkTimeResponse, TNManager.GetPlayer(playerID), clientTime, ToBytes(m_time));
	}
	
	// Rebuild the latencies list to calculate an acurate average ping
	void CLIENTBuildLatencies(double clientTime, double serverTime)
	{
		if (latencies.Count < iterations - 1)
		{
			// If we don't have enough latencies yet to make an average then request more time requests until we do
			latencies.Add((m_time - clientTime) / 2);
			
			tno.SendQuickly((int)GLOBAL.RFCs.NetworkTimeRequest, Target.Host, ToBytes(m_time), TNManager.playerID);
		}
		else
		{
			// The latencies are added to a list, where they are ordered and then any latency times
			// above or below 1 standard deviation of the median will be discarded
			// This is done to remove any anomalous results that may have been added due to network error or lag
			// The remaining latencies are reorded and then averaged, this is used as the average latency time
			// For more info, go here: http://www.mine-control.com/zack/timesync/timesync.html
			
			latencies.Add((m_time - clientTime) / 2);
			
			latencies.Sort();
			int midpoint = (int)(latencies.Count * 0.5f);
			double median = latencies[midpoint];
			double sigma = StandardDeviation(latencies);
			
			double sigmaMin = median - sigma;
			double sigmaMax = median + sigma;
			
			System.Collections.Generic.List<double> temp = new System.Collections.Generic.List<double>();
			foreach (double v in latencies)
			{
				if (v > sigmaMin & v < sigmaMax)
				{
					temp.Add(v);
				}
			}
			latencies = temp;
			
			latencies.Sort();
		}
	}
	
	// Recieve packet from the server
	[RFC((int)GLOBAL.RFCs.NetworkTimeResponse)]
	void CLIENTNetworkTimeResponse(byte[] cT, byte[] sT)
	{
		// The doubles are stored as bytes during transit
		double clientTime = ToDouble(cT);
		double serverTime = ToDouble(sT);
		
		// Make sure the latencies list has been built
		CLIENTBuildLatencies(clientTime, serverTime);
		
		// If it's not full yet, don't update any clock values
		if (latencies.Count < iterations - 1)
		{
			return;
		}
		
		// If the request was for a time drift check, then just calculate time drift and
		// not worry about updating clock delta
		if (!readjustingTime)
		{
			m_approxTimeInaccuracy = (serverTime - m_time) + latencies.Average();
			return;
		}
		else
		{
			// Once we have found the averaged latency time, we calculate time difference
			// and then apply it to the client's clock adding in latency time as well
			double clockDelta = (serverTime - m_time) + latencies.Average();
			m_offset += clockDelta;
			
			TimeSpan delta = timer.Elapsed;
			m_time = delta.TotalSeconds + m_offset;
			
			// Once this is all done, the network is ready to begin sending data
			// NOTE: Time would be generally synchronized during a loading screen, this variable
			//		 could be used to ensure that the player can't start playing until time synchronization is done
			GLOBAL.networkInitialized = true;
			readjustingTime = false;
		}
	}
	
	// For calculating the standard deviation of the latencies
	public static double StandardDeviation(System.Collections.Generic.List<double> valueList)
	{
		double M = 0.0;
		double S = 0.0;
		int k = 1;
	    
		foreach (double value in valueList) 
	    {
	        double tmpM = M;
	        M += (value - tmpM) / k;
	        S += (value - tmpM) * (value - M);
	        k++;
	    }
	    return Math.Sqrt(S / (k-1));
	}
	
	byte[] ToBytes(double d)
	{
		return BitConverter.GetBytes(d);
	}
	
	double ToDouble(byte[] b)
	{
		return BitConverter.ToDouble(b, 0);
	}
}
