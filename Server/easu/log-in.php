<?php
	require 'local/db-access.php';
	$userName = $_POST['userName'];
	$password = $_POST['password'];
	// Get data record of user called 'userName'.
	$userQuery = $database->select(
		$tabUsers,				// TABLE
		['id', 'password'],		// COLUMNS
		['name' => $userName]	// WHERE
	);
	if($userQuery) {
		$userData = $userQuery[0]; // Get first (it should be the only one) record data.
		// Compare 'password' (provided by user) with password found in the database.
		if($password == $userData['password']) {
			$currentTime = time();
			// Get information if user with the data provided is not already logged in.
			$sessionQuery = $database->select(
				$tabSessions,					// TABLE
				['last_activity'],				// COLUMNS
				['user_id' => $userData['id']]	// WHERE
			);
			if($sessionQuery) {
				// Get current connection data array.
				$sessionData = $sessionQuery[0];
				// Time after which account will be free (it is normally dynamically updated).
				$sessionTimeLimit = (int)$sessionData['last_activity'] + $updateInterval;
				// If time after last activity update is greater than update interval, account is free.
				if($currentTime > $sessionTimeLimit) {
					$database->delete($tabSessions, [
						'user_id' => $userData['id']
					]);
					include $_SERVER['DOCUMENT_ROOT'] . '/easu/log-in.php';
				}
				/* 	If current time is not greater than a connection time limit, user has lenghten
				 	lease by update-interval.
				 */
				else {
					echo $amrAlreadyLoggedIn;
				}
			}
			// If connection is not found, create a one.
			else {
				$database->insert(
					$tabSessions,									// TABLE
					[												// VALUES
						'user_id' 			=> $userData['id'],
						'port' 				=> $_SERVER['SERVER_PORT'],
						'server_address' 	=> $_SERVER['SERVER_NAME'],
						'last_activity' 	=> $currentTime
					]
				);
				echo $amrLoggedIn;
			}
		}
		// If passwords don't match, throw error.
		else {
			echo $amrWrongPassword;
		}
	} else {
		echo $amrUserNotFound;
	}
?>
