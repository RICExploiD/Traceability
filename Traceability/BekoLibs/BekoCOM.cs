using System;
using System.IO.Ports;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;

namespace Traceability
{
    class BekoCOM
    {
        //поля
        private SerialPort serialPort;
        private const int timeToReconnect = 4000; //ms для таймера проверки соединения
        private System.Threading.Timer timerReConnect;
        private bool lastConnected = false;
        private bool isConnecting = false;

        //события
        public delegate void delegHandlerBool(bool isConnected);
        public delegate void delegHandlerStrMsg(string msg);
        public delegate void delegHandlerStr(string barcode);
        public event delegHandlerBool OnConnectionChange; //срабатывает при изменении статуса соединения
        public event delegHandlerStrMsg OnLog; //событие куда выбрасываем логи
        public event delegHandlerStr OnBarcodeReaded;

        public bool StartFromConfig(string configPrefix)
        {
            try
            {
                string portName = ConfigurationManager.AppSettings[configPrefix + "ComPort"].ToUpper();
                int baudRate = Int32.Parse(ConfigurationManager.AppSettings[configPrefix + "BaudRate"]);
                string sPar = ConfigurationManager.AppSettings[configPrefix + "Parity"].ToLower();//сюда считываем строчку с Parity                
                Parity parity;
                switch (sPar)
                {
                    case "odd":
                    case "1":
                        parity = Parity.Odd;
                        break;
                    case "even":
                    case "2":
                        parity = Parity.Even;
                        break;
                    case "mark":
                    case "3":
                        parity = Parity.Mark;
                        break;
                    case "space":
                    case "4":
                        parity = Parity.Space;
                        break;
                    default:
                        parity = Parity.None;
                        break;
                }
                int dataBits = Int32.Parse(ConfigurationManager.AppSettings[configPrefix + "DataBits"]);
                string ssBits = ConfigurationManager.AppSettings[configPrefix + "StopBits"].ToLower();//сюда считываем строчку StopBits
                StopBits stopBits;
                switch (ssBits)
                {
                    case "one":
                    case "1":
                        stopBits = StopBits.One;
                        break;
                    case "two":
                    case "2":
                        stopBits = StopBits.Two;
                        break;
                    case "onepointfive":
                    case "3":
                        stopBits = StopBits.OnePointFive;
                        break;
                    default:
                        stopBits = StopBits.None;
                        break;
                }
                return Start(portName, baudRate, parity, dataBits, stopBits);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("Error init COM port " + ex.Message);
                return false;
            }
        }

        public bool Start(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            List<string> ports = new List<string>(SerialPort.GetPortNames());
            string strPorts = "";
            foreach (string st in ports) strPorts += st + ",";
            if (!ports.Contains(portName))
            {
                OnLog?.Invoke("Невозможно открыть порт " + portName + " доступные порты: " + strPorts + " проверьте файл конфигурации и COM устройство.");
                return false;
            }
            try
            {
                serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                //serialPort.ReadTimeout = 1000; // в примере из инета это было, но не очень понятно зачем                
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("Error init COM port " + ex.Message);
                return false;
            }
            //  тут подписываемся на событие прихода данных в порт            
            serialPort.DataReceived += SerialPort_DataReceived;
            timerReConnect = new Timer(new TimerCallback(timerReConnect_Tick), null, 0, timeToReconnect);
            return true;
        }

        public void Stop()
        {
            if (timerReConnect is null) return;
            
            timerReConnect.Dispose();

            if (serialPort is null) return;

            serialPort.Close();
            serialPort.Dispose();

            OnConnectionChange?.Invoke(false);
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // changed by Ivan Raskov 18.10.2022
            // 300 -> 200
            Thread.Sleep(200);

            //byte[] bytes = new byte[25];
            //int count = serialPort.Read(bytes, 0, serialPort.BytesToRead);
            //string hex = BitConverter.ToString(bytes);
            string barcode = serialPort.ReadExisting();
            //string barcode = serialPort.ReadLine();

            // added by Ivan Raskov 18.10.2022
            if (barcode.Contains("\r"))
            {
                barcode = barcode.Split('\r')[0];

                barcode = barcode.Replace("\r", "");
            }

            OnLog?.Invoke("Read: " + barcode);
            OnBarcodeReaded?.Invoke(barcode);
        }

        private void timerReConnect_Tick(object state)
        {
            if (isConnecting) return;
            isConnecting = true;
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.Open();
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke("Ошибка соединения " + ex.Message);
                }
            }
            bool newStatus = serialPort.IsOpen;
            if (lastConnected != newStatus)
            {
                //значит состояние изменилось
                lastConnected = newStatus;
                OnConnectionChange?.Invoke(newStatus);
                string msg = newStatus ? "установлено" : "потеряно";
                OnLog?.Invoke($"Соединение {serialPort.PortName} {msg}");
            }
            isConnecting = false;
        }

    }
}
