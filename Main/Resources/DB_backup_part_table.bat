
set MYSQL_PATH=%1
set USER=%2
set PASSWORD=%3
set DB=%4
set TABLE_NAME=%5
set WHERE_CONDITION=%6
set OUTPUT_FILE=%7

::set WHERE_CONDITION=%5
::set OUTPUT_FILE=%6

%MYSQL_PATH%\mysqldump --user=%USER% --password=%PASSWORD% --verbose --no-create-info --skip-disable-keys --skip-tz-utc %DB% %TABLE_NAME% --where=%WHERE_CONDITION% > %OUTPUT_FILE%
