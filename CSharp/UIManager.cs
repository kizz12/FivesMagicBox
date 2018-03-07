using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;

public class UIManager : MonoBehaviour {

	//This "little" fella will manage all of our button functions and interactions across the system and call functions from across the system

	//~~~~~~Start UI Elements ARD/SYS ~~~~~
	public Button connectARD;
	public Button pingARD;
	public Button connSettings;
	public Button FHMISettings;
	public Button beginBoxRead;
	public Text beginBoxReadTxt;
	public Text connectARDTxt;
	public Text connStatus;
	public Text serialStatus;
	public Text lastRawSerial;
	public Text COMChannel;
	public Text BAUDRate;
	public GameObject ConnectionPanel;
	public GameObject FHMIPanel;
	public Dropdown COMDD;
	public Button COMGO;
	public InputField BAUDIF;
	public Button BAUDGO;
	public Button exitBtn;
	public GameObject startupPanel;
	public GameObject exportingPanel;
	public GameObject exportMenu;
	public GameObject settingsMenu;
	public InputField GapNoteIF;
	public Button GapNoteSet;
	
	//~~~~~~Start UI Elements BRS ~~~~~
	public Text lastDBMsg;
	public Button pingPHP;
	public Button startServices;
	public Text mySQLStatus;
	public Text apacheStatus;
	public Text currentDB;
	public Text currentDB1;
	public Text currentRes;
	public Text currentRes1;
	public InputField dbName;
	public InputField resName;
	public Button setDBName;
	public Button setRes;
	public Text resWarning;
	public Text resWarning1;
	public Button setSpeed;
	public InputField getSpeed;
	public Text speedTxt;
	public Text speedTxt1;
	public Button exportDBBtn;
	public Toggle usbToggle;
	public Toggle noUSBToggle;
	public Text usbPathTest;
	public Text currentDB2;
	public Text usbTgText;
	public Text currentSelectedLane;
	public Button confirmDelData;
	public Text confDelDataText;
	public GameObject confirmDel;
	public Toggle inStream1;
	public Toggle inStream2;
	public Toggle inStreamDual;
	public string phpCheck;
	public Button audioControl;
	public Text audioControlTxt;
	public Text DBCharWarn;
	
	//~~~~~Privs~~~~~~~
	private float uiUpdateRate=1.0f; //controls refresh rate of Status screen
	private bool usbOn=false;
	private bool lastUSBOn = false;
	private bool lastBoxReadState;
	private int currentCOM;
	private int lastCOM;
	private int currentBAUD;
	private int lastBAUD;
	private ColorBlock boxReadColor;
	private ColorBlock audioButtonColor;
	private int currentSpeed = 600;
	
	void Start () {
		//listeners for buttons
		connectARD.onClick.AddListener(ConnectArd);
		pingARD.onClick.AddListener(PingArd);
		beginBoxRead.onClick.AddListener(StartBoxMeasure);
		connSettings.onClick.AddListener(ConnSetting);
		FHMISettings.onClick.AddListener(FHMIDisplay);
		COMGO.onClick.AddListener(COMChange);
		BAUDGO.onClick.AddListener(BAUDChange);
		exitBtn.onClick.AddListener(onExit);
		pingPHP.onClick.AddListener(onPHPPing);
		startServices.onClick.AddListener(startService);
		setDBName.onClick.AddListener(dbChange);
		setRes.onClick.AddListener(resChange);
		setSpeed.onClick.AddListener(speedChange);
		exportDBBtn.onClick.AddListener(dbExport);
		confirmDelData.onClick.AddListener(dbRemove);
		audioControl.onClick.AddListener(setAudioState);
		GapNoteSet.onClick.AddListener(handleGapNotification);
		
		//init states
		lastBoxReadState = Interlace.comManager.boxReadOnline;
		boxReadColor = beginBoxRead.colors;
		audioButtonColor = audioControl.colors;			
		resWarning.enabled = false;
		resWarning1.enabled = false;
		startupPanel.SetActive(true);
		exportingPanel.SetActive(false);
		DBCharWarn.enabled = false;
		GapNoteIF.placeholder.GetComponent<Text>().text = Interlace.comManager.warnGap.ToString(); 
		
		//persistence
		Interlace.comManager.encResolution = PlayerPrefs.GetFloat("encRes"); //persistence
		currentSpeed = PlayerPrefs.GetInt("speed"); //persistence
		
		
		//invokes
		InvokeRepeating("statusCheck", uiUpdateRate, uiUpdateRate); //open up our status check and keep checking at the update rate
		Invoke("getPHPUpdate",1);
		Invoke("getPHPUpdate",2);
		Invoke("getPHPUpdate",3);
		Invoke("getPHPUpdate",4);//has to be called multiple times due to some strange issue I don't have time to determine
		Invoke("checkPHP",5);
		Invoke("checkArd",5);
		
		if (Interlace.comManager.boxReadOnline) { //catch and disable boxread if ard is still forked or a system restart happens
			Debug.Log("Resetting Box Read Mode");
			Interlace.comManager.endBoxReadARD();
		}
	}

