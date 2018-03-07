using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO.Ports;

public class Interlace : MonoBehaviour {
	
	public static Interlace comManager;
	
	//This script will handle the serial connection to and from the Arduino, serial state control, and other PC <-> Arduino commands.
	
	public string comPort = "/dev/ttyUSB0";
	public int baudRate = 9600;
	public bool isConnected = false;
	public SerialPort stream;
	public string lastValue;
	public bool breakConnection = false;
	public bool boxReadOnline;
	public string currentDB = "FMB01";
	public float encResolution = 0.25f; //Default 1/4in resolution (1/2in on the ENC or 24ppr on photocraft)
	public float warnGap = 9.0f;
	public bool usingAudio = false;
	public AudioClip bellC;
	public AudioClip crashC;
	public AudioClip enableC;
	
	private string serialValue;
	private string lastSentValue;
	private volatile bool listening;
	private Thread readThread;
	private bool exitOnLoop=false;
	private bool boxDataReady = false; //used in ui manager to detect if box read is still reading
	private AudioSource audioSFX;
	private bool lastAudioState;
	
	void Start() {
		comManager = this;
		OpenCom();
		audioSFX = GetComponent<AudioSource>();
		lastAudioState = usingAudio;
	}

	public void OpenCom() { //in an effort to make this script static accessible, am moving start function to here where I can control opening and closing serial
		Debug.Log("Opening!");
		try {
			stream = new SerialPort(comPort, baudRate); //must close and reopen com to update com or baud - MUST WRITE UPDATE IF STREAM COM OR BAUD CHANGED!
			stream.Open();
			stream.ReadTimeout = 500; //currently don't plan to add this to settings, too risky
		} catch (Exception e) {
			Debug.Log("Error on Connect: " + e);
			stream = null;
			isConnected = false;
		}
		if (stream!=null) {
			isConnected = true;
			Debug.Log("Connection Established! Port: " +comPort);
		}
	}
	
	public void CloseCom() { //in an effort to make this script static accessible, am moving start function to here where I can control opening and closing serial
		Debug.Log("Closing!");
		try {
			if (stream!=null) {
				while (this.listening) {
					breakConnection = true;
				}
				stream.Close();
				boxReadOnline = false;
				isConnected = false;
				stream=null;
			}
		} catch (Exception e) { 
			Debug.Log("Error on Disconnect: " + e);
		}
	}
	
	public bool pingARD() { //pings the arduino
		try {
			if (stream!= null && !this.listening) {
				String s = "PING;";
				stream.Write(s);
				Debug.Log("PING Sent");
				StartListener(true);
				return true;
			} else {
				Debug.Log("Failed to ping, already reading?");
				return false;
			}
		} catch (Exception e) {
			Debug.Log("Error on Ping: " + e);
			return false;
		}
	}
	
	public void forceDelay() {
		float delay = 0.5f;
		while (delay > 0) {
			delay -= Time.deltaTime;
			Debug.Log(delay);
		}
	}
	
	public bool beginBoxReadARD() { //this function starts the box read by telling the ard to start measuring and opening a thread for listening
		try {
			if ((stream!= null) && !this.listening) {
				String cval = lastValue;
				String s = "pushBoxT;";
				stream.Write(s);
				StartListener(false);
				boxReadOnline = true;
				//forceDelay();
				while (cval == lastValue) {
					
				}
				Debug.Log(lastValue);
				Debug.Log("BoxRead Lane 1 Sent");
				return true;
			} else {
				Debug.Log("Failed to start box read, are we already listening?");
				boxReadOnline = false;
				return false;
			}
		} catch (Exception e) {
			Debug.Log("Error on BoxRead: " + e);
			boxReadOnline = false;
			return false;
		}
	}
	
	public bool beginBoxReadARDL2() { //this function starts the box read for lane 2 only, by telling the ard to start measuring and opening a thread for listening
		try {
			if ((stream!= null) && !this.listening) {
				String cval = lastValue;
				String s = "pushBoxT2;";
				stream.Write(s);
				StartListener(false);
				boxReadOnline = true;
				//forceDelay();
				while (cval == lastValue) {
					
				}
				Debug.Log("BoxRead Lane 2 Sent");
				Debug.Log(lastValue);
				return true;
			} else {
				Debug.Log("Failed to start box read, are we already listening?");
				boxReadOnline = false;
				return false;
			}
		} catch (Exception e) {
			Debug.Log("Error on BoxRead: " + e);
			boxReadOnline = false;
			return false;
		}
	}
	
