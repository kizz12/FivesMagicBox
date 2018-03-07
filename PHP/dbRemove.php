<?php 
    //customized php script to handle exporting specific db via post from C#
    $mysqlUserName = "root";
    $mysqlPassword = "123";
    $mysqlHostName = "localhost";
	$currentDatabases = array();
	$i = 0;

	$mysqli = new mysqli($mysqlHostName,$mysqlUserName,$mysqlPassword); 
	$result = $mysqli->query("SHOW DATABASES");     
	while ($row = mysqli_fetch_array($result)) {
		$currentDatabases[] = $row[0];
		//echo $i.": ".$row[0]."<br>";
		//echo $i.": ".$currentDatabases[i]."<br>";
		$i=$i+1;
	}
	
	foreach ($currentDatabases as &$value) {
		if (($value != "information_schema") && ($value != "mysql") && ($value != "performance_schema") && ($value != "") && ($value != null)) {
			$delres = $mysqli->query("DROP DATABASE $value"); 
		}
	}
	unset($value);
	$mysqli->close();
	//$mysqli->select_db($DbName); 
?>