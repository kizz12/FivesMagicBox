<?php 

//$currentDB;

handleInMsg(); //this guy will call our constructive functions to make stuff happen

function ConnectMySQL($currentDB) { //Connects to DB
	$servername = "localhost";
	$username = "root";
	$password = "123";
	$success = false;
	// Create connection
	if ($currentDB=="") {
		 $conn = new mysqli($servername, $username, $password);
	}
	if ($currentDB!="") {
		 $conn = new mysqli($servername, $username, $password, $currentDB);
	}
	// Check connection
	if ($conn->connect_error) {
		//die("");
		$success = false;
		return $success;
	} 
	return $conn;
}

function disconMySQL($conn) {
	$conn->close();
}

function createDB($conn, $dbName) { //creates DB
	$success = false;
	$sql = "CREATE DATABASE IF NOT EXISTS ".$dbName;
	if ($conn->query($sql) === TRUE) {
		//echo "DB Created";
		$success = true;
		return $success;
	} else {
		$success = false;
		return $success;
	}

}

function createTable($conn) { //automatically sets up tables for DB
	$success = false;
	$sql = "CREATE TABLE IF NOT EXISTS Lane1 (
	id INT(6) UNSIGNED AUTO_INCREMENT PRIMARY KEY, 
	gap FLOAT(6) UNSIGNED NOT NULL,
	length FLOAT(6) UNSIGNED NOT NULL,
	currentTime TIME(3),
	date TIMESTAMP)";
	$sql2 = "CREATE TABLE IF NOT EXISTS Lane2 (
	id INT(6) UNSIGNED AUTO_INCREMENT PRIMARY KEY, 
	gap FLOAT(6) UNSIGNED NOT NULL,
	length FLOAT(6) UNSIGNED NOT NULL,
	currentTime TIME(3),
	date TIMESTAMP)";
	
	if (($conn->query($sql) === TRUE) && ($conn->query($sql2) === TRUE)) {
		//echo "Table Added";
		$success = true;
		return $success;
	} else {
		$success = false;
		return $success;
	}
}

function insertData($conn, $gap, $length, $lane) { //pumps data into table
	$sql = "INSERT INTO Lane".$lane." (gap, length, currentTime)
	VALUES ('".$gap."', '".$length."',NOW(3))";

	if ($conn->query($sql) === TRUE) {
		echo "Box Added";
	} else {
		echo "Error: " . $sql . "<br>" . $conn->error;
	}
}

function handleInMsg() { //listens for commands via post
	$command = $_POST["command"];
	$postDB = $_POST["database"];
	$postGap = $_POST["gap"];
	$postLength = $_POST["length"];
	$postLane = $_POST["lane"];
	switch ($command) {
		case "insertData":
			$tempVal = "";
			$currentConn = ConnectMySQL($tempVal);
			createDB($currentConn, $postDB);
			disconMySQL($currentConn);
			$currentConn = ConnectMySQL($postDB);
			createTable($currentConn);
			insertData($currentConn,$postGap,$postLength,$postLane);
			disconMySQL($currentConn);
			//echo "Data Added";
			break;
		case "ping":
			echo "pong";
			break;
		default:
			echo "Command not recognized: ".$command;
			break;
	}
}
?>
