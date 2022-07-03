<?php
    require __DIR__ . '/../db-access.php';

    $currentTime = time();
    $sessionQuery = $database->select(
        $tabSessions,
        ['user_id', 'last_activity']
    );
    foreach($sessionQuery as $session) {
        $userLastActivity 	= $session['last_activity'];
        $userId 			= $session['user_id'];
        if($userLastActivity + $updateInterval < $currentTime) {
            $database->delete(
                $tabSessions,
                ['user_id' => $userId]
            );
        }
    }
?>
