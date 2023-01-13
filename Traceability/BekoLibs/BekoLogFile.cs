using System;

//��� ��������� �����-������
using System.IO;

//��� ������������ ������������ ������������ �����
using System.Reflection;


namespace Traceability
{
    public class BekoLogFile
    {
        //����
        private DirectoryInfo dir;
        private FileInfo outFile;
        private StreamWriter swLog; //����� ��� ������ �����
        private int lastDay; //� ������� ����� ���������� � ����� ����
        private string directoryName; //��� ����������� ������� ��������
        private string prefixLog = ""; //��� ������� �������� � ������ ������ � ����� ����� �����

        //��������
        public bool CanWrite { get { return swLog != null; } } //��������� � false, ���� �� ����� ���� �� ������
        public BekoLogFile(string Directory = @"\logs", string Prefix = "")
        {
            directoryName = Directory;
            prefixLog = Prefix;
            InitNewFile();
        }

        //�������
        public delegate void delegHandlerStr(string msg);
        public event delegHandlerStr LogEvent;
        public event delegHandlerStr LogEventWithPrefix;


        private void InitNewFile()
        {
            //����� ������ ����������� ������� ����� (���������� �������� ��� Service � Windows Mobile)
            if (dir == null)
            {
                string appName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
                string folderName = appName.Substring(0, appName.LastIndexOf(@"\"));
                dir = new DirectoryInfo(folderName + directoryName);
            }


            try //���������, ���� ��� ������
            {
                if (swLog != null) swLog.Close();
            }
            catch { }
            try //����� ���� �������� �� ����������� ������
            {
                lastDay = DateTime.Now.Day;
                if (!dir.Exists) dir.Create(); //������� ����� �����, ���� � ��� ���
                //����� ������� ����� � ������ �� ������� ������ 30-�� ����
                FileInfo[] logFiles = dir.GetFiles(); //�������� ������ ������
                foreach (FileInfo f in logFiles)
                {
                    //������������� ����� �� ���� ��������, ���� ��������� ������, ���� ���������� ������ (� ���� ������ �������)
                    if ((DateTime.Now - f.LastWriteTime).TotalDays > 30) f.Delete();
                }

                string fileName = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("D2") + "-" + DateTime.Now.Day.ToString("D2") + ".txt";
                outFile = new FileInfo(dir.FullName + "\\" + fileName);
                //���� ���� ����, �� ��������� ���, ���� ��� - �������. ��������� ��� ��������.
                swLog = outFile.AppendText();
                swLog.AutoFlush = true; //�������������� ������������ �� ������ � ����, ������ ������� ������
            }
            catch { }
        }

        public void WriteLog(string msg)
        {            
            if (lastDay != DateTime.Now.Day) InitNewFile();
            string msg1 = DateTime.Now.ToString("dd.MM HH:mm:ss.fff") + " -> " + msg;
            string msg2 = DateTime.Now.ToString("dd.MM HH:mm:ss.fff") + " -> " + prefixLog + msg;
            if (swLog != null) swLog.WriteLine(msg1);
            if (LogEvent != null) LogEvent.Invoke(msg1);
            if (LogEventWithPrefix != null) LogEventWithPrefix.Invoke(msg2);
        }

        ~BekoLogFile() 
        {
            try
            {
                swLog?.Close();
            }
            catch { }
        }

    }
}