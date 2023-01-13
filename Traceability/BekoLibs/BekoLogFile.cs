using System;

//для файлового ввода-вывода
using System.IO;

//для опеределения расположения исполняемого файла
using System.Reflection;


namespace Traceability
{
    public class BekoLogFile
    {
        //поля
        private DirectoryInfo dir;
        private FileInfo outFile;
        private StreamWriter swLog; //поток для записи логов
        private int lastDay; //в полночь будем сбрасывать в новый файл
        private string directoryName; //для возможности задания каталога
        private string prefixLog = ""; //для задания префикса в случае вывода в общий поток логов

        //свойства
        public bool CanWrite { get { return swLog != null; } } //сработает в false, если не будет прав на запись
        public BekoLogFile(string Directory = @"\logs", string Prefix = "")
        {
            directoryName = Directory;
            prefixLog = Prefix;
            InitNewFile();
        }

        //события
        public delegate void delegHandlerStr(string msg);
        public event delegHandlerStr LogEvent;
        public event delegHandlerStr LogEventWithPrefix;


        private void InitNewFile()
        {
            //более точное определение текущей папки (изначально заплатка для Service и Windows Mobile)
            if (dir == null)
            {
                string appName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
                string folderName = appName.Substring(0, appName.LastIndexOf(@"\"));
                dir = new DirectoryInfo(folderName + directoryName);
            }


            try //закрываем, если был открыт
            {
                if (swLog != null) swLog.Close();
            }
            catch { }
            try //далее идут операции по регистрации файлов
            {
                lastDay = DateTime.Now.Day;
                if (!dir.Exists) dir.Create(); //создать папку логов, если её ещё нет
                //далее очистка папки с логами от событий старше 30-ти дней
                FileInfo[] logFiles = dir.GetFiles(); //получаем список файлов
                foreach (FileInfo f in logFiles)
                {
                    //анализировать можно по дате создания, дате последней записи, дате последнего чтения (я взял второй вариант)
                    if ((DateTime.Now - f.LastWriteTime).TotalDays > 30) f.Delete();
                }

                string fileName = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("D2") + "-" + DateTime.Now.Day.ToString("D2") + ".txt";
                outFile = new FileInfo(dir.FullName + "\\" + fileName);
                //если файл есть, то открываем его, если нет - создаем. Открываем для дозаписи.
                swLog = outFile.AppendText();
                swLog.AutoFlush = true; //автоматическое выталкивание из буфера в файл, каждую команду записи
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