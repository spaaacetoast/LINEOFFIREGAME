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
using System;
using TNet;
using AngryRain.Multiplayer;

/// <summary>
/// Stores physics and positional data for networking and entity interpolation.
/// </summary>
public class State : System.Object
{
	public float timestamp {get; set;}
	public Vector3 pos {get; set;}
	public Vector3 rot {get; set;}
	public Vector3 velocity {get; set;}
	public Vector3 angularVelocity {get; set;}
	
	public bool Compare(State I)
	{
		if (pos != I.pos) {return false;}
		if (rot != I.rot) {return false;}
		if (velocity != I.velocity) {return false;}
		if (angularVelocity != I.angularVelocity) {return false;}
		
		return true;
	}
}

/// <summary>
/// Data structure for flow type
/// </summary>
public enum FlowType : byte
{
	ServerToClient,
	ClientToServer
}

/// <summary>
/// Networked Entity Interpolation Class.
/// </summary>
public class SyncNetworkObject : TNBehaviour {
	
	// Set the flow type to determine whether the client should recieve or send data to the server
	// NOTE: Changing the role during runtime does reset the buffer on both client and server!
	[SerializeField]
	private FlowType flow = FlowType.ServerToClient;

    private FlowType Flow
	{
		get {return flow;}
		set 
		{
			flow = value;
			initalized = false;
			Initialize();
		}
			
	}
	
	// Data structure allows for quick-swapping of client/server rolls during runtime without breaking
	// Any conditions or values
	enum Role
	{
		Client,
		Server
	}
	
	Role role
	{
		get
		{
			if (Flow == FlowType.ServerToClient)
			{
				return (TNManager.isHosting) ? Role.Server : Role.Client;
			}
			else
			{
				return (!TNManager.isHosting) ? Role.Server : Role.Client;
			}
		}
	}
	
	//Packets per second
	public int tickrate = 20;
	
	// How much data do we want to store in the client and server buffers
	public int buffersize = 20;
	
	// The window of time to allow for extrapolation (I.e. 100ms by default)
	public float interpolationBackTime = 0.1f;
	
	// Set a limit to how much interpolation can auto-adjust to client lag
	public float interpolationLimit = 0.3f;
	
	// After 500ms extrapolation is too great (possible server D/C or major lag)
	public float extrapolationLimit = 0.5f;
	
	// If the entity has an external method of prediction, then we don't want to do anything here
	// As the prediction script should take care of the entity's movement
	// Example of this is Client-Side player prediction
	// NOTE: This variable is set clientside
	public bool doesPredict = false;
	
	// Whether the script can auto-adjust how much to interpolate by, based on latency
	public bool autoAdjustInterpolationTime = false;
	
	public int timestampCount;
	
	// Client/Server buffer, exposed publicly
	// Can be used for hit-detection, lag compensation or just keeping
	// track of past object states
	// exists both on the server an the client
    public System.Collections.Generic.List<State> cs_Buffer = new System.Collections.Generic.List<State>();
	
	// The next time in seconds the server should send another packet
	private float next;
	
	// To save bandwidth, the state of the object is only synced
	// if something has changed
	private State lastState;
	private State curState;
	
	// To ensure that data isn't just cut off (due to delta changes being sent over continious fixed updates)
	// Sends the threshold amount of packets even if the player hasn't moved
	private byte justSent = 0;
	public byte justSentThreshold = 20;
	
	// Stop any packets being recieved before the script has finished initalizing
	public bool initalized = false;

    //Target Rigidbody
    public Rigidbody targetRigidbody;
	
	void Start()
	{
		Initialize();
	}
	
	void Initialize()
	{
		// Check if user is authoritative, if so, construct the required variables and begin sending packets
        cs_Buffer = new System.Collections.Generic.List<State>();
		if (role == Role.Server)
		{
			lastState = new State();
			curState = new State();
			
			UpdateInterval();
			
			// You can sync a rigidbody or just an average transform
			 if (targetRigidbody && !targetRigidbody.isKinematic)
			{
				lastState.pos = targetRigidbody.position;
				lastState.rot = targetRigidbody.rotation.eulerAngles;
				lastState.velocity = targetRigidbody.velocity;
				lastState.angularVelocity = targetRigidbody.angularVelocity;
			}
			else
			{
				lastState.pos = transform.position;
				lastState.rot = transform.rotation.eulerAngles;
			}
			
			justSent = 0;
			timestampCount = 0;
		}
		initalized = true;
	}
	
	// Allow the server to set who is authoritative for this object
	public void SetRole(FlowType newFlow)
	{
		if (TNManager.isHosting)
		{
			Flow = newFlow;
			tno.Send((int)GLOBAL.RFCs.RigidbodySyncSetAuthoritative, Target.Others, (byte)newFlow);
		}
	}
	
