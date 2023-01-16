echo OFF
set MYSQL_PATH=%1
set USER=%2
set PASSWORD=%3
set DB=%4
set OUTPUT_FILE=%5

%MYSQL_PATH%\mysqldump --user=%USER% --password=%PASSWORD% --verbose --add-drop-table --skip-tz-utc --databases %DB% > %OUTPUT_FILE%
