using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Opc.Ua.Client;

namespace Traceability
{
    using BekoOPCUA;
    using System;
    using Services;

    partial class MainWindow : Window
    {
        private const string OPCUAPrefix = "ns=2;s=";
        private BekoOPCDA myOPCDA { get; set; }
        private BekoOPCUA myOPCUA { get; set; }
        private Dictionary<string, object> OpcTags { get; set; } = new Dictionary<string, object>();
        private void OPCInit(OPCType type)
        {
            switch (type)
            {
                case OPCType.UA:
                    {
                        var ip = ConfigurationManager.AppSettings["OPCIPServer"];
                        var port = ConfigurationManager.AppSettings["OPCPort"];

                        myOPCUA = new BekoOPCUA($"{ip}:{port}", SHAType.SHA1) { IsReadOnly = false };

                        myOPCUA.OnLogEvent += (message, _) => OPCLogAction(message);
                        myOPCUA.OnConnectionChange += MyOPC_OnConnectionChange;
                        
                        myOPCUA.Run();
                        
                        if (!myOPCUA.IsConnected)
                        {
                            MessageBox.Show("Не удалось соединиться с OPC UA сервером. " +
                                "Проверьте настройки подключения и соединение с сервером.",
                                "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                            
                        myOPCUA.SubscribeAndGetTag(OpcTags["nOPCTagIsHere"].ToString(), 50)
                            .Notification += OnPresenseNodeUpdate;
                        myOPCUA.SubscribeAndGetTag(OpcTags["nOPCTagButtons"].ToString(), 50)
                            .Notification += OnButtonNodeUpdate;

                        break;
                    }
                case OPCType.DA:
                    {
                        var progid = ConfigurationManager.AppSettings["OPCProgID"];
                        var node = ConfigurationManager.AppSettings["OPCNode"];

                        myOPCDA = new BekoOPCDA(progid, node, true);
                        myOPCDA.OnLogEvent += OPCLogAction;
                        myOPCDA.OnConnectionChange += MyOPC_OnConnectionChange;
                        myOPCDA.OnChangeSubcribedTags += MyOPC_OnChangeSubcribedTags;

                        break;
                    }
            }
        }
        private void MyOPC_OnChangeSubcribedTags(int TagIndex, short newValue)
        {

            if (TagIndex == (int)OpcTags["nOPCTagIsHere"])
            {
                //защита от получения события кнопки после съeзда с датчика
                Task.Delay(300).GetAwaiter().GetResult();

                OnTagIsHereChanged(newValue);

                return;
            }

            if (TagIndex == (int)OpcTags["nOPCTagButtons"])
            {                
                OnTagButtonChanged(newValue);

                return;
            }
        }
        private void OnTagIsHereChanged(object value)
        {
            //бит наличия паллета
            if (Convert.ToByte(value) == 1 ||
                Convert.ToByte(value) == 49)
                ProductIsHere();
            else
                ProductIsNotHere();
        }
        private void OnTagButtonChanged(object value)
        {
            //TODO
            //ButtonPressedToSkip(); ButtonPressedToSave();
            
            //0 или 48 - это кнопка отжата
            //1 или 49 - это кнопка нажата
            if (Convert.ToByte(value) == 1 || 
                Convert.ToByte(value) == 49)
                EventBtnSendPressed();
            else
                EventBtnSendUnPressed();
        }

        private void ProductIsHere() { UpdateIsHereLabel(); }
        private void ProductIsNotHere()
        {
            OPCLogAction("<-- Паллет отсутствует -->\n");

            WorkPageCore.ClearOperatorNoitfyQueueData();

            WorkPageCore.CanGoPermit = false;

            UpdateIsHereLabel(false);
            ClearTemporaryData();
            observedWorkPage?.ClearTemporaryData();
            if (WorkPageCore.IsCurrentLineWM) ClearWMDetails();
        }
        private void EventBtnSendUnPressed() { UpdateSendButtonLabel(false); }
        private void EventBtnSendPressed() { UpdateSendButtonLabel(); }
        
        public void ReconnectToOPCTags(ConnectionPLCType scanType)
        {
            try
            {
                myOPCDA?.Disconnect();
                myOPCUA?.Terminate();
            }
            catch { }

            ConnectToOpcTags(ConnectionPLCType.OPC, AppSettings.OPCType);
        }

        private void ConnectToOpcTags(ConnectionPLCType PLCType, OPCType OPCType = OPCType.None)
        {
            OpcTags.Clear();

            switch (PLCType)
            {
                case ConnectionPLCType.OPC:

                    //Доавляем теги в OPC
                    string tagIsHere = ConfigurationManager.AppSettings["TagIsHere"].ToString();
                    string tagButtons = ConfigurationManager.AppSettings["TagButtons"].ToString();
                    string tagCanGo = ConfigurationManager.AppSettings["TagCanGo"].ToString();
                    
                    switch (OPCType)
                    {
                        case OPCType.DA:
                            {
                                OPCInit(OPCType);

                                OpcTags.Add("nOPCTagIsHere", myOPCDA.AddToSubcribe(tagIsHere));
                                OpcTags.Add("nOPCTagButtons", myOPCDA.AddToSubcribe(tagButtons));
                                OpcTags.Add("nOPCTagCanGo", myOPCDA.AddToSubcribe(tagCanGo));

                                break;
                            }
                        case OPCType.UA:
                            {
                                OpcTags.Add("nOPCTagIsHere", $"{OPCUAPrefix}{tagIsHere}");
                                OpcTags.Add("nOPCTagButtons", $"{OPCUAPrefix}{tagButtons}");
                                OpcTags.Add("nOPCTagCanGo", $"{OPCUAPrefix}{tagCanGo}");
                                
                                OPCInit(OPCType);

                                break;
                            }
                    }

                    break;

                case ConnectionPLCType.Sie: default: break;
            }
        }

        private void WriteToOPCUATag(string tag, object value)
        {
            try 
            {
                var opctag = OpcTags[tag]?.ToString();
                myOPCUA.Write(opctag, value); 
            }
            catch (Exception) { }
        }
        private void OnPresenseNodeUpdate(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in monitoredItem.DequeueValues()) OnTagIsHereChanged(value.Value);
        }
        private void OnButtonNodeUpdate(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in monitoredItem.DequeueValues()) OnTagButtonChanged(value.Value);
        }
    }
}
