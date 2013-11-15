***MS SQL schema dump v1***
Exports MS SQL Server database schema, that includes:

DB
  Schema, User Types, User Table Types, Triggers, Full Text Catalogues,
  Full Text StopLists, Stored Procedures, Functions
DB.Tables
  Schema, Triggers, Indexes, DRI, Statistics
DB.Views
  Schema, Triggers, Indexes, DRI, Statistics

  
*Pass a junk parameter to start with default values shown below!


Usage: mssqldump -h data-source-host -u username -p password
       mssqldump [-d path/for/files] [-c] [-s] [-a] [-b DB1[,DB2[,DB3]]]


Options:
     -h : SQL server host, defaults to (local)
     -u : username, defaults to sa
     -p : password, defaults to sa
     -d : Local path for saved files, defaults to C:\\_SQL_SCHEMA_DUMP\\
     -c : Delete all files and folders from local path, defaults to false
     -s : Also export statistics, defaults to false
     -a : Use DAC to try decrypt encrypted objects, defaults to false
     -b : Comma separated value of databases to export, defaults to empty string
