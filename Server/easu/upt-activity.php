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
		} else {
			echo $arrFail;
		}
	} else {
		echo $arrUserNotFound;
	}
?>
