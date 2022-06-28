<?php
    require '../local/db-access.php';
    $sessionQuery = $database->select(
        $tabSessions,                   // TABLE
        ['user_id', 'last_activity']    // COLUMNS
    );
    $packed = '';
    if($sessionQuery) {
        foreach($sessionQuery as $session) {
            $userId = $session['user_id'];
            $userQuery = $database->select(
                $tabUsers,              // TABLE
                ['id', 'name'],         // COLUMNS
                ['id[=]' => $userId]    // WHERE
            );
            if($userQuery) {
                $userData = $userQuery[0];
                $userName = $userData['name'];
                $userTime = $session['last_activity'];
                $packed .= "&$userName=$userTime";
            }
        }
        echo $packed;
    }
?>
