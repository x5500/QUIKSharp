using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    public class CalcBuySellResult
    {
        /// <summary>
        /// Максимально возможное количество бумаг
        /// </summary>
        [JsonProperty("qty")]
        public long Qty { get; set; }

        /// <summary>
        /// Сумма комиссии Buy
        /// </summary>
        [JsonProperty("comission")]
        public decimal Comission { get; set; }
    }
}