<?php
	/*
		DBACCESS.PHP:
		Connects to database using data read from JSON configuration file 'jagc-server.json'.
		Provides functionality essential for working with JAGC UPM (Unity PHP MySQL) structure.
	*/
	require '../vendor/autoload.php';
	use Medoo\Medoo;
	// Here go fully independent constants.
	if(!defined('CFILE')) {
		define('CFILE', 'local/server.json'); // Configuration file path.
	}
	if(!defined('SFILE')) {
		define('SFILE', 'local/structure.json'); // Structure file path.
	}
	if(!defined('AMR_LOGGED_IN')) {
		define('AMR_LOGGED_IN', 0); // User logged in successfuly.
	}
	if(!defined('AMR_USER_NOT_FOUND')) {
		define('AMR_USER_NOT_FOUND', 1); // User not found in the database.
	}
	if(!defined('AMR_WRONG_PASSWORD')) {
		define('AMR_WRONG_PASSWORD', 2); // User provided wrong password for account.
	}
	if(!defined('AMR_ALREADY_LOGGED_IN')) {
		define('ARM_ALREADY_LOGGED_IN', 3); // User is already logged in to specific account.
	}
	if(!defined('AMR_USER_LOGGED_OUT')) {
		define('AMR_USER_LOGGED_OUT', 4); // Last user is still logging out.
	}
	if(!defined('AUR_UPDATED')) {
		define('AUR_UPDATED', 0);
	}
	if(!defined('AUR_USER_NOT_FOUND')) {
		define('AUR_USER_NOT_FOUND', 1);
	}
	if(!defined('AUR_FAIL')) {
		define('AUR_FAIL', 2);
	}
	if(file_exists(constant('CFILE'))) {
		// Decode configuration data from JSON format into associative array.
		$file 		= fopen(constant('CFILE'), 'r');
		$content 	= fread($file, filesize(constant('CFILE')));
		$conf 		= json_decode($content, true);
		fclose($file);
		// Make Medoo object for database management.
		$database = new Medoo([
				'type' 		=> $conf['dbtype'],
				'host' 		=> $conf['dbhost'],
				'database' 	=> $conf['dbname'],
				'username' 	=> $conf['dbuser'],
				'password' 	=> $conf['dbpass']
		]);
		// Here go declarations of common variables which need to use configuration.
		if(!defined('UPDATE_INTERVAL')) {
			$ui = (float)$conf['updateinterval'];
			define('UPDATE_INTERVAL', $ui / 1000.0);
		}
		$usertab = $conf['usertabname']; // Name of users table.
		$conntab = $conf['conntabname']; // Name of connections table.
	}
?>
