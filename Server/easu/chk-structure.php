<?php
    /*
    CHK-STRUCT.PHP:
    Returns true if the server has proper structure.
    */
    $good = 1;
    if(file_exists("local/structure.json")) {
        // Check if local structure file is the same as original, by comparing it
        // with file located in trusted server. If not, replace it ...
        $struFile = fopen("local/structure.json", "r");
        $stru = json_decode(fread($struFile, filesize("local/structure.json")));
        foreach($stru as $file => $desc) {
            if($file == "local") {
                continue;
            } elseif(file_exists($file)) {
                continue;
            } else {
                $good = 0;
                break;
            }
        }
        if($good == 1) {
            foreach($stru->local as $file => $desc) {
                if(file_exists("local/" . $file)) {
                    continue;
                } else {
                    $good = 0;
                    break;
                }
            }
        }
    } else {
        $good = 0;
    }
    echo $good;
?>
