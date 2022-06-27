<?php
	/*
		UPDATE-ACTIVITY.PHP:
		Lengthens the lease time of this connection by update interval.
		If not called every update-interval value, connection gets outdated and
		could be removed when some user will perform connection-checking related action.
	*/
	require 'local/db-access.php';

	$userName = $_GET['username'];
	// Get id of the user called 'username'.
	$userQuery = $database->select($usertab, ['id'], ['name' => $userName]);
	// If user is found ...
	if(count($userQuery) > 0)
	{
		$currentTime = time();
		$uid = $userQuery[0]['id']; // Extract user id.
		// Update last activity time to the current.
		$update = $database->update(
			$conntab, 							// TABLE
			['last_activity' => $currentTime], 	// VALUES
			['user_id' 		=> $uid]			// WHERE
		);
		if($update->rowCount() > 0) {
			echo constant('AUR_UPDATED');
			// Check others (we are careful).
			$connQuery = $database->select($conntab, ['user_id', 'last_activity']);
			foreach($connQuery as $conn) {
				$userTime 	= $conn['last_activity'];
				$userId 	= $conn['user_id'];
				if($userTime + constant('UPDATE_INTERVAL') * 2.0 < $currentTime) {
					$database->delete($conntab, ['user_id' => $userId]);
				}
			}
		} else {
			echo constant('AUR_FAIL');
		}
	} else {
		echo constant('AUR_USER_NOT_FOUND');
	}
?>
