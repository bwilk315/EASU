<?php
	/*
		LOGOUT.PHP:
		Removes connection record from table 'connections' if possible.
	*/
	require 'local/db-access.php';

	$userName = $_POST['username'];
	// Find the record of user called 'username'.
	$userQuery = $database->select(
		$usertab,
		['id'],
		['name' => $userName]
	);
	if(count($userQuery) > 0)
	{
		// Extract user ID from query result.
		$userId = $userQuery[0]['id'];
		// Delete connection with found ID (ID of the user logging-out).
		$database->delete($conntab, ['user_id' => $userId]);
	}
?>
