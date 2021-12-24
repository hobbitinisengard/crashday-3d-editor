using System.IO;
using UnityEngine;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
public static class PackageManager
{
    public static string[] DefaultArchives = { "dat000.cpk", "dat002.cpk", "dat005.cpk", "dat006.cpk" };
    private static string[] TargetFileTypes = { "cat", "cfl", "dds", "p3d", "tga" };

    public static void LoadDefaultCPKs()
    {
        foreach (string s in DefaultArchives)
            LoadCPK(IO.GetCrashdayPath() + "\\data\\" + s);
    }

    public static void LoadCPK(string filePath, string Tileset_id = null)
    {
        if (!File.Exists(filePath))
            return;

        // Read file
        FileStream fs;
        try
        {
            fs = new FileStream(filePath, FileMode.Open);
        }
        catch
        {
            Debug.Log("GameData file open exception: " + filePath);
            return;
        }

        string path = IO.GetCrashdayPath();

        if (Tileset_id != null)
            path += "\\moddata\\" + Tileset_id + "\\";
        else
            path += "\\data\\";

        Directory.CreateDirectory(path);

        try
        {
            ZipFile zf = new ZipFile(fs);
            //checking zips for integrity takes way too much time
            //if (false/*zf.TestArchive(true) == false*/)
            //{
            //  Debug.Log("Zip file failed integrity check!");
            //  zf.IsStreamOwner = false;
            //  zf.Close();
            //  fs.Close();
            //}
            //else
            
            foreach (ZipEntry zipEntry in zf)
            {
                // Ignore directories
                if (!zipEntry.IsFile)
                    continue;

                string entryFileName = zipEntry.Name;

                //dont load already unpacked files
                if (File.Exists(path + entryFileName))
                {
                //Debug.Log("Assuming " + path + " is unpacked");
                    break;
                }

                // Skip unneeded file types
                bool flag = true;
                foreach (string ext in TargetFileTypes)
                {
                    if (entryFileName.EndsWith(ext))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                    continue;

                byte[] buffer = new byte[4096];     // 4K is optimum
                Stream zipStream = zf.GetInputStream(zipEntry);

                string fullZipToPath = path + entryFileName;

                if (!Directory.Exists(path + Path.GetDirectoryName(entryFileName)))
                    Directory.CreateDirectory(path + Path.GetDirectoryName(entryFileName));

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                using (FileStream streamWriter = File.Create(fullZipToPath))
                {
                    StreamUtils.Copy(zipStream, streamWriter, buffer);
                }
            }

            zf.IsStreamOwner = false;
            zf.Close();
            fs.Close();
        }
        catch
        {
            Debug.Log("Zip file error!");
        }
    }
}
