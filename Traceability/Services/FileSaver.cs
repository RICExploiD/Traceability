using System.IO;

namespace Traceability.Logic
{
    static class FileSaver
    {
        static public void SaveStringToTmpFile(string MTK) { using (var stream = new StreamWriter("./LastMTK.tmp", false)) stream.Write(MTK); }
        static public string ReadStringFromFile() { using (var stream = new StreamReader("./LastMTK.tmp")) return stream.ReadLine(); }
        static public string ReadStringFromFileSafe() { try { return ReadStringFromFile(); } catch { return string.Empty; } }
    }
}