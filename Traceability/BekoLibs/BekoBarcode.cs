namespace Traceability
{
    public static class BekoBarcode
    {
        
        public static bool Barcode22IsOk(string barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return false;

            const string pattern = @"^73\d{20}$";
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, pattern);
            /*
            if (!(barcode.Length == 22 && barcode.Substring(0, 2) == "73")) return false;
            foreach (char c in barcode)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;*/
        }

        public static bool BarcodeDummyRFIsOk(string barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return false;

            const string pattern = @"^\d{12}$";
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, pattern);
        }

        public static bool BarcodeMicIsOk(string barcode)
        {
            //MIC - Material Identity Card 
            //здесь будем делать проверки на коректность баркода
            //"5753570800+0212200010+10"

            //здесь написано 
            //  ^         - начало строки
            //  \d{10}    - 10 цифр
            //  \+        - символ '+'
            //  \d{10}    - 10 цифр
            //  \+        - символ '+'
            //  \d{1,5}   - 1-5 цифр
            //  $         - символ конца строки

            if (string.IsNullOrEmpty(barcode) || barcode == "NoRead") return false;

            const string pattern = @"^\d{10}\+\d{10}\+\d{1,5}$";
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, pattern);
        }

        public static bool BarcodeCSIsOk(string barcode)
        {
            //CS - case of WM
            //здесь будем делать проверки на коректность баркода корпуса WM
            //"CS_7321610001_E450_2019528165739"
            //"CS_20190703173058"

            //здесь написано 
            //  ^CS_         - начинается на CS_

            if (string.IsNullOrEmpty(barcode) || barcode == "NoRead") return false;

            const string pattern = @"^CS_";
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, pattern);
        }

        public static bool BarcodeTSIsOk(string barcode)
        {
            //TS - Tube of WM
            //здесь будем делать проверки на коректность баркода бака WM
            //"TS_34_2322901100_20210513050623"

            //здесь написано 
            //  ^TS_         - начинается на TS_
            
            if (string.IsNullOrEmpty(barcode) || barcode == "NoRead") return false;

            const string pattern = @"^TS_";
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, pattern);
        }
    }
}
