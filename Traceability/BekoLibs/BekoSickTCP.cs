using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Traceability
{
    class BekoSickTCP
    {

        private string lastReconnectException = string.Empty;
        //поля
        private const int timeToReconnect = 5000; //ms для таймера проверки соединения
        private const int timeToLaserOff = 60000; //ms для таймера проверки соединения
        private TcpClient tcpClient;
        private NetworkStream nStream;
        private readonly string remoteIp;
        private readonly int remotePort;

        private bool nowConnecting = false; //не начинаем новое подключение, если не завершили прошлое
        private bool nowReading = false; //не начинаем новое чтение, если не завершили прошлое

        private readonly Timer timerRead;
        private readonly Timer timerReConnect;
        private readonly System.Timers.Timer timerLaserOff = new System.Timers.Timer
        {
            AutoReset = false,
            Enabled = false,
            Interval = timeToLaserOff,
        };


        public readonly bool isBarcode22Mode; //true - будем читать только 22 символа баркода. false - выдавать порции информации от символа начала, до символа конца 

        //свойства
        public bool IsConnected { get; private set; }

        public int Status { get; }
        public int Period { get; set; } = 1000; //ms

        //события
        public delegate void delegHandlerNoParam();
        public delegate void delegHandlerBoolConn(bool isConnected);
        public delegate void delegHandlerBarCode(string BarCode);
        public delegate void delegHandlerStr(string msg);
        public event delegHandlerNoParam OnConnectionLost;
        public event delegHandlerNoParam OnConnectionRestore;
        public event delegHandlerBoolConn OnConnectionChange;
        public event delegHandlerBarCode ReadBarcode;
        public event delegHandlerStr OnLogEvent;


        ~BekoSickTCP() { Stop(); }
        public BekoSickTCP(string RemoteIp, int RemotePort, bool useModeBarcode22 = true)
        {
            remoteIp = RemoteIp;
            remotePort = RemotePort;
            isBarcode22Mode = useModeBarcode22;
            tcpClient = new TcpClient();
            timerRead = new Timer(new TimerCallback(timerRead_Tick), null, 0, Period);
            timerReConnect = new Timer(new TimerCallback(timerReConnect_Tick), null, 0, timeToReconnect);
            timerLaserOff.Elapsed += TimerLaserOff_Elapsed;
        }

        public bool ScannerOn()
        {
            try
            {
                byte[] bytes2 = new byte[] { 0x2, 0x73, 0x4d, 0x49, 0x20, 0x34, 0x37, 0x03 };
                nStream.Write(bytes2, 0, bytes2.Length);
                return true;
            }
            catch { return false; }
        }
        public bool ScannerOff()
        {
            try
            {
                byte[] bytes2 = new byte[] { 0x2, 0x73, 0x4d, 0x49, 0x20, 0x34, 0x38, 0x03 };
                nStream.Write(bytes2, 0, bytes2.Length);
                return true;
            }
            catch { return false; }
        }

        private void timerReConnect_Tick(object state)
        {
            if (tcpClient.Client != null && tcpClient.Connected)
            {
                byte[] tmp = new byte[] { 0 };
                try { tcpClient.Client.Send(tmp, 1, 0); } catch { } //эта строчка для принудительной проверки состояния соединения
            }

            if (tcpClient.Connected != IsConnected)
            {
                //сменился статус соединения
                IsConnected = tcpClient.Connected;
                OnStatusChanged();
            }

            TraceStatus();

            if (tcpClient.Connected) return;

            if (nowConnecting) return;

            lock (this)
            {
                nowConnecting = true;
                try
                {
                    //try { tcpClient.Close(); } catch { }
                    tcpClient = new TcpClient(); //для пересоздания сокета
                    tcpClient.Connect(remoteIp, remotePort);
                    if (tcpClient.Connected != IsConnected)
                    {
                        IsConnected = tcpClient.Connected;
                        OnStatusChanged(); //для отсылки событий за пределы класса
                    }

                    nStream = tcpClient.GetStream();
                    lastReconnectException = string.Empty;
                }
                catch (Exception ex)
                {
                    var errorMessage = "Ошибка при соединении со сканером " + remoteIp + ":" + remotePort + "\nОшибка: " + ex.Message;
                    
                    if (!lastReconnectException.Equals(errorMessage))
                    {
                        lastReconnectException = errorMessage;
                        //здесь будет обработка ошибок при подключении
                        OnLogEvent?.Invoke(errorMessage);
                    }
                }
                nowConnecting = false;
            }
        }

        
        public void Stop()
        {
            try 
            { 
                tcpClient?.Close(); 
                tcpClient?.Dispose();
            } 
            catch { }

            try 
            { 
                nStream?.Close();
                nStream?.Dispose();
            } catch { }

            try  
            { 
                timerRead?.Dispose();
                timerReConnect?.Dispose();
            } catch { }

            OnConnectionChange?.Invoke(false);
        }
        private void timerRead_Tick(object sender)
        {
            if (nowReading) return;
            lock (this) nowReading = true;
            if (tcpClient.Client != null && tcpClient.Connected)
            {
                try
                {
                    if (isBarcode22Mode)
                    {
                        //читаем строго порциями по 22 символа (24, если учитывать символы начала и конца сообщения)
                        byte[] dataBarcode22 = new byte[24]; //22 символа сам баркод, плюс он обрамлен символами \u0002 и \u0003 - символы начала и конца текста в консоли
                        int countBytes = 0;

                        if (nStream != null && nStream.DataAvailable)
                        {
                            countBytes = nStream.Read(dataBarcode22, 0, dataBarcode22.Length);
                            string response = Encoding.UTF8.GetString(dataBarcode22, 0, countBytes);
                            response = response.Replace("\u0002", "").Replace("\u0003", ""); //убираем символы начала и конца строки
                            OnLogEvent?.Invoke("Read: " + response);
                            ReadBarcode?.Invoke(response);
                        }
                    }
                    else
                    {
                        //попробуем читать по одному байту
                        int curByte = 0;
                        byte[] dataBarcodeCustom = new byte[255]; //будем считать, что длиннее баркода у нас не будет ))
                        int countBytes = 0;                        //проверим вообще наличие данных в потоке
                        string response = "";
                        if (nStream != null && nStream.DataAvailable)
                        {

                            while (curByte != 2) curByte = nStream.ReadByte(); //дождемся символа начала (curByte == 2)
                                                                               //далее будеи читать пока не будет символа конца сообщения (curByte == 3) или всего буфера (curByte == -1). -1 - это нештатка без символа канца строки
                            curByte = 0;
                            while (curByte != -1 && curByte != 2 && curByte != 3)
                            {
                                curByte = nStream.ReadByte();
                                dataBarcodeCustom[countBytes++] = (byte)curByte;
                            }
                            countBytes--; // - сотрем последний символ (окончания сообщения)
                            response = Encoding.UTF8.GetString(dataBarcodeCustom, 0, countBytes);
                            if (response == "sAI 47 1")
                            {
                                timerLaserOff.Start();
                                OnLogEvent?.Invoke("Команда включить лазер выполнена");
                            }
                            else if (response == "sAI 48 1")
                            {
                                OnLogEvent?.Invoke("Команда погасить лазер выполнена");
                            }
                            else
                            {
                                if (timerLaserOff.Enabled) timerLaserOff.Stop(); //вручную запросили, получили ответ, таймер можно отменить
                                if (remotePort == 2111)
                                {
                                    //default value of sick port 2111 is:
                                    //"\u0002\r\nTT=226ms OTL=10mm CC=1 FC=600, MC=1\r\nCS_202111021254\r\nDATAMATRIX\r\nSZ=16x16, RES=6.00x6.00Pixel, CTV=83%, UECV=100%\r\n\u0003"
                                    string[] items = response.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                    if (items.Length != 6) OnLogEvent?.Invoke("Ошибка формата для порта 2111");
                                    if (items.Length > 3) response = items[2];
                                }

                                OnLogEvent?.Invoke("Read: " + response);
                                ReadBarcode?.Invoke(response);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLogEvent?.Invoke("Ошибка при чтении " + ex.Message);
                }
            }
            nowReading = false;
        }

        private void TraceStatus()
        {
            OnConnectionChange?.Invoke(IsConnected);
        }

        private void OnStatusChanged()
        {
            //сюда попадаем при изменении статуса
            if (IsConnected)
            {
                OnConnectionRestore?.Invoke();
                OnLogEvent?.Invoke("Соединение со сканером " + remoteIp + ":" + remotePort + " установлено");
            }
            else
            {
                OnConnectionLost?.Invoke();
                OnLogEvent?.Invoke("Соединение со сканером " + remoteIp + ":" + remotePort + " ПОТЕРЯНО");
            }
        }
        private void TimerLaserOff_Elapsed(object sender, System.Timers.ElapsedEventArgs e) => ScannerOff();
    }
}
