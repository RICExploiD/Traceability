using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;


namespace Traceability
{
    class BekoSQL
    {
        //поля
        readonly string appName = "NewAppName";
        readonly bool cDBIsReadonly = false;

        private SqlConnection SQLconnection; //наше соединение с БД
        private bool lastConnectionState = false;
        private int countError = 0; //после 5ти ошибок будем гасить соединение
        private Timer SQLTimerCheckConn;
        private Timer SQLTimerInsertQueue;
        private List<string> SQLInsertQueue = new List<string>(); //очередь на запись данных в БД

        //попытаемся предотвратить стопор приложения по ошибочному запросу в БД
        private string curSQLQueryErrorStr = ""; //здесь храним последний неудачный запрос
        private int countCurSQLQueryError = 0; //здесь храним количество провалившихся попыток для данного запроса
        private const int countErrorsToDropSQLQuery = 7; //количество ошибок выполнения sql запроса, после которого этот запрос будет признан невозможным к выполнению\
        const string prefixEmail = "email_SQLError_";
        private List<string> emails = new List<string>(); //здксь будем хранить список кому отправляем
        private string LastSussessQuery = ""; //здесь будем хранить последний выполненный запрос для анализа ошибок выполнения

        //свойства
        public int CountOf_SQLInsertQueue { get { return SQLInsertQueue.Count; } }

        //события
        public delegate void delegHandlerStr(string msg);
        public delegate void delegHandlerBool(bool isConnectedNow);
        public delegate void delegHandlerInt(int count);
        public delegate void delegHandlerNoParam();

        public event delegHandlerStr LogEvent; //событие куда выбрасываем логи
        public event delegHandlerStr LogNewSql; //сюда выбрасываем sql запросы
        public event delegHandlerBool OnConnectionChange; //рабатывает при потере или восстановлении связи
        public event delegHandlerNoParam SuccessWrite;
        public event delegHandlerInt OnChangeCountInsertQueue;

        public BekoSQL(string connectionString, bool IsReadonly = false, string AppName = "")
        {
            //для целей отладки
            cDBIsReadonly = IsReadonly;
            //если не задано имя приложения из основной программы, то возьмем имя процесса
            if (string.IsNullOrEmpty(AppName)) appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            else appName = AppName;

            SQLconnection = new SqlConnection(connectionString);
            SQLTimerCheckConn = new Timer(new TimerCallback(SQLTimerConnection_Tick), null, 0000, 4000);
            //раскомментировать для записи в БД
            SQLTimerInsertQueue = new Timer(new TimerCallback(SQLTimerInsertQueue_Tick), null, 10000, 3000);

            //подгрузим из конфига тех, кому будем отправлять отчет о невыполнении sql запроса
            emails = ReadEmailListFromConfig(prefixEmail);
        }

        ~BekoSQL()
        {
            try
            {
                SQLconnection.Close();
            }
            catch { }
        }

        private void SQLTimerConnection_Tick(object state)
        {
            if (countError > 5) //принудительно перезапустить соединение после 5 ошибок
            {
                try
                {
                    SQLconnection.Close();
                }
                catch { }
                countError = 0;
            }
            if (SQLconnection.State != ConnectionState.Open)
            {
                try
                {
                    SQLconnection.Open();
                }
                catch (Exception ex) { LogEvent?.Invoke("Ошибка подключения к БД: " + ex.Message); }
            }

            if (SQLconnection.State == ConnectionState.Open && lastConnectionState == false)
            {
                LogEvent?.Invoke("Соединение с БД успешно установлено.");
                LogEvent?.Invoke("Отчет о сбое выполнения SQL будет отправлен на " + emails.Count + " email.");
                lastConnectionState = true;
                OnConnectionChange?.Invoke(true);
            }

            if (SQLconnection.State != ConnectionState.Open && lastConnectionState == true)
            {
                LogEvent?.Invoke("Соединение с БД потеряно.");
                lastConnectionState = false;
                OnConnectionChange?.Invoke(false);
            }
        }

        public void InsertToBuffer(string cmdSQL)
        {
            if (SQLInsertQueue.Count < 9999)
            {
                SQLInsertQueue.Add(cmdSQL);
                LogNewSql?.Invoke(cmdSQL);
                OnChangeCountInsertQueue?.Invoke(SQLInsertQueue.Count);
            }
            else
            {
                LogEvent?.Invoke("Очередь на запись в SQL превысила 9999 записей. Потерянный запрос:\n" + cmdSQL);
            }
        }

