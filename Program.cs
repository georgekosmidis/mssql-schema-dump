using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace MSSQLDump {
    class Program {
        private static string HOST = "(local)";
        private static string USER = "sa";
        private static string PASS = "sa";
        private static string SavePath = @"C:\_SQL_SCHEMA_DUMP\";
        private static bool CleanDir = false;
        private static bool ExportStatistics = false;
        private static bool UseDAC = false;
        private static List<string> DBs = new List<string>();
        private static _DB DB = new _DB( HOST, USER, PASS );

        static void Main( string[] args ) {
            if ( args.Count() == 0 ) {
                WriteHelp();
                return;
            }
            if ( !ReadArguments( args ) )
                return;
            //Clean Dir
            if ( CleanDir && Directory.Exists( SavePath + Path.DirectorySeparatorChar + pathify( HOST ) ) ) {
                Console.WriteLine( "Cleaning Directory '" + SavePath + Path.DirectorySeparatorChar + pathify( HOST ) + "'" );
                var b = DeleteDirectory( SavePath + Path.DirectorySeparatorChar + pathify( HOST ) );
                if ( !b )
                    return;
                Console.Clear();
            }

            //Use DAC
            if ( UseDAC ) {
                Console.WriteLine( "Trying to enable DAC..." );
                try {
                    DB.TryEnableDAC();
                }
                catch {
                    Console.WriteLine( "ERROR!" );
                    Console.WriteLine( "DAC cannot be enabled, retry without the option but encrypted objects will be omitted!" );
                    return;
                }

                DB = new _DB( "ADMIN:" + HOST, USER, PASS );
                Console.Clear();
            }
            var cn = new SqlConnection( "packet size=4096;user id=" + USER + ";Password=" + PASS + ";data source=" + HOST + ";persist security info=True;initial catalog=master;" );
            try {
                cn.Open();
                cn.Close();
            }
            catch ( Exception ex ) {
                Console.Clear();
                Console.WriteLine( "ERROR!" );
                Console.WriteLine( ex.Message );
                Console.WriteLine( "(Server:" + HOST + ", User:" + USER + ", PASS: " + PASS.Substring( 0, 1 ) + (new String( '*', PASS.Length - 2 )) + PASS.Substring( PASS.Count() - 1, 1 ) + ")" );
                Console.ReadKey();
                return;
            }
            var sc = new ServerConnection( cn );
            Server server = new Server( sc );

            //START
            SavePath = csFile.CreateFolder( SavePath, pathify( HOST ) );

            //SERVER
            var filePath = PrepareSqlFile( "*", "", "SERVER", HOST, SavePath, "" );
            WriteSQLInner<Server>( "*", "", "SERVER", HOST, filePath, server, ScriptOption.DriAll );

            foreach ( var db in server.Databases.Cast<Database>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                if ( db.IsSystemObject )
                    continue;
                if ( DBs.Count() > 0 && !DBs.Contains( db.Name.ToLower() ) )
                    continue;
                var dbPath = csFile.CreateFolder( SavePath, pathify( db.Name ) );

                Console.WriteLine( "=================================================" );
                Console.WriteLine( "DB: " + db.Name );
                Console.WriteLine( "-------------------------------------------------" );

                //var schema = "dbo";
                var filename = "";
                var objPath = "";
                //System.Collections.Specialized.StringCollection cs = new System.Collections.Specialized.StringCollection();
                //////////////////////////////////////////////////////////////////////////
                //DB
                var currentPath = dbPath;
                filePath = PrepareSqlFile( db.Name, "", "DB", db.Name, currentPath, "" );
                WriteSQLInner<Database>( db.Name, "", "DB", db.Name, filePath, db, ScriptOption.Default );

                //////////////////////////////////////////////////////////////////////////
                //SCHEMA
                foreach ( var schema2 in db.Schemas.Cast<Schema>().AsQueryable() ) {
                    filePath = PrepareSqlFile( db.Name, "", "Schema", schema2.Name, currentPath, "" );
                    WriteSQLInner<Schema>( db.Name, "", "Schema", schema2.Name, filePath, schema2, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB USER TYPES
                currentPath = csFile.CreateFolder( dbPath, pathify( "UTYPE" ) );
                foreach ( UserDefinedType o in db.UserDefinedTypes ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "UTYPE", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedType>( db.Name, o.Schema, "UTYPE", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB TRIGGERS
                currentPath = csFile.CreateFolder( dbPath, pathify( "TRIGGER" ) );
                foreach ( DatabaseDdlTrigger o in db.Triggers.Cast<DatabaseDdlTrigger>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "TRIGGER", o.Name, currentPath, "" );
                    WriteSQLInner<DatabaseDdlTrigger>( db.Name, "dbo", "TRIGGER", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB USER TABLE TYPES
                currentPath = csFile.CreateFolder( dbPath, pathify( "TTYPES" ) );
                foreach ( UserDefinedTableType o in db.UserDefinedTableTypes ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "TTYPES", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedTableType>( db.Name, o.Schema, "TTYPES", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB FULLTEXT CATALOGS
                currentPath = csFile.CreateFolder( dbPath, pathify( "FTC" ) );
                foreach ( FullTextCatalog o in db.FullTextCatalogs ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "FTC", o.Name, currentPath, "" );
                    WriteSQLInner<FullTextCatalog>( db.Name, "dbo", "FTC", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB FULLTEXT STOPLISTS
                currentPath = csFile.CreateFolder( dbPath, pathify( "FTL" ) );
                foreach ( FullTextStopList o in db.FullTextStopLists ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "FTL", o.Name, currentPath, "" );
                    WriteSQLInner<FullTextStopList>( db.Name, "dbo", "FTL", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //STORED PROCEDURES
                currentPath = csFile.CreateFolder( dbPath, pathify( "PROCEDURE" ) );
                foreach ( StoredProcedure o in db.StoredProcedures.Cast<StoredProcedure>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "PROCEDURE", o.Name, currentPath, "" );
                    WriteSQLInner<StoredProcedure>( db.Name, o.Schema, "PROCEDURE", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //FUNCTIONS
                currentPath = csFile.CreateFolder( dbPath, pathify( "FUNCTION" ) );
                foreach ( UserDefinedFunction o in db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where( oo => oo.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "FUNCTION", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedFunction>( db.Name, o.Schema, "FUNCTION", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //TABLE
                foreach ( Table o in db.Tables.Cast<Table>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {

                    currentPath = csFile.CreateFolder( dbPath, pathify( "TABLE" ) );
                    filePath = PrepareSqlFile( db.Name, o.Schema, "TABLE", o.Name, currentPath, "" );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.Default );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.Indexes );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.DriAll );


                    //////////////////////////////////////////////////////////////////////////
                    //TABLE TRIGGERS
                    currentPath = csFile.CreateFolder( dbPath, pathify( "TRIGGER" ) );
                    foreach ( Trigger ot in o.Triggers.Cast<Trigger>().AsQueryable().Where( oo => oo.IsSystemObject == false ) ) {
                        filePath = PrepareSqlFile( db.Name, o.Schema, "TRIGGER", ot.Name, currentPath, "TABLE_" + o.Name );
                        WriteSQLInner<Trigger>( db.Name, o.Schema, "TRIGGER", ot.Name, filePath, ot, ScriptOption.Default );
                    }

                    //////////////////////////////////////////////////////////////////////////
                    //TABLE STATISTICS
                    if ( ExportStatistics ) {
                        currentPath = csFile.CreateFolder( dbPath, pathify( "STATISTIC" ) );
                        foreach ( Statistic ot in o.Statistics.Cast<Statistic>().AsQueryable() ) {
                            filePath = PrepareSqlFile( db.Name, o.Schema, "STATISTIC", ot.Name, currentPath, "TABLE_" + o.Name );
                            WriteSQLInner<Statistic>( db.Name, o.Schema, "STATISTIC", ot.Name, filePath, ot, ScriptOption.OptimizerData );
                        }
                    }
                }

                //////////////////////////////////////////////////////////////////////////
                //VIEWS
                foreach ( View o in db.Views.Cast<View>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {

                    currentPath = csFile.CreateFolder( dbPath, pathify( "VIEW" ) );
                    filePath = PrepareSqlFile( db.Name, o.Schema, "VIEW", o.Name, currentPath, "" );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.Default );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.Indexes );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.DriAllConstraints );

                    //////////////////////////////////////////////////////////////////////////
                    //VIEW TRIGGERS
                    currentPath = csFile.CreateFolder( dbPath, pathify( "TRIGGER" ) );
                    foreach ( Trigger ot in o.Triggers.Cast<Trigger>().AsQueryable().Where( oo => oo.IsSystemObject == false ) ) {
                        filePath = PrepareSqlFile( db.Name, o.Schema, "TRIGGER", ot.Name, currentPath, "VIEW_" + o.Name );
                        WriteSQLInner<Trigger>( db.Name, o.Schema, "TRIGGER", ot.Name, filePath, ot, ScriptOption.Default );
                    }

                    //////////////////////////////////////////////////////////////////////////
                    //VIEW STATISTICS
                    if ( ExportStatistics ) {
                        currentPath = csFile.CreateFolder( dbPath, pathify( "STATISTIC" ) );
                        foreach ( Statistic ot in o.Statistics.Cast<Statistic>().AsQueryable() ) {
                            filePath = PrepareSqlFile( db.Name, o.Schema, "STATISTIC", ot.Name, currentPath, "VIEW_" + o.Name );
                            WriteSQLInner<Statistic>( db.Name, o.Schema, "STATISTIC", ot.Name, filePath, ot, ScriptOption.OptimizerData );
                        }
                    }
                }

            }

            if ( UseDAC )
                DB.TryDisableDAC();

            Console.WriteLine( Environment.NewLine );
            Console.WriteLine( "Done!" );
            Console.ReadKey();

        }

        #region Helpers
        private static void WriteHelp() {
            Console.WriteLine( "***MS SQL schema dump v1 Beta (http://github.com/georgekosmidis/mssql-schema-dump)***" );
            Console.WriteLine( "Exports MS SQL Server database schema, that includes:" );
            Console.WriteLine( "DB" );
            Console.WriteLine( "  Schema, User Types, User Table Types, Triggers, Full Text Catalogues," );
            Console.WriteLine( "  Full Text StopLists, Stored Procedures, Functions" );
            Console.WriteLine( "DB.Tables" );
            Console.WriteLine( "  Schema, Triggers, Indexes, DRI, Statistics" );
            Console.WriteLine( "DB.Views" );
            Console.WriteLine( "  Schema, Triggers, Indexes, DRI, Statistics" );
            Console.WriteLine( "Pass a junk parameter to start with default values shown below!" );
            Console.WriteLine( "" );
            Console.WriteLine( "Usage: mssqldump -h data-source-host -u username -p password" );
            Console.WriteLine( "       mssqldump [-d path/for/files] [-c] [-s] [-a] [-b DB1[,DB2[,DB3]]]" );
            Console.WriteLine( "" );
            Console.WriteLine( "Options:" );
            Console.WriteLine( "     -h : SQL server host, defaults to (local)" );
            Console.WriteLine( "     -u : username, defaults to sa" );
            Console.WriteLine( "     -p : password, defaults to sa" );
            Console.WriteLine( "     -d : Local path for saved files, defaults to C:\\_SQL_SCHEMA_DUMP\\" );
            Console.WriteLine( "     -c : Delete all files and folders from local path, defaults to false" );
            Console.WriteLine( "     -s : Also export statistics, defaults to false" );
            Console.WriteLine( "     -a : Use DAC to try decrypt encrypted objects, defaults to false" );
            Console.WriteLine( "     -b : Comma separated value of databases to export, defaults to empty string" );
            Console.ReadKey();
        }
        private static bool ReadArguments( string[] args ) {
            try {
                for ( int i = 0; i < args.Count(); i++ ) {
                    switch ( args[i] ) {
                        case "-h":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                HOST = args[i + 1];
                            i++;
                            continue;
                        case "-u":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                USER = args[i + 1];
                            i++;
                            continue;
                        case "-p":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                PASS = args[i + 1];
                            i++;
                            continue;
                        case "-d":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                SavePath = args[i + 1];
                            i++;
                            continue;
                        case "-c":
                            CleanDir = true;
                            continue;
                        case "-s":
                            ExportStatistics = true;
                            continue;
                        case "-a":
                            UseDAC = true;
                            continue;
                        case "-b":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" ) {
                                DBs = args[i + 1].Split( ',' ).ToList<string>().ConvertAll( d => d.ToLower() );
                            }
                            continue;
                    }
                }
            }
            catch {
                Console.Clear();
                Console.WriteLine( "ERROR!" );
                Console.WriteLine( "You have an error in your arguments passed." );
                Console.WriteLine( "Press any key to read help" );
                Console.ReadKey();
                Console.Clear();
                WriteHelp();
                return false;
            }
            return true;
        }
        private static bool WriteSQLInner<T>( string db, string schema, string objType, string objName, string filePath, T o, ScriptingOptions so ) where T : SqlSmoObject {
            if ( schema == "" )
                schema = "dbo";
            if ( db == "*" )
                Console.WriteLine( objType + ": " + objName );
            else
                Console.WriteLine( objType + ": " + db + "." + schema + "." + objName + " (" + so.ToString() + ")" );


            System.Collections.Specialized.StringCollection cs = new System.Collections.Specialized.StringCollection();
            try {
                cs = (o as dynamic).Script( so );
            }
            catch ( Exception ex ) {
                if ( UseDAC ) {
                    try {
                        DB.ChangeDB( db );
                        var dt = DB.GetDecryptedObject( objName, objType );
                        cs.Clear();
                        cs.Add( dt.Rows[0]["script"].ToString() );
                    }
                    catch ( Exception ex2 ) {
                        Console.WriteLine( ex2.Message );
                        return false;
                    }
                }
                else {
                    Console.WriteLine( ex.Message );
                    return false;
                }
            }

            if ( cs != null ) {
                var ts = "";
                foreach ( var s in cs )
                    ts += s + Environment.NewLine;
                if ( !String.IsNullOrWhiteSpace( ts.Trim() ) ) {
                    if ( !File.Exists( filePath ) )
                        csFile.writeFile( filePath, sqlComments( db, schema, objType, objName ), true );
                    csFile.writeFile( filePath, ts + ";" + Environment.NewLine, true );
                }
            }

            return true;
        }
        private static string PrepareSqlFile( string db, string schema, string objType, string objName, string objPath, string filePrefix ) {
            filePrefix = filePrefix != "" ? filePrefix + "_" : filePrefix;
            var filePath = objPath + Path.DirectorySeparatorChar + pathify( filePrefix + objType + "_" + schema + "_" + objName ) + ".sql";

            return filePath;
        }
        private static string pathify( string s ) {
            foreach ( var c in System.IO.Path.GetInvalidFileNameChars() )
                s = s.Replace( c, '_' );
            return s;
        }
        private static string sqlComments( string db, string schema, string type, string name ) {
            var s = "--****************************************************" + Environment.NewLine;
            s += "--MS SQL schema dump v1 Beta" + Environment.NewLine;
            s += "--Latest Version on GitHub: http://github.com/georgekosmidis/mssql-schema-dump" + Environment.NewLine;
            s += "--George Kosmidis <www.georgekosmidis.com>" + Environment.NewLine;
            s += "-------------------------------------------------------" + Environment.NewLine;
            s += "--DB: " + db + Environment.NewLine;
            s += "--SCHEMA: " + schema + Environment.NewLine;
            s += "--" + type + ": " + name + Environment.NewLine;
            s += "--" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + Environment.NewLine;
            s += "--****************************************************" + Environment.NewLine + Environment.NewLine;
            return s;
        }
        private static bool DeleteDirectory( string target_dir ) {
            string[] files = Directory.GetFiles( target_dir );
            string[] dirs = Directory.GetDirectories( target_dir );

            foreach ( string file in files ) {
                try {
                    File.SetAttributes( file, FileAttributes.Normal );
                    File.Delete( file );
                }
                catch {
                    Console.WriteLine( "ERROR!" );
                    Console.WriteLine( "File '" + file + "' is locked." );
                    Console.WriteLine( "R: Retry, any other key to exit" );

                    var k = Console.ReadKey();
                    if ( k.Key.ToString().ToLower() == "r" )
                        return DeleteDirectory( target_dir );

                    return false;
                }
            }
            Thread.Sleep( 200 );

            foreach ( string dir in dirs ) {
                var b = DeleteDirectory( dir );
            }

            try {
                Directory.Delete( target_dir, false );
            }
            catch {
                Console.WriteLine( "ERROR!" );
                Console.WriteLine( "Directory '" + target_dir + "' is locked." );
                Console.WriteLine( "R: Retry, any other key to exit" );

                var k = Console.ReadKey();
                if ( k.Key.ToString().ToLower() == "r" )
                    return DeleteDirectory( target_dir );

                return false;
            }
            return true;
        }

        #endregion
    }
}
