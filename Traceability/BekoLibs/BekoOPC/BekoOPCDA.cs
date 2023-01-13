using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Traceability
{
    using OPCAutomation;
    using Models;

    class BekoOPCDA
    {
        private string lastReconnectException = string.Empty;

        //tmp
        readonly bool enableWriteToOPC = false; //false is default

        //поля
        private string OPCProgID; // "KEPware.KEPServerEx.V5"
        private string OPCNode; //"BRKIR140V"
        private OPCServer opcServer;

        private OPCGroup opcReadGroup; //для ручного чтения из контроллера
        private List<string> opcReadTags = new List<string>(); //для хранения текстовых имен тегов

        private List<OPCGroup> opcReadBarCode22Groups;
        private List<string> opcReadBarCode22TagBases = new List<string>(); //для хранения текстовых имен тегов первого символа баркода

        private List<OPCGroup> opcReadDummy12RFGroups;
        private List<string> opcReadDummy12RFTagBases = new List<string>();

        private OPCGroup opcWriteGroup; //для записи в контроллер
        private List<string> opcWriteTags = new List<string>(); //для хранения текстовых имен тегов

        private OPCGroup opcSubscribeGroup; //для подписки на изменения
        private List<string> opcSubscribeTags = new List<string>(); //для хранения текстовых имен тегов

        private const int timeToReconnect = 5000; //ms для таймера проверки соединения

        private Timer timerReConnect;

        private bool isConnected = false; //служебная для обработки событий изменения статуса
        private bool isReconnecting = false; //флаг, что соединение уже идет

        //свойства
        public bool IsConnected { get { return isConnected; } private set { StatusChange(value); isConnected = value; } } //здесь будем взводить флаг, что есть изменения
        public bool WriteEnable { get { return enableWriteToOPC; } }


        //события
        public delegate void delegHandlerNoParam();
        public delegate void delegHandlerBoolConn(bool isConnected);
        public delegate void delegHandlerInt2(int TagIndex, short newValue);

        public event delegHandlerBoolConn OnConnectionChange;
        public event delegHandlerInt2 OnChangeSubcribedTags;
        public event delegHandlerStr OnLogEvent;

        public BekoOPCDA(string ProgID, string Node, bool OPCReadOnly = false)
        {
            OPCProgID = ProgID;
            OPCNode = Node;
            enableWriteToOPC = !OPCReadOnly;

            timerReConnect = new Timer(new TimerCallback(TimerReConnect_Tick), null, 1, timeToReconnect);
        }

        ~BekoOPCDA()
        {
            Disconnect();
        }
        public void Disconnect()
        {
            ClearAllSubscribedTags();
            timerReConnect?.Dispose();
            opcServer?.Disconnect();
            IsConnected = false;
        }
        private void ClearAllSubscribedTags()
        {
            opcReadTags?.Clear();
            opcWriteTags?.Clear();
            opcSubscribeTags?.Clear();
            opcReadDummy12RFGroups?.Clear();
            opcReadBarCode22Groups?.Clear();
        }

        private void TimerReConnect_Tick(object state)
        {
            lock (this)
            {
                if (isReconnecting) return;
                isReconnecting = true;
                int step = 0;
                try
                {
                    if (opcServer == null || opcServer.ServerState != 1 || IsConnected == false)
                    {
                        step = 1;
                        opcServer = new OPCServer();
                        opcServer.Connect(OPCProgID, OPCNode);

                        //заново заполним группу на ручное чтение
                        step = 2;
                        opcReadGroup = opcServer.OPCGroups.Add("GroupRead");  //Имя группы может быть любым,  
                        for (int i = 0; i < opcReadTags.Count; i++) opcReadGroup.OPCItems.AddItem(opcReadTags[i], i + 1);

                        //заполним группы на чтение баркода по OPC
                        step = 3;
                        opcReadBarCode22Groups = new List<OPCGroup>();
                        for (int i = 0; i < opcReadBarCode22TagBases.Count; i++)
                        {
                            AddGroupToBArCode22Groupe(opcReadBarCode22TagBases[i]);
                        }

                        //заполним группы на чтение Dummy12RF по OPC
                        step = 4;
                        opcReadDummy12RFGroups = new List<OPCGroup>();
                        for (var i = 0; i < opcReadDummy12RFTagBases.Count; i++)
                        {
                            AddToDummy12RFGroupe(opcReadDummy12RFTagBases[i]);
                        }

                        //заново заполним группу на запись
                        step = 5;
                        opcWriteGroup = opcServer.OPCGroups.Add("GroupWrite");  //Имя группы может быть любым, 
                        for (int i = 0; i < opcWriteTags.Count; i++) opcWriteGroup.OPCItems.AddItem(opcWriteTags[i], i + 1);

                        //заново заполним группу на подписку
                        step = 6;
                        opcSubscribeGroup = opcServer.OPCGroups.Add("GroupSubscribe");  //Имя группы может быть любым, 
                        for (int i = 0; i < opcSubscribeTags.Count; i++) opcSubscribeGroup.OPCItems.AddItem(opcSubscribeTags[i], i + 1);
                        opcSubscribeGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(OPCdataChangedListener);
                        if (opcSubscribeTags.Count > 0) //подписка без тегов невозможна
                        {
                            opcSubscribeGroup.IsActive = true;
                            opcSubscribeGroup.IsSubscribed = true;
                        }

                        IsConnected = true;
                        lastReconnectException = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = "Не удалось установить соединение или добавить теги. Step = " + step.ToString() + " " + ex.Message;

                    if (!lastReconnectException.Equals(errorMessage))
                    {
                        lastReconnectException = errorMessage;
                        OnLogEvent?.Invoke(errorMessage);
                        opcServer = null;
                        IsConnected = false;
                    }
                }
                isReconnecting = false;
            }
        }

        private void StatusChange(bool newValue)
        {
            if (isConnected != newValue)
            {
                OnConnectionChange?.Invoke(newValue);
                OnLogEvent?.Invoke(newValue ? "Соединение установлено." : "Соединение потеряно.");
            }
        }

        public int AddToReadGroup(string tagName)
        {
            opcReadTags.Add(tagName);
            if (IsConnected)
                try
                {
                    opcReadGroup.OPCItems.AddItem(tagName, opcReadTags.Count); //при записи в OPC будем ориентироваться на номер OPC тега в списке
                }
                catch (Exception ex)
                {
                    OnLogEvent?.Invoke("Ошибка добавления тега на чтение " + ex.Message);
                }
            return opcReadTags.Count;
        }

        private void AddGroupToBArCode22Groupe(string TagBase)
        {
            try
            {
                int nowNumber = opcReadBarCode22Groups.Count + 1;
                opcReadBarCode22Groups.Add(opcServer.OPCGroups.Add("BarCodes" + nowNumber.ToString()));//Имя группы может быть любым, )
                for (var i = 0; i < 22; i++)
                {
                    opcReadBarCode22Groups[nowNumber - 1].OPCItems.AddItem(TagBase + i.ToString("D2"), i);  //первый символ RF_OMNIA_A3_BARCODE_CHAR00, второй 01 и т.д.
                }
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke("Ошибка добавления тега на чтение BarCode22: " + ex.Message);
            }
        }

        private void AddToDummy12RFGroupe(string TagBase)
        {
            try
            {
                var nowNumber = opcReadDummy12RFGroups.Count + 1;
                opcReadDummy12RFGroups.Add(opcServer.OPCGroups.Add("Dummy12RF" + nowNumber.ToString()));//Имя группы может быть любым, )
                for (var i = 0; i < 12; i++)
                {
                    opcReadDummy12RFGroups[nowNumber - 1].OPCItems.AddItem(TagBase + i.ToString("D2"), i);  //первый символ RF_OMNIA_A3_BARCODE_CHAR00, второй 01 и т.д.
                }
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke($"Ошибка добавления тега на чтение Dummy12RF: {ex.Message}");
            }
        }

        public int AddToReadBarCode22Groups(string OPCTagBase)
        {
            opcReadBarCode22TagBases.Add(OPCTagBase);
            if (isConnected) AddGroupToBArCode22Groupe(OPCTagBase);

            return opcReadBarCode22TagBases.Count;
        }

        public int AddToReadDummy12RFGroups(string OPCTagBase)
        {
            opcReadDummy12RFTagBases.Add(OPCTagBase);
            if (isConnected)
            {
                AddToDummy12RFGroupe(OPCTagBase);
            }

            return opcReadDummy12RFTagBases.Count;
        }

        public string ReadBarCode22(int BarCodeIndex)
        {
            if (!IsConnected) return "";
            if (BarCodeIndex < 0 || BarCodeIndex > opcReadBarCode22Groups.Count)
            {
                OnLogEvent?.Invoke("Неверный индекс тега на чтение: " + BarCodeIndex.ToString() + " а count = " + opcReadBarCode22Groups.Count.ToString());
                return "";
            }

            BarCodeIndex--; //т.к. здесь у нас нумерация с нуля

            string StockNumber;
            Object input = null;
            Object quality = null;
            Object timeStamp = null;
            int[] a = new int[22];
            byte[] bytes = new byte[22];// = BitConverter.GetBytes(i);
                                        //string s2 = Encoding.ASCII.GetString(bytes);
            try
            {
                for (var i = 0; i < 22; i++)
                {
                    OPCGroup group = opcReadBarCode22Groups[BarCodeIndex];
                    OPCItem item = group.OPCItems.Item(i + 1);
                    item.Read(1, out input, out quality, out timeStamp);
                    bytes[i] = (byte)input;
                }
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke("Ошибка чтения BarCode22 " + ex.Message);
            }
            StockNumber = Encoding.ASCII.GetString(bytes);
            return StockNumber;
        }

        public string ReadDummy12RF(int BarCodeIndex)
        {
            if (!IsConnected)
                return string.Empty;

            if (BarCodeIndex < 0 || BarCodeIndex > opcReadDummy12RFGroups.Count)
            {
                OnLogEvent?.Invoke($"Неверный индекс тега на чтение: {BarCodeIndex}, а count = {opcReadDummy12RFGroups.Count}");
                return string.Empty;
            }

            BarCodeIndex--; //т.к. здесь у нас нумерация с нуля

            var bytes = new byte[12];// = BitConverter.GetBytes(i);
                                     //string s2 = Encoding.ASCII.GetString(bytes);
            try
            {
                for (var i = 0; i < 12; i++)
                {
                    var group = opcReadDummy12RFGroups[BarCodeIndex];
                    var item = group.OPCItems.Item(i + 1);
                    item.Read(1, out object input, out object quality, out object timeStamp);
                    bytes[i] = (byte)input;
                }

                return Encoding.ASCII.GetString(bytes);
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke($"Ошибка чтения Dummy12RF {ex.Message}");
                return string.Empty;
            }
        }

        public Int16 ReadFromTag(int TagIndex)
        {
            if (!IsConnected) return 0;
            if (TagIndex < 0 || TagIndex > opcReadTags.Count)
            {
                OnLogEvent?.Invoke("Неверный индекс тега на чтение: " + TagIndex.ToString() + " а count = " + opcReadTags.Count.ToString());
                return 0;
            }

            Object input = null;
            Object quality = null;
            Object timeStamp = null;
            Int16 result = 0;

            try
            {
                OPCItem item = opcReadGroup.OPCItems.Item(TagIndex);
                item.Read(1, out input, out quality, out timeStamp);
                result = Int16.Parse(input.ToString());
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke("Ошибка чтения тега " + ex.Message);
            }

            return result;
        }

        public int AddToWriteGroup(string tagName)
        {
            opcWriteTags.Add(tagName);
            if (IsConnected)
                try
                {
                    opcWriteGroup.OPCItems.AddItem(tagName, opcWriteTags.Count); //при записи в OPC будем ориентироваться на номер OPC тега в списке
                }
                catch (Exception ex)
                {
                    OnLogEvent?.Invoke("Ошибка добавления тега на запись " + ex.Message);
                }
            return opcWriteTags.Count;
        }

        public bool WriteToTag(int TagIndex, Int16 value)
        {
            bool successWrite = false;
            if (!IsConnected) return successWrite;
            if (TagIndex < 0 || TagIndex > opcWriteTags.Count)
            {
                OnLogEvent?.Invoke("Неверный индекс тега на запись: " + TagIndex.ToString() + " а count = " + opcWriteTags.Count.ToString());
                return successWrite;
            }
            try
            {
                OPCItem item = opcWriteGroup.OPCItems.Item(TagIndex);
                if (enableWriteToOPC) item.Write(value);
                successWrite = true;
            }
            catch (Exception ex)
            {
                OnLogEvent?.Invoke("Ошибка записи в тег. Tag index = " + TagIndex + ", value = " + value + ". " + ex.Message);
            }
            return successWrite;
        }

        public int AddToSubcribe(string tagName)
        {
            opcSubscribeTags.Add(tagName);
            if (IsConnected)
                try
                {
                    opcSubscribeGroup.OPCItems.AddItem(tagName, opcSubscribeTags.Count); //при записи в OPC будем ориентироваться на номер OPC тега в списке
                    opcSubscribeGroup.IsActive = true; //если вдруг ещё не активна ))
                    opcSubscribeGroup.IsSubscribed = true;
                }
                catch (Exception ex)
                {
                    OnLogEvent?.Invoke("Ошибка добавления тега на подписку " + ex.Message);
                }
            return opcSubscribeTags.Count;
        }
        private void OPCdataChangedListener(int transactionID, int count, ref Array handles, ref Array values, ref Array qualities, ref Array timeStamps)
        { 
            //слушаем событие "холодильник приехал"
            //переменные для логов
            string curHandle = "", curValue = "";
            int step = 0;
            for (int i = 1; i <= count; i++)
            {
                try
                {
                    if ((int)handles?.GetValue(i) > 0 && (int)handles?.GetValue(i) <= opcSubscribeTags.Count)
                    {
                        curHandle = curValue = "";
                        step = 1;
                        curHandle = handles.GetValue(i)?.ToString();
                        step = 2;
                        curValue = values.GetValue(i)?.ToString();
                        step = 3;
                        int intHandle = int.Parse(curHandle);
                        step = 4;
                        short intValue = short.Parse(curValue);
                        step = 5;
                        //current value = values.GetValue(i).ToString()
                        OnChangeSubcribedTags?.Invoke(intHandle, intValue);
                    }
                }
                catch (Exception ex)
                {
                    if (step < 5)
                    {
                        OnLogEvent?.Invoke("Ошибка при разборе данных по подписке: step = " + step + " i=" + i + " handle=" + curHandle + " value=" + curValue + " error=" + ex.Message);
                    }
                    else
                    {
                        OnLogEvent?.Invoke("Ошибка в основной программе после реакции на подписку: step = " + step + " i=" + i + " handle=" + curHandle + " value=" + curValue + " error=" + ex.Message);
                    }
                }
            }
        }

    }
}

/*
 v 1.1 - добавлена опция чтения баркода Dummy 
 */