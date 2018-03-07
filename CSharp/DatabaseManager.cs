using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using UnityEngine;
using System.Threading;

public class DatabaseManager : MonoBehaviour {

	public static DatabaseManager dbManager;
	
	//This script takes care of passing data to PHP as well as parsing data for the database, pinging apache/php and other database manipulation
	
	public string lastDBMsg;
	//private bool online = true;
	private Thread webGrabThread;
	private volatile bool checking;
	public string phpResponse;

	
	void Start () {
		dbManager = this;
	}

	public string verifyPHP() { //puts socket in thread to listen so that main thread doesn't freeze
        if (!this.checking) {
			webGrabThread = new Thread(pingPHP);//create a new thread
			webGrabThread.IsBackground = true; //assign it to background
            this.checking = true;//mark it as listening
            webGrabThread.Start();//launch the thread
			return phpResponse;
        } else {
			Debug.Log("Cannot Ping PHP, Thread is open!");
			return "";
		}
		
    }

	public string sendData(string URL, string command, string db, string gap, string length, string lane) { //This function sends data to any php file via post
		using(WebClient client = new WebClient()) {
			try {
				client.DownloadString(URL); 
			} catch (Exception e) {
				//online = false;
				return e.ToString();
			}
			var reqparm = new System.Collections.Specialized.NameValueCollection();
			reqparm.Add("command", command);
			reqparm.Add("database", db);
			reqparm.Add("gap", gap);
			reqparm.Add("length", length); 
			reqparm.Add("lane", lane);
			byte[] responsebytes = client.UploadValues(URL, "POST", reqparm);
			string responsebody = Encoding.UTF8.GetString(responsebytes);
			lastDBMsg = responsebody;
			return responsebody;
		}
	}
	
	public Tuple<string, string, string, string> parseBoxData(string serialString) { //Parses data from arduino for database, returns 3 values
		string g = "G:"; //declare static values contained in the string
		string l = ";L:";
		string id = ";ID:";
		string semi = ";";
		string finalGap; //our final storage vars
		string finalLength;
		string finalID;
		string finalLane;
		
		int gIndex = serialString.IndexOf(g, 0); //finding the start position of each static value and deduce the length and position of respective values
		int lIndex = serialString.IndexOf(l, 0);
		int idIndex = serialString.IndexOf(id, 0);
		int eol = serialString.IndexOf(semi, idIndex+3);
		finalGap = serialString.Substring(gIndex+2, lIndex-3); //extract the values and insert them into our final var
		finalLength = serialString.Substring(lIndex+3, idIndex-(lIndex+3));
		finalID = serialString.Substring(idIndex+4, eol-(idIndex+4));
		finalLane = serialString.Substring(gIndex-1, 1);
		
		var data = new Tuple<string,string,string,string>(finalLane,finalGap,finalLength,finalID); //output them  to a tuple then return
		return data;
	}
	
	public void pingPHP() { //a repeatable ping
		try {
			string myURL = "http://localhost/dbHandle.php";
			string response = sendData(myURL,"ping","","", "", "");
			//online = true;
			phpResponse = response;
			this.checking = false;
			return;
		} catch (Exception e) {
			Debug.Log("Error Pinging PHP: "+e);
			//online = false;
			phpResponse = "";
			this.checking = false;
			return;
		}
	}
	
	public string exportDatabase(string dbName, string path, string lane) { //exports a db
		string myURL = "http://localhost/dbExport.php";
		string response = sendData(myURL,path,dbName,"", "", lane);
		return response;
	}
	
	public bool removeDatabases() { //removes ALL db's
		string myURL = "http://localhost/dbRemove.php";
		sendData(myURL,"","","","","");
		return true;
	}
}