	// Client apply new authoritative settings for this object
	[RFC((int)GLOBAL.RFCs.RigidbodySyncSetAuthoritative)]
	void RecieveRole(byte newFlow)
	{
		if (!TNManager.isHosting)
		{
			Flow = (FlowType)newFlow;
		}
	}
	
	// Set the next time a packet should be sent to the client(s)
	void UpdateInterval () {next = Time.time + (tickrate > 0 ? (1f / tickrate) : 0f);}
	
	void FixedUpdate()
	{
		// Don't run until we're ready to
		if (!initalized) {return;}
		
		// Check to see if another packet is ready to be sent and if it should be sent at all
		if (tickrate > 0 && next < Time.time && role == Role.Server)
		{
			// You can sync a rigidbody or just an average transform
			 if (targetRigidbody && !targetRigidbody.isKinematic)
			{
				curState = new State();
				curState.pos = targetRigidbody.position;
				curState.rot = targetRigidbody.rotation.eulerAngles;
				curState.velocity = targetRigidbody.velocity;
				curState.angularVelocity = targetRigidbody.angularVelocity;
			}
			else
			{
				curState = new State();
                curState.pos = targetRigidbody.transform.position;
                curState.rot = targetRigidbody.transform.rotation.eulerAngles;
			}
			
			// Check to see if the object has actually changed since last time
			if (!curState.Compare(lastState))
			{
				DoSync(true);
			}
			else
			{
				// If it hasn't changed send out a few dummy packets to ensure that interpolation finishes nicely
				if (justSent > 0 && justSent <= justSentThreshold)
				{
					DoSync(false);
				}
				else if (justSent > justSentThreshold)
				{
					justSent = 0;
				}
			}
			
			lastState = curState;
			
			UpdateInterval();
		}
	}
	
