namespace Traceability
{
    public enum AssemblyPoint 
    {
        // Washing machine
        Motors = 102, 
        Pump = 104, 
        EmiFilter = 118, 
        Heater = 107, 
        MainBoard = 108, 
        Cable = 109, 
        PowerChord = 116,

        // Refrigerator
        DefrosHeater = 68,
        RefInverterCard = 72,
        FreezerFan = 73

    }; // для проверки, что программа поддерживает этот номер точки
    public enum ScanType { SickIP, COM, OPC }; // для проверки, что программа поддреживает этот тип сканера
    public enum OPCType { DA, UA, None }; // тип подключения OPC
    public enum ConnectionPLCType { OPC, Sie }; //тип соединения с PLC
    public enum ComponentBarcodeType { MTK, Individual, Revoked }; // тип сканируемого баркода компонента
    public enum ComponentSaveType { AfterButton, AfterScan }; // когда сохранять компонент в БД (только для Individual)
    public enum ComponentClearType { Button, Sensor }; // когда сохранять компонент в БД (только для Individual)
    public enum AvailValues
    {
        IsHere, Case_Barcode, Case_Dummy, Case_Product, Case_Model, Case_CompatibleMaterials,
        Tub_Barcode, Tub_Dummy, Tub_Product, IsMerged, IsEqual,
        Comp_IsFixedInDB, Comp_Barcode, Comp_Number, Comp_CountInBarcode, Comp_CountInDB, Comp_CountInUse, Comp_CountBalance,
        ExtraComp_Barcode, ExtraComp_Point, ExtraComp_CheckEnable, ExtraComp_CheckType
    };
    public enum OperNotifyType { Ok, Info, Attention, Error }
    public enum ProductionLine { Refrigerator = 1, WashingMachine = 2 }
    public enum ProductBarcodeType { Stock22, CS, TS };
}