        private void SQLTimerInsertQueue_Tick(object state)
        {
            if (!cDBIsReadonly)
            {
                lock (this) //попытка не допустить повторного выполнения одинаковых запросов к БД
                {
                    try
                    {
                        if (SQLconnection.State == ConnectionState.Open)
                            while (SQLInsertQueue.Count > 0)
                                using (SqlCommand command = new SqlCommand(SQLInsertQueue[0], SQLconnection))
                                {
                                    command.ExecuteNonQuery();
                                    LastSussessQuery = SQLInsertQueue[0];
                                    SQLInsertQueue.RemoveAt(0);
                                    OnChangeCountInsertQueue?.Invoke(SQLInsertQueue.Count);
                                    SuccessWrite?.Invoke();
                                }
                    }
                    catch (Exception ex)
                    {
                        countError++;
                        LogEvent.Invoke("Ошибка записи в БД : " + ex.Message);
                        //проверка на то, что данный запрос вообще не выполним
                        if (SQLInsertQueue.Count > 0)
                            if (SQLInsertQueue.Count > 0 && SQLInsertQueue[0] != curSQLQueryErrorStr)
                            {
                                //уже больше одного раза не можем выполнить этот запрос
                                if (countCurSQLQueryError++ > countErrorsToDropSQLQuery)
                                {
                                    //если мы превысили количество попыток для выполнения запроса
                                    //сами (в этом файле) отправим email
                                    SendEmailOnDropSQLQuery(SQLInsertQueue[0] + "<br>" + ex.Message + "<br> Последний выполненный запрос: " + LastSussessQuery);
                                    SQLInsertQueue.RemoveAt(0); //удаляем бракованный запрос из очереди
                                }
                            }
                            else
                            {
                                //впервые не можем выполнить этот запрос
                                countCurSQLQueryError = 0;
                                curSQLQueryErrorStr = SQLInsertQueue[0];
                            }
                    }
                }
            }
        }

        public List<string> GetSQLInsertQueue()
        {
            return SQLInsertQueue;
        }

        public void SetSQLInsertQueue(List<string> list)
        {
            SQLInsertQueue = list;
            OnChangeCountInsertQueue?.Invoke(SQLInsertQueue.Count);
        }

        public object GetAnswer(string SQLQuestion)
        {
            LogNewSql?.Invoke(SQLQuestion);
            try
            {
                if (SQLconnection.State == ConnectionState.Open)
                    using (SqlCommand command = new SqlCommand(SQLQuestion, SQLconnection))
                    {
                        return command.ExecuteScalar();
                    }
                else return null;
            }
            catch
            {
                return null;
            }
        }

        public string ExecuteNow(string SQLQuestion)
        {
            //создано для срочного выполнения вставки по результатам теста.

            // Commented by Ivan Raskov 14 10 2022 
            // Attempt to increase performance

            //LogNewSql?.Invoke(SQLQuestion);
            if (!cDBIsReadonly)
            {
                try
                {
                    if (SQLconnection.State == ConnectionState.Open)
                        using (SqlCommand command = new SqlCommand(SQLQuestion, SQLconnection))
                        {
                            return command.ExecuteNonQuery().ToString();
                        }
                    else return "no connection";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }else
            {
                return "db is in ReadOnly mode";
            }
        }

        public SqlDataReader ExecuteReader(string queryString)
        {
            //создана для получения набора данных
            LogNewSql?.Invoke(queryString);
            try
            {
                if (SQLconnection.State == ConnectionState.Open)
                {
                    SqlCommand command = new SqlCommand(queryString, SQLconnection);
                    return command.ExecuteReader();
                }
                LogEvent?.Invoke("Ошибка выполнения SQL ExecuteReader: \n" + queryString + "\n no SQL connection");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke("Ошибка выполнения SQL ExecuteReader: \n" + queryString + "\n" + ex.Message);
            }
            //если дошли до сюда, то вернем null
            return null;
        }

        private void SendEmailOnDropSQLQuery(string errMsg)
        {
            LogEvent?.Invoke("errorSQL is dropped: " + errMsg);
            try
            {
                brnet_sendEmail.SendMessage bmail = new brnet_sendEmail.SendMessage();

                string msg = "Это сообщение сгенерировано автоматически. <br>" +
                    "Не удалось выполнить SQL запроc: <br> " + errMsg.Replace("\n", "<br>");
                if (emails.Count > 0)
                {
                    string emailList = emails[0];
                    for (int i = 1; i < emails.Count; i++) //i < listOfEmail.Count
                    {
                        emailList += ", " + emails[i];
                    }
                    bmail.SendEmail_v2("no-reply@beko.ru", "app_" + appName, emailList, "Error on SQL Query", msg,
                        "", "", "", 0, appName);
                }
                else
                {
                    LogEvent?.Invoke("List to email is empty. Email not sent.");
                }
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke("Не могу отправить email об ошибочном SQL:" + ex.Message);
            }
        }

        private List<string> ReadEmailListFromConfig(string prefix)
        {
            int i = 1;
            List<string> curList = new List<string>();
            while (i > 0)
            {
                try
                {
                    string st = ConfigurationManager.AppSettings[prefix + i++.ToString()];
                    if (string.IsNullOrEmpty(st)) i = 0; //закончили чтение
                    else curList.Add(st);
                }
                catch
                {
                    i = 0;
                }
                if (i > 100) i = 0;
            }
            return curList;
        }

    }
}


/*
 лог изменений
v1.1 RF_2A_Omnia
    - теперь одно событие на потерю и восстановление связи - OnConnectionChange(bool NewState)
    - теперь почту модуль отправляет сам - не забываем добавить в конфиг адреса и в проект web service
 */