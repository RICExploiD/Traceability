namespace Traceability.Models
{
    using Services;
    public class BekoComponent
    {
        public BekoComponent(string componentCode) { ComponentCode = componentCode; }
        /// <summary>
        /// Component code of saved component.
        /// </summary>
        public string ComponentCode { get; }
        /// <summary>
        /// Barcode that recieved from scanner device by current object.
        /// </summary>
        public string ComponentBarcode { get; set; }
        /// <summary>
        /// Material code contained in component barcode.
        /// </summary>
        public string ComponentMaterial { get; set; } = null;
        /// <summary>
        /// Material model.
        /// </summary>
        public string ComponentMaterialModel 
        { 
            get 
            {
                if (ComponentMaterial == null) return null;
                return Sql.GetMaterialModel(ComponentMaterial); 
            } 
        }
        /// <summary>
        /// Creates a new <paramref name="BekoComponent"/> object by station to initialize component code.
        /// </summary>
        public static BekoComponent CreateNew(int? station) => station is null ? null : new BekoComponent(Sql.GetComponentCodeByStation(station));
    }
}