	void Update () {
		if ((!usbOn && (lastUSBOn != usbOn)) || (usbToggle.isOn && !usbOn)) { //two if's handle checking usb plugged in or not
			noUSBToggle.isOn=true;
			usbToggle.isOn=false;
			lastUSBOn = usbOn;
		}
		if (usbOn && (lastUSBOn != usbOn)) {
			noUSBToggle.isOn=false;
			usbToggle.isOn=true;
			lastUSBOn = usbOn;
		}
	}
	
	void setAudioState() { //handle changing audio enable/dis state
		if (Interlace.comManager.usingAudio) { //if true, set false
			Interlace.comManager.usingAudio = false;
			audioControlTxt.text = "ENABLE AUDIO";
			audioButtonColor.normalColor = Color.grey;
			audioButtonColor.highlightedColor = Color.grey;
			audioControl.colors = audioButtonColor;
			return;
		}
		if (!Interlace.comManager.usingAudio) {//if false set true
			Interlace.comManager.usingAudio = true;
			audioControlTxt.text = "DISABLE AUDIO";
			audioButtonColor.normalColor = Color.white;
			audioButtonColor.highlightedColor = Color.white;
			audioControl.colors = audioButtonColor;
			return;
		}
	}
	
	void getPHPUpdate() { //simply checks php, redundant but for startup only
		phpCheck = DatabaseManager.dbManager.verifyPHP();
	}
	
	void checkPHP() { //will ping php and report that we are online or offline. Only called in start
		
		//Debug.Log("PHPCheck Result: "+phpCheck);
		if (phpCheck == "pong") {
			Debug.Log("Apache Online");
			Debug.Log("MySQL Online");
		}
		if (phpCheck != "pong") {
			Debug.Log("Apache Offline");
			Debug.Log("MySQL Offline");
		}
	}
	
	void checkArd() { //verifies if ard is online. Only ran in start
		if (Interlace.comManager.isConnected) {
			Debug.Log("Arduino Online");
		}
		if (!Interlace.comManager.isConnected) {
			Debug.Log("Arduino Offline");
		}
	}
	
	void onExit() { //handles quitting
		Application.Quit();
	}
	
	public void startService() { //takes care of starting services should they go offline
		string returndata = ServiceControl.startService();
		Debug.Log("Service start attempted. Data: '" +returndata+ "'. Blank = success.");
	}
	
	public void ConnSetting() { //enables System Status Panel
		if (!ConnectionPanel.activeSelf) {
			ConnectionPanel.SetActive(true);
			FHMIPanel.SetActive(false);
			return;
		} else {
			ConnectionPanel.SetActive(false);
			FHMIPanel.SetActive(false);
			return;
		}
	}
	
