<?php
	require 'local/db-access.php';
	$userName = $_POST['userName'];
	// Find the record of user called 'username'.
	$userQuery = $database->select(
		$tabUsers,
		['id'],
		['name' => $userName]
	);
	if($userQuery) {
		// Extract user ID from query result.
		$userId = $userQuery[0]['id'];
		// Delete connection with found ID (ID of the user logging-out).
		$delete = $database->delete(
			$tabSessions,
			['user_id' => $userId]
		);
		if($delete->rowCount() > 0) {
			echo $amrLoggedOut;
		} else {
			echo $amrAlreadyLoggedOut;
		}
	} else {
		echo $amrUserNotFound;
	}
?>