	void Update()
	{
		// Don't run until we're ready to
		if (!initalized) {return;}
		
		if (role == Role.Client)
		{	
			if (cs_Buffer.Count == 0) {return;}
            			
			// Sets the client's render time, and how much in the past it should wait to process things
			float interpolationTime = NetworkTime.Instance.time - (autoAdjustInterpolationTime ?
                (Mathf.Min(Mathf.Max((MultiplayerManager.GetPing(tno.ownerID) / 1000f) + interpolationBackTime, 0.1f), interpolationLimit)) : interpolationBackTime);
			
			//Use Interpolation if the packets in the buffer are within acceptable times.
			if (cs_Buffer[0].timestamp > interpolationTime)
			{
                for (int i = 0; i < cs_Buffer.Count; i++)
				{
                    try
                    {
                        if (cs_Buffer[i].timestamp <= interpolationTime || i == timestampCount - 1)
                        {
                            // The newest playback state, should be > interpolation time (ms) old.
                            State rhs = cs_Buffer[Mathf.Max(i - 1, 0)];
                            // The playback state closest to the interpolation time in age.
                            State lhs = cs_Buffer[i];

                            // Determine the time between the two states to see if interpolation is even necessary.
                            float length = rhs.timestamp - lhs.timestamp;
                            float t = 0.0f;

                            if (length > 0.0001)
                            {
                                t = (interpolationTime - lhs.timestamp) / length;
                            }

                            if (targetRigidbody && !targetRigidbody.isKinematic)
                            {
                                targetRigidbody.position = Vector3.Lerp(targetRigidbody.position, Vector3.Lerp(lhs.pos, rhs.pos, t), 0.8f);
                                targetRigidbody.rotation = Quaternion.Slerp(Quaternion.Euler(lhs.rot), Quaternion.Euler(rhs.rot), t);
                                targetRigidbody.velocity = Vector3.Lerp(lhs.velocity, rhs.velocity, t);
                                targetRigidbody.angularVelocity = Vector3.Lerp(lhs.angularVelocity, rhs.angularVelocity, t);
                            }
                            else
                            {
                                targetRigidbody.transform.position = Vector3.Lerp(lhs.pos, rhs.pos, t);
                                targetRigidbody.transform.rotation = Quaternion.Slerp(Quaternion.Euler(lhs.rot), Quaternion.Euler(rhs.rot), t);
                            }
                            return;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(cs_Buffer.Count + ", " + ex); }
				}
			}
			// Use Extrapolation if no more new data has arrived.
			else
			{
				State newest = cs_Buffer[0];
				float extrapolationLength = interpolationTime - newest.timestamp;
				
				if (extrapolationLength < extrapolationLimit && newest != null)
				{
					if (targetRigidbody && !targetRigidbody.isKinematic)
					{
						float axisLength = extrapolationLength * newest.angularVelocity.magnitude * Mathf.Rad2Deg;
						Quaternion angularRotation = Quaternion.AngleAxis(axisLength, newest.angularVelocity);
						
						targetRigidbody.position = newest.pos + newest.velocity * extrapolationLength;
						targetRigidbody.rotation = angularRotation * Quaternion.Euler(newest.rot);
						targetRigidbody.velocity = newest.velocity;
						targetRigidbody.angularVelocity = newest.angularVelocity;
					}
					else
					{
                        targetRigidbody.transform.position = newest.pos;
                        targetRigidbody.transform.rotation = Quaternion.Euler(newest.rot);
					}
				}
			}
		}
	}
	
	void DoSync(bool stateChanged, Player p = null)
	{
		if (!GLOBAL.networkInitialized) {return;}
		
		if (role == Role.Server)
		{
			State state = new State();
			state.timestamp = NetworkTime.Instance.time;
			
			// You can sync a rigidbody or just an average transform
			 if (targetRigidbody && !targetRigidbody.isKinematic)
			{
				state.pos = targetRigidbody.position;
				state.rot = targetRigidbody.rotation.eulerAngles;
				state.velocity = targetRigidbody.velocity;
				state.angularVelocity = targetRigidbody.angularVelocity;
			}
			else
			{
                state.pos = targetRigidbody.transform.position;
                state.rot = targetRigidbody.transform.rotation.eulerAngles;
			}
			
			if (p != null)
			{
				// Send to specified player
				tno.SendQuickly(75, p, state.timestamp, state.pos, state.rot, state.velocity, state.angularVelocity);
			}
			else
			{
				// Send to everyone except the authoritative client (itself)
				tno.SendQuickly(75, Target.Others, state.timestamp, state.pos, state.rot, state.velocity, state.angularVelocity);
			}
			
			if (cs_Buffer.Count > buffersize)
			{
				cs_Buffer.RemoveAt(buffersize);
				//Shift the buffer down if it exceeds its max size.
			}
			cs_Buffer.Insert(0, state);
			
			// Refer to declaration of 'justSent' at the top of the script for its purpose
			justSent = (byte)(stateChanged ? 1 : justSent + 1);
		}
	}
	
	// The authoritive client can call this function externally
	// To force syncing of the entity
	public void ForceSync(Player p = null)
	{
		DoSync(true, p);
	}
	
	// If a new player joins, ensure they get the latest state
	void OnNetworkPlayerJoin(Player p)
	{
		ForceSync(p);
	}
	
	// When new data arrives, buffer it
	[RFC(75)]
	void CLIENTHandleSync(float ts, Vector3 pos, Vector3 rot, Vector3 vel, Vector3 angVel)
	{
        try
        {
            if (initalized && role == Role.Client && !doesPredict)
            {
                // Load the packet values into a new State, ready to be inserted into the buffer
                State state = new State();
                state.timestamp = ts;
                state.pos = pos;
                state.rot = rot;
                state.velocity = vel;
                state.angularVelocity = angVel;

                if (cs_Buffer.Count > 0 && ts > cs_Buffer[0].timestamp || cs_Buffer.Count == 0)
                {
                    cs_Buffer.Insert(0, state); // Update the buffer.

                    // Keep track of the number of elements in the buffer. Even though the buffer never gets cleared
                    // after initalization, this just helps us track which elements to interpolate between.
                    timestampCount = Mathf.Min(timestampCount + 1, buffersize);
                }

                if (cs_Buffer.Count > buffersize)
                {
                    cs_Buffer.RemoveAt(buffersize);
                    //Shift the buffer down if it exceeds its max size.
                }

                /*List<int> errorList = new List<int>();
                for (int i = 0; i < timestampCount - 1; i++)
                {
                    if (cs_Buffer[i].timestamp < cs_Buffer[i + 1].timestamp)
                    {
                        // If the packet has arrived out-of-order, add it to a list as reference, then delete it later.
                        errorList.Add(i);
                    }
                }
                foreach (int i in errorList)
                {
                    //Remove the out-of-sync packets.
                    cs_Buffer.RemoveAt(i);
                    timestampCount--;
                }*/
            }
        }
        catch( Exception ex )
        {
            Debug.LogError(ex);
            enabled = false;
        }
	}

    public State GetState(int index)
    {
        if (cs_Buffer.Count > index)
            return cs_Buffer[index];
        else
            return new State() { pos = targetRigidbody.position, rot = targetRigidbody.rotation.eulerAngles, timestamp = 0, velocity = targetRigidbody.velocity, angularVelocity = targetRigidbody.angularVelocity };
    }
}