	public void COMChange() { //handles changing com port
		int comChoice = COMDD.value + 1;
		if (comChoice < 11) {
			if (!Interlace.comManager.isConnected) {
				Interlace.comManager.comPort = "COM"+comChoice.ToString();
				Debug.Log("COM PORT Updated!");
				COMGO.transform.gameObject.SetActive(false);
				return;
			} 
			if (Interlace.comManager.isConnected) {
				ConnectArd();
				if (!Interlace.comManager.isConnected) {
					Interlace.comManager.comPort = "COM"+comChoice.ToString();
					Debug.Log("Disconnected and COM PORT Updated!");
					COMGO.transform.gameObject.SetActive(false);
					return;
				} else {
					Debug.Log("Failed to Disconnect!");
				}
			}
		} if (comChoice == 11) {//for non-numerical due to linux
			if (!Interlace.comManager.isConnected) {
				Interlace.comManager.comPort = "/dev/ttyUSB0";
				Debug.Log("COM PORT Updated!");
				COMGO.transform.gameObject.SetActive(false);
				return;
			} 
			if (Interlace.comManager.isConnected) {
				ConnectArd();
				if (!Interlace.comManager.isConnected) {
					Interlace.comManager.comPort = "/dev/ttyUSB0";
					Debug.Log("Disconnected and COM PORT Updated!");
					COMGO.transform.gameObject.SetActive(false);
					return;
				} else {
					Debug.Log("Failed to Disconnect!");
				}
			}
		}		
	}
	
	public void BAUDChange() { //handles changing baud rate
		string baudChoice = BAUDIF.text;
		if (!Interlace.comManager.isConnected) {
			Interlace.comManager.baudRate = int.Parse(baudChoice);
			Debug.Log("BAUD Rate Updated!");
			BAUDGO.transform.gameObject.SetActive(false);
			return;
		} 
		if (Interlace.comManager.isConnected) {
			ConnectArd();
			if (!Interlace.comManager.isConnected) {
				Interlace.comManager.baudRate = int.Parse(baudChoice);
				Debug.Log("Disconnected and BAUD Rate Updated!");
				BAUDGO.transform.gameObject.SetActive(false);
				return;
			} else {
				Debug.Log("Failed to Disconnect!");
			}
		}
	}
	
	public void handleGapNotification() {
		string gapNotification = GapNoteIF.text;
		Interlace.comManager.warnGap = float.Parse(gapNotification);
		GapNoteIF.text = "";
		GapNoteIF.placeholder.GetComponent<Text>().text = gapNotification;
		GapNoteSet.transform.gameObject.SetActive(false);
	}
	
	public void dbChange() { //handles changing database
		string newDBChoice = dbName.text;
		if (Regex.IsMatch(newDBChoice, @"^[a-zA-Z0-9_]+$")) { //parse the string for any non standard chars
			Interlace.comManager.currentDB = newDBChoice;
			Debug.Log("DB Updated!");
			setDBName.transform.gameObject.SetActive(false);
			DBCharWarn.enabled = false;//REMOVE ACTIVE WARNING IF THERE
			return;
		} else { //if contains bad chars
			Debug.Log("Failed to update. Only use letters, numbers and '_'.");
			DBCharWarn.enabled = true;//MAKE ACTIVE WARNING UNTIL FIXED
			return;
		}
	}
	
	public bool grabUSB() { //does checks to verify if USB is in
		string usbName = ServiceControl.findUSB();
		if (usbName == "") {
			usbPathTest.text = "USB Name: NO USB!";
			usbTgText.text = "CANNOT EXPORT";
			return false;
		} else {
			usbPathTest.text = "USB Name: "+usbName;
			usbTgText.text = "Export to USB";
			return true;
		}
	}
	
