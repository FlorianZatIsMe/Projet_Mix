
set MYSQL_PATH=%1
set USER=%2
set PASSWORD=%3
set DB=%4
set INPUT_FILE=%5

%MYSQL_PATH%\mysql --user=%USER% --password=%PASSWORD% --verbose %DB% < %INPUT_FILE%