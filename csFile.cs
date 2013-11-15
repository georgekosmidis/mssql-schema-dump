using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MSSQLDump {
    class csFile {
        public static string CreateFolder( string path, string folder ) {
            path = System.IO.Path.Combine( path, folder );
            if (!Directory.Exists( path ))
                System.IO.Directory.CreateDirectory( path );
            return path;
        }
        public static void writeFile( string filePath, string c, bool append ) {
            string s = ReadFile( filePath );
            TextWriter tw = new StreamWriter( filePath );
            try {
                if (append)
                    tw.WriteLine( s + c );
                else
                    tw.WriteLine( c );
            }
            finally {
                tw.Close();
            }
        }

        public static string ReadFile( string filePath ) {
            byte[] buffer;
            try {
                FileStream fileStream = new FileStream( filePath, FileMode.Open, FileAccess.Read );
                try {
                    int length = (int)fileStream.Length;  // get file length
                    buffer = new byte[length];            // create buffer
                    int count;                            // actual number of bytes read
                    int sum = 0;                          // total number of bytes read

                    // read until Read method returns 0 (end of the stream has been reached)
                    while ((count = fileStream.Read( buffer, sum, length - sum )) > 0)
                        sum += count;  // sum is a buffer offset for next reading
                }
                finally {
                    fileStream.Close();
                }
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                return enc.GetString( buffer );
            }
            catch {
                return "";
            }
        }
    }
}