	public void dbExport() { //handles exporting database using dbmanager functions | Modified to handle multiple lanes and dual reading
		exportingPanel.SetActive(true);
		string path = "";
		string data = "";
		string db = Interlace.comManager.currentDB;
		if (inStream1.isOn) {//stream 1
			if (usbToggle.isOn) {
				string usbName = ServiceControl.findUSB();
				usbPathTest.text = "USB Name: "+usbName;
				if (usbName.Contains(" ")) {
					usbName = usbName.Replace(" ","\\ ");
				}
				path = "/var/www/html/dbExport/";
				string finalPath = "/media/fives/"+usbName+"/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "1");
				string moveFile = ServiceControl.moveToUSB(path+db+"L1.csv",finalPath);
				if (moveFile != "") {
					Debug.Log("MV ERROR: "+moveFile);
				}
				Debug.Log("Exporting Lane 1 to: "+finalPath);
			} 
			if (!usbToggle.isOn) {
				path = "/home/fives/FMB/DBExport/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "1");
				Debug.Log("Exporting Lane 1 to: "+path);
			}
			if (data!="") {
			Debug.Log("DB Export Data Error: "+data);
			}
			exportingPanel.SetActive(false);
			Debug.Log("Lane One DB Exported");
			return;
		}
		if (inStream2.isOn) { //stream 2
			if (usbToggle.isOn) {
				string usbName = ServiceControl.findUSB();
				usbPathTest.text = "USB Name: "+usbName;
				if (usbName.Contains(" ")) {
					usbName = usbName.Replace(" ","\\ ");
				}
				path = "/var/www/html/dbExport/";
				string finalPath = "/media/fives/"+usbName+"/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "2");
				string moveFile = ServiceControl.moveToUSB(path+db+"L2.csv",finalPath);
				if (moveFile != "") {
					Debug.Log("MV ERROR: "+moveFile);
				}
				Debug.Log("Exporting Lane 2 to: "+finalPath);
			} 
			if (!usbToggle.isOn) {
				path = "/home/fives/FMB/DBExport/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "2");
				Debug.Log("Exporting Lane 2 to: "+path);
			} 
			if (data!="") {
			Debug.Log("DB Export Data Error: "+data);
			}
			exportingPanel.SetActive(false);
			Debug.Log("Lane Two DB Exported");
			return;
		}
		if (inStreamDual.isOn) {//dual streams
			if (usbToggle.isOn) {
				string usbName = ServiceControl.findUSB();
				usbPathTest.text = "USB Name: "+usbName;
				if (usbName.Contains(" ")) {
					usbName = usbName.Replace(" ","\\ ");
				}
				path = "/var/www/html/dbExport/";
				string finalPath = "/media/fives/"+usbName+"/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "1");
				string moveFile = ServiceControl.moveToUSB(path+db+"L1.csv",finalPath);
				if (moveFile != "") {
					Debug.Log("MV ERROR: "+moveFile);
				}
				data = DatabaseManager.dbManager.exportDatabase(db, path, "2");
				moveFile = ServiceControl.moveToUSB(path+db+"L2.csv",finalPath);
				if (moveFile != "") {
					Debug.Log("MV ERROR: "+moveFile);
				}
				Debug.Log("Exporting Lane 1 & 2 to: "+finalPath);
			} 
			if (!usbToggle.isOn) {
				path = "/home/fives/FMB/DBExport/";
				data = DatabaseManager.dbManager.exportDatabase(db, path, "1");
				data = DatabaseManager.dbManager.exportDatabase(db, path, "2");
				Debug.Log("Exporting Lane 1 & 2 to: "+path);
			} 
			if (data!="") {
			Debug.Log("DB Export Data Error: "+data);
			}
			exportingPanel.SetActive(false);
			Debug.Log("Dual Lane DB Exported");
			return;
		}
		
	}
	
	public void dbRemove() { //removes ALL databases except critical db's
		DatabaseManager.dbManager.removeDatabases();
		confDelDataText.text = "All data removed!";
		confirmDel.SetActive(false);
		return;
	}
	
	public void resChange() { //handles changing resolution
		string newRes = resName.text;
		try {
		Interlace.comManager.encResolution = float.Parse(newRes)/2;
		PlayerPrefs.SetFloat("encRes", Interlace.comManager.encResolution);
		Debug.Log("Resolution Updated!");
		setRes.transform.gameObject.SetActive(false);
		return;
		} catch (Exception e) {
			Debug.Log("Error changing resolution: "+e);
			return;
		}	
	}
	
	public void speedChange() { //handles changing speed
		string newSpeed = getSpeed.text;
		try {
		currentSpeed = int.Parse(newSpeed);
		PlayerPrefs.SetInt("speed", currentSpeed);
		Debug.Log("Speed Updated!");
		setSpeed.transform.gameObject.SetActive(false);
		return;
		} catch (Exception e) {
			Debug.Log("Error changing speed: "+e);
			return;
		}	
	}
	
	public void FHMIDisplay() {//enables FHMI Settings panel
		if (!FHMIPanel.activeSelf) {
			ConnectionPanel.SetActive(false);
			FHMIPanel.SetActive(true);
			return;
		} else {
			FHMIPanel.SetActive(false);
			ConnectionPanel.SetActive(false);
			return;
		}
	}
	
