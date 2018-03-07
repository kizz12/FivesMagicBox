using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class ServiceControl {
	
	public static string ExecuteCommand(string command) { //this little guy allows me to send commands direct to unix
		string consoleLog = "";
		Process proc = new System.Diagnostics.Process ();
		proc.StartInfo.FileName = "/bin/bash";
		proc.StartInfo.Arguments = "-c \" " + command + " \"";
		proc.StartInfo.UseShellExecute = false; 
		proc.StartInfo.RedirectStandardOutput = true;
		proc.Start ();
		while (!proc.StandardOutput.EndOfStream) {
			consoleLog = proc.StandardOutput.ReadLine ();
		}
		return consoleLog;
    }
	
	public static string startService() { //this function will start apache2 and mysql
		string servicedata = ExecuteCommand("service apache2 start; service mysql start");
		DatabaseManager.dbManager.verifyPHP(); //send a ping to verify that were online now
		return servicedata;
	}
	
	public static string findUSB() { //check and returns the name of connected USB's
		string servicedata = ExecuteCommand("ls /media/fives/");
		return servicedata;
	}
	
	public static string moveToUSB(string orgFile, string path) { //Moves a file from a dir to usb
		//Thread.Sleep(5000); // may need because of write time
		string servicedata = ExecuteCommand("cp "+orgFile+" "+path);
		ExecuteCommand("rm "+orgFile);
		return servicedata;
	}
}