	public bool beginBoxReadARDDual() { //this function starts the box read for both lanes by telling the ard to start measuring and opening a thread for listening
		try {
			if ((stream!= null) && !this.listening) {
				String cval = lastValue;
				String s = "pushBoxTD;";
				stream.Write(s);
				StartListener(false);
				boxReadOnline = true;
				//forceDelay();
				while (cval == lastValue) {
					
				}
				Debug.Log("BoxRead Both Lanes Sent");
				Debug.Log(lastValue);
				return true;
			} else {
				Debug.Log("Failed to start box read, are we already listening?");
				boxReadOnline = false;
				return false;
			}
		} catch (Exception e) {
			Debug.Log("Error on BoxRead: " + e);
			boxReadOnline = false;
			return false;
		}
	}
	
	public void pushToDB(string sval) { //this function pushes new box data to the database using various functions to parse and then send the data | Also converts pulses to in based on resolution
		if (boxReadOnline) {
			if ((sval!=lastSentValue) && (sval.Length>10)) {
				var data = DatabaseManager.dbManager.parseBoxData(sval);
				float finalGap = float.Parse(data.Item2)*encResolution;//UNSWAPPED, issue was due to NO/NC PE setting in arduino
				float finalLength = float.Parse(data.Item3)*encResolution;
				string finalLane = data.Item1;
				
				if (usingAudio && (finalGap < warnGap)) { //checks for gap error and plays noise if it is too small
					audioSFX.PlayOneShot(crashC,1.0F);
				}
				if (usingAudio && (finalGap >= warnGap)) {
					audioSFX.PlayOneShot(bellC,1.0F);
				}
				string myURL = "http://localhost/dbHandle.php";
				string response = DatabaseManager.dbManager.sendData(myURL,"insertData",currentDB,finalGap.ToString(), finalLength.ToString(), finalLane);
				Debug.Log(response + ", FG: "+finalGap+", FL: "+finalLength);
				lastSentValue = sval;
			}
		}
	}
	
	public bool endBoxReadARD() { //This will wait and make sure the thread closes by interrupting the loop, once broke we send the stop command and listen 1x for the confirmation.
		try {
			if (stream!= null) {
				while (this.listening) {
					breakConnection = true;
				}
				breakConnection = false;
				String cval = lastValue;
				String s = "pushBoxF;";
				stream.Write(s);
				StartListener(true);
				boxReadOnline = false;
				//forceDelay();
				while (cval == lastValue) {
					
				}
				Debug.Log(lastValue);
				Debug.Log("BoxEnd Sent");
				return true;
			} else {
				Debug.Log("Failed to end box test because port not open!");
				boxReadOnline = false;
				return false;
			}
		} catch (Exception e) {
			Debug.Log("Error on BoxRead: " + e);
			boxReadOnline = false;
			return false;
		}
	}
	
	public void StartListener(bool exitOnRead) { //puts socket in thread to listen so that main thread doesn't freeze
        if (!this.listening) {
            readThread = new Thread(ConnectListen);//create a new thread
			readThread.IsBackground = true; //assign it to background
            this.listening = true;//mark it as listening
            readThread.Start();//launch the thread
			exitOnLoop=exitOnRead;
        }
    }
	
	void ConnectListen() { //This modified loop listener allows for interrupt due to timeout delay
		if (stream != null) {
			while (!breakConnection) {
				try {
					serialValue = stream.ReadLine();
					Debug.Log("From ARD: "+serialValue);
					//pushToDB(serialValue);
					boxDataReady = true;
					lastValue = serialValue;
					if (exitOnLoop) { //if only 1x read, return here after a good read
						this.listening = false;
						return;
					}	
				} catch (TimeoutException) { //catch our timeout but don't exit, keep looping
					//do nothing
				} catch (Exception e) {
					Debug.Log("Error on read: " + e);
					stream.Close();
					isConnected = false;
					stream=null;
					this.listening = false;
					return;
				}
			} 
			this.listening = false;
			return;
		}
		this.listening = false;
		return;
	}
	
	void Update() {
		if (boxDataReady) {
			pushToDB(serialValue);
			boxDataReady = false;
		}
		if (usingAudio != lastAudioState) {
			if (usingAudio) {
				audioSFX.PlayOneShot(enableC,1.0F);
			}
			lastAudioState = usingAudio;
		}
		
	}
	
	void OnApplicationQuit() { //make sure we close everything on exit!
		try {
		if (stream!=null) {
			stream.Close();
			isConnected = false;
			stream=null;
		} else {
			//Debug.Log("Thread never open.");
		}
		} catch (Exception e) {
			Debug.Log("Error closing socket: " +e);
		}
	}	
} //EOF