	public void statusCheck() { //This method handles actual logic for the status system (refreshed based on invoke)
	
		if (lastBoxReadState!=Interlace.comManager.boxReadOnline) { //check if boxread is active and react
			if (!Interlace.comManager.boxReadOnline) {
				boxReadColor.normalColor = Color.red;
				boxReadColor.highlightedColor = Color.red;
				beginBoxReadTxt.text= "BEGIN BOX READ";
			}
			if (Interlace.comManager.boxReadOnline) {
				boxReadColor.normalColor = Color.green;
				boxReadColor.highlightedColor = Color.green;
				beginBoxReadTxt.text= "STOP BOX READ";	
			}
			lastBoxReadState = Interlace.comManager.boxReadOnline;
		}
		
		if (settingsMenu.activeSelf) { //if we're in the settings panel, update items
			phpCheck = DatabaseManager.dbManager.verifyPHP(); //grab a cheeky php check
			if (phpCheck=="pong") {
				mySQLStatus.text = "MySQL Server: Online";
				apacheStatus.text = "Apache Server: Online";
			} 
			if (phpCheck!="pong") {
				mySQLStatus.text = "MySQL Server: Offline";
				apacheStatus.text = "Apache Server: Offline";
			} //to here out if you want to stop lag on windows for testing only
			lastDBMsg.text = "Last DB Message: " + DatabaseManager.dbManager.lastDBMsg;
			lastRawSerial.text = "Last Raw Serial Data: " + Interlace.comManager.lastValue; 
			COMChannel.text = "COM Channel: " + Interlace.comManager.comPort; //comPort to be set via connection settings menu
			BAUDRate.text = "BAUD Rate: " + Interlace.comManager.baudRate; //baudRate to be set via the connection settings menu

			if (Interlace.comManager.isConnected) { //Connection status based off of boolean from interlace
				connStatus.text = "Arduino Status: Online";
				connectARDTxt.text = "DISCONNECT ARDUINO";
				serialStatus.text = "Serial Status: Online on " + Interlace.comManager.comPort;
			} 
			if (!Interlace.comManager.isConnected) {
				connStatus.text = "Arduino Status: Offline";
				connectARDTxt.text = "CONNECT ARDUINO";
				serialStatus.text = "Serial Status: NO TX/RX";
			}
			string fullCOMPORT = Interlace.comManager.comPort;
			string comport = Interlace.comManager.comPort.Substring(3);
			if (fullCOMPORT!="/dev/ttyUSB0") {
				currentCOM = int.Parse(comport)-1;
				if (currentCOM != lastCOM) {
					checkCOM();
				}
			} 
			if (fullCOMPORT=="/dev/ttyUSB0") {
				currentCOM = 11;
				if (currentCOM != lastCOM) {
					checkCOM();
				}
			}
			currentBAUD =  Interlace.comManager.baudRate;
			if (currentBAUD != lastBAUD) {
				checkBAUD();
			}
		}
		
		if (exportMenu.activeSelf) { //if were on the export panel, update the panel items
			usbOn = grabUSB(); //grab usb status
			if (inStream1.isOn) {
			currentSelectedLane.text = "Current Lane: 1";
			}
			if (inStream2.isOn) {
			currentSelectedLane.text = "Current Lane: 2";
			}
			if (inStreamDual.isOn) {
			currentSelectedLane.text = "Current Lane: Dual";
			}
		}
		
		//check statuses and update their texts
		beginBoxRead.colors = boxReadColor;
		audioControl.colors = audioButtonColor;
		currentRes.text = "Current ENC Res: "+Interlace.comManager.encResolution*2 + "\" | Real Res: "+Interlace.comManager.encResolution+"\"";
		currentRes1.text = "Current ENC Res: "+Interlace.comManager.encResolution*2 + "\" | Real Res: "+Interlace.comManager.encResolution+"\"";
		speedTxt.text = "Current Speed: " + currentSpeed.ToString();
		speedTxt1.text = "Current Speed: " + currentSpeed.ToString();
		currentDB.text = "Current Database Name: "+Interlace.comManager.currentDB;
		currentDB1.text = "Current Database Name: "+Interlace.comManager.currentDB;
		currentDB2.text = "Current Selected Database: "+Interlace.comManager.currentDB;
		//end check statuses
		
		if (((Interlace.comManager.encResolution<0.125) && (currentSpeed <= 600)) || ((Interlace.comManager.encResolution<0.25) && (currentSpeed > 600))) { //warn if resolution is bad
			resWarning.enabled = true;
			resWarning1.enabled = true;
		} else {
			resWarning.enabled = false;
			resWarning1.enabled = false;
		}
		
		startupPanel.SetActive(false);	//remove the startup panel after first init
	}
	
