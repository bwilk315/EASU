<?php
	/*
		LOGIN.PHP:
		Inserts new connection if user provided correct data (username and password).
		Handles any error which can occurr during the log-in process.
	*/
	require 'local/db-access.php';

	$username = $_GET['username'];
	$password = $_GET['password'];
	// Get data record of user called 'username'.
	$userQuery = $database->select(
		$usertab,				// TABLE
		['id', 'password'],		// COLUMNS
		['name' => $username]	// WHERE
	);

	if(count($userQuery) > 0) {
		// Get first (it should be the only one) record data.
		$userData = $userQuery[0];
		// Compare 'password' (provided by user) with password found in the database.
		if($password == $userData['password']) {
			$currentTime = time();
			// Get information if user with the data provided is not already logged in.
			$connQuery = $database->select(
				$conntab,						// TABLE
				['last_activity'],				// COLUMNS
				['user_id' => $userData['id']]	// WHERE
			);
			if(count($connQuery) > 0) {
				// Get current connection data array.
				$connData = $connQuery[0];
				// Time after which account will be free (it is normally dynamically updated).
				$connTimeLimit = (int)$connData['last_activity'] + constant('UPDATE_INTERVAL');
				// If time after last activity update is greater than update interval, account is free.
				if($currentTime > $connTimeLimit) {
					$database->delete($conntab, [
						'user_id' => $userData['id']
					]);
					echo constant('AMR_USER_LOGGED_OUT');
				}
				/* 	If current time is not greater than a connection time limit, user has lenghten
				 	lease by update-interval.
				 */
				else echo constant('AMR_ALREADY_LOGGED_IN');
			}
			// If connection is not found, create a one.
			else {
				$database->insert(
					$conntab,										// TABLE
					[												// VALUES
						'user_id' 			=> $userData['id'],
						'port' 				=> $_SERVER['SERVER_PORT'],
						'server_address' 	=> $_SERVER['SERVER_NAME'],
						'last_activity' 	=> $currentTime
					]
				);
				echo constant('AMR_LOGGED_IN');
			}
		}
		// If passwords don't match, throw error.
		else echo constant('AMR_WRONG_PASSWORD');
	}
	else echo constant('AMR_USER_NOT_FOUND');
?>
