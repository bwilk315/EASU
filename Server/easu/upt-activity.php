<?php
	require 'local/db-access.php';
	$userName = $_POST['userName'];
	// Get id of the user called 'username'.
	$userQuery = $database->select(
		$tabUsers,
		['id'],
		['name' => $userName]
	);
	// If user is found ...
	if($userQuery)
	{
		$currentTime = time();
		$userId = $userQuery[0]['id']; // Extract user id.
		// Update last activity time to the current.
		$update = $database->update(
			$tabSessions, 						// TABLE
			['last_activity' => $currentTime], 	// VALUES
			['user_id' 		=> $userId]			// WHERE
		);
		if($update) {
			echo $arrUpdated;
			// Check others (we are careful).
			$sessionQuery = $database->select(
				$tabSessions,
				['user_id', 'last_activity']
			);
			foreach($sessionQuery as $session) {
				$userLastActivity 	= $session['last_activity'];
				$userId 			= $session['user_id'];
				if($userLastActivity + $updateInterval * 2.0 < $currentTime) {
					$database->delete(
						$tabSessions,
						['user_id' => $userId]
					);
				}
			}
		} else {
			echo $arrFail;
		}
	} else {
		echo $arrUserNotFound;
	}
?>
