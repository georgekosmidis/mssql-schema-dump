<h1>MS SQL schema dump v1 Beta</h1><br />
Exports MS SQL Server database schema, that includes:<br />
<br />
<b>DB</b><br />
  Schema, User Types, User Table Types, Triggers, Full Text Catalogues,<br />
  Full Text StopLists, Stored Procedures, Functions<br />
<b>DB.Tables</b><br />
  Schema, Triggers, Indexes, DRI, Statistics<br />
<b>DB.Views</b><br />
  Schema, Triggers, Indexes, DRI, Statistics<br />
<br />
<i>Pass a junk parameter to start with default values shown below!</i><br />
<br />
<b>Usage:</b>
<code>
mssqldump -h data-source-host -u username -p password 
       mssqldump [-d path/for/files] [-c] [-s] [-a] [-b DB1[,DB2[,DB3]]]<br />
</code>
<br />
Options:<br />

<code>-h : SQL server host, defaults to (local)</code><br />
<code>-u : username, defaults to sa</code><br />
<code>-p : password, defaults to sa</code><br />
<code>-d : Local path for saved files, defaults to C:\\\_SQL_SCHEMA_DUMP</code><br />
<code>-c : Delete all files and folders from local path, defaults to false</code><br />
<code>-s : Also export statistics, defaults to false</code><br />
<code>-a : Use DAC to try decrypt encrypted objects, defaults to false</code><br />
<code>-b : Comma separated value of databases to export, defaults to empty string</code>
