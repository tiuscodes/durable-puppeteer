namespace DurablePuppeteer.Model
{
    using System;
    using System.Text;

    public class PropertyRecord
    {
        public String Owner
        { get; set; }
        public String Address
        { get; set; }
        public String Suburb
        { get; set; }
        public String Town
        { get; set; }
        public String TaName
        { get; set; }
        private String datesold;
        public String DateSold
        {
            get
            {
                return this.datesold;
            }
            set
            {
                var inputEncoding = Encoding.GetEncoding("iso-8859-1");
                var isoBytes = inputEncoding.GetBytes(value);
                var output = Encoding.UTF8.GetString(isoBytes);
                this.datesold = output;
            }
        }

        public String FloorSize
        { get; set; }
        public String LandSize
        { get; set; }
        public String BedroomNumber
        { get; set; }
        private String ratingValuation;
        public String RatingValuation
        {
            get
            {
                return this.ratingValuation;
            }
            set
            {
                this.ratingValuation = value.Replace("$", "");
            }
        }

        private String lastSalePrice;
        public String LastSalePrice
        {
            get
            {
                return this.lastSalePrice;
            }
            set
            {
                this.lastSalePrice = value.Replace("$", "");
            }
        }
    }
}