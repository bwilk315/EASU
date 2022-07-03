<?php
	if(empty($_SERVER['DOCUMENT_ROOT'])) {
		$root = __DIR__ . '/../../';
	} else {
		$root = $_SERVER['DOCUMENT_ROOT'];
	}
	require $root . '/easu/local/composer/vendor/autoload.php';
	use Medoo\Medoo;

	/*** Server properties. Ones with wrong/invalid values are set after configuration decode. ***/
	$configFile 			= $root . '/easu/local/server.json';
	$structureFile 			= $root . '/easu/local/structure.json';
	$database 				= null;
	$updateInterval 		= -1.0;
	$timerInterval 			= -1.0;
	$tabUsers				= 'unknown';
	$tabSessions 			= 'unknown';
	/*** AMR (Account Manager Response) cases. ***/
	$amrLoggedIn 			= 0;
	$amrUserNotFound 		= 1;
	$amrWrongPassword 		= 2;
	$amrAlreadyLoggedIn 	= 3;
	$amrLoggedOut 			= 4;
	$amrAlreadyLoggedOut 	= 5;
	/*** ARR (Activity Reporter Response) cases. ***/
	$arrUpdated 			= 0;
	$arrUserNotFound 		= 1;
	$arrFail 				= 2;
	// Decode configuration data from JSON format into associative array.
	if(file_exists($configFile)) {
		$file 				= fopen($configFile, 'r'); 					// Open configuration file.
		$content 			= fread($file, filesize($configFile));		// Read content of configuration file.
		$conf 				= json_decode($content, associative: true);	// Decode configuration to associative array.
		fclose($file); // Close file, it isn't needed anymore.
		// Make Medoo object for database management, use configuration data.
		$database = new Medoo([
				'type' 		=> $conf['dbType'],
				'host' 		=> $conf['dbHost'],
				'database' 	=> $conf['dbName'],
				'username' 	=> $conf['dbUser'],
				'password' 	=> $conf['dbPass']
		]);
		/*** Server properties which needs configuration data to be set. ***/
		$updateInterval 	= (float)$conf['updateInterval'] / 1000.0;
		$timerInterval 		= (float)$conf['timerInterval'] / 1000.0;
		$tabUsers 			= $conf['usersTableName']; 		// Name of users table.
		$tabSessions 		= $conf['sessionsTableName']; 	// Name of connections table.
	}
?>
