<?php
	/*
		SYNC.PHP:
		Outputs data essential for client-server synchronization.
	*/
	require 'local/db-access.php';
	
	echo constant('UPDATE_INTERVAL');
?>