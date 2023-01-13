namespace Traceability.Models
{
    public class BekoProduct
    {
        /// <summary>
        /// Type of scanned product.
        /// </summary>
        public ProductBarcodeType BarcodeType { get; set; }
        /// <summary>
        /// Barcode that recieved from observer parent MainWindow object.
        /// </summary>
        public string ProductBarcode { get; set; }
        /// <summary>
        /// Awaited component material.
        /// </summary>
        public string ProductMaterial { get; set; }
        /// <summary>
        /// IOT product type.
        /// </summary>
        public string ProductIOTType { get; set; }
        /// <summary>
        /// Product number of scanned barcode (first 10 symbols).
        /// </summary>
        public string ProductNo 
        { 
            get 
            {
                if (Services.WorkPageCore.Line.Equals(ProductionLine.WashingMachine))
                    return _wmProduct;
                else
                    return ProductBarcode?.Substring(0, 10) ?? ""; 
            }
            set
            {
                if (Services.WorkPageCore.Line.Equals(ProductionLine.WashingMachine))
                    _wmProduct = value;
            }
        }
        private string _wmProduct;
        /// <summary>
        /// Serial number of scanned barcode (from 10th symbol 12 symbols).
        /// </summary>
        public string SerialNo { get { return ProductBarcode?.Substring(10, 12) ?? ""; } }
        /// <summary>
        /// Object that contains additional info about product like a washing machine.
        /// </summary>
        public WashineMachineDetails WMDetails { get; private set; } = null;
        public void InitWMDetails() { WMDetails = new WashineMachineDetails(); }
    }
}