	private void checkCOM() { //checks for com update and pushes (kinda complex due to way the unity engine works with value fields)
		if (currentCOM != 11) {
			string comport = Interlace.comManager.comPort.Substring(3);
			COMDD.value = int.Parse(comport)-1;
			lastCOM = int.Parse(comport)-1;
			currentCOM = int.Parse(comport)-1;
			COMGO.transform.gameObject.SetActive(false);
			return;
		} if (currentCOM == 11) { 
			COMDD.value = 11;
			lastCOM = 11;
			currentCOM = 11;
			COMGO.transform.gameObject.SetActive(false);
		}
	}
	
	private void checkBAUD() { //checks baud rate
		currentBAUD =  Interlace.comManager.baudRate;
		BAUDIF.text = currentBAUD.ToString();
		lastBAUD = currentBAUD;
		BAUDGO.transform.gameObject.SetActive(false);
		return;
	}
	
	private void ConnectArd() { //pushes connect to audrino for interlace
		if (!Interlace.comManager.isConnected) {
			Interlace.comManager.OpenCom();
			return;
		} if (Interlace.comManager.isConnected) {
			Interlace.comManager.CloseCom();
			return;
		}		
	}
	
	private void PingArd() { //pushes ping arduino to interlace
		if (Interlace.comManager.isConnected) {
			Interlace.comManager.pingARD();
			return;
		} if (!Interlace.comManager.isConnected) {
			Debug.Log("Must connect before Ping!");
			return;
		}		
	}
	
	public void onPHPPing() { //handles pinging php via dbmanager
		string pingdata = DatabaseManager.dbManager.verifyPHP();
		if (pingdata=="pong") {
			return;
		} else {
			Debug.Log("Ping failed, check services");
		}
	}
	
	private void StartBoxMeasure() { //controls button color and pushing to interlace for box reading | Was rewritten to be better :D
		if (!Interlace.comManager.boxReadOnline) {
			if (!Interlace.comManager.isConnected) {
				Interlace.comManager.OpenCom();
				if (Interlace.comManager.isConnected) {
					if (inStream1.isOn) {
						Interlace.comManager.beginBoxReadARD();
					}
					if (inStream2.isOn) {
						Interlace.comManager.beginBoxReadARDL2();
					}
					if (inStreamDual.isOn) {
						Interlace.comManager.beginBoxReadARDDual();
					}
					boxReadColor.normalColor = Color.green;
					boxReadColor.highlightedColor = Color.green;
					beginBoxReadTxt.text= "STOP BOX READ";
					beginBoxRead.colors = boxReadColor;
					return;
				} else {
					Debug.Log("Error starting Box read!");
				}
			}
			if (Interlace.comManager.isConnected) {
				if (inStream1.isOn) {
					Interlace.comManager.beginBoxReadARD();
				}
				if (inStream2.isOn) {
					Interlace.comManager.beginBoxReadARDL2();
				}
				if (inStreamDual.isOn) {
					Interlace.comManager.beginBoxReadARDDual();
				}
				boxReadColor.normalColor = Color.green;
				boxReadColor.highlightedColor = Color.green;
				beginBoxReadTxt.text= "STOP BOX READ";
				beginBoxRead.colors = boxReadColor;
				return;
			}
		}
		if (Interlace.comManager.boxReadOnline) {
			if (Interlace.comManager.isConnected) {
				Interlace.comManager.endBoxReadARD();
				boxReadColor.normalColor = Color.red;
				boxReadColor.highlightedColor = Color.red;
				beginBoxReadTxt.text= "BEGIN BOX READ";
				beginBoxRead.colors = boxReadColor;
			}
			if (!Interlace.comManager.isConnected) {
				Interlace.comManager.endBoxReadARD();
				boxReadColor.normalColor = Color.red;
				boxReadColor.highlightedColor = Color.red;
				beginBoxReadTxt.text= "BEGIN BOX READ";
				beginBoxRead.colors = boxReadColor;
			}
			return;
		}
	}
} //EOC
