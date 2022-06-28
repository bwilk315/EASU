<?php
    require 'local/db-access.php';
    // Variable used for determining (on finish) if the structure is valid.
    $good                   = 1;
    if(file_exists($structureFile)) {
        // Decode structure file from JSON format to associative array.
        $file               = fopen($structureFile, 'r');
        $content            = fread($file, filesize($structureFile));
        $structure          = json_decode($content, associative: true);
        // Check files existence, for now omit 'local' catalog.
        foreach($structure as $file => $version) {
            if($file == 'local') {
                continue;
            } elseif(file_exists($file)) {
                continue;
            } else {
                // If file required in root directory is missing report error.
                $good       = 0;
                break;
            }
        }
        // If all required files in the root directory exists, check 'local' folder.
        if($good == 1) {
            foreach($structure['local'] as $file => $version) {
                if(file_exists("local/$file")) {
                    continue;
                } else {
                    // If file required in 'local' directory is missing report error.
                    $good   = 0;
                    break;
                }
            }
        }
    } else {
        // If structure file doesn't exist report error.
        $good               = 0;
    }
    echo $good;
?>
