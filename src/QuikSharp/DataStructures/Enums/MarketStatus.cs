namespace QUIKSharp.DataStructures

{
    public enum MarketStatus
    {
        Close = 0,

        /// <summary>
        /// торгуется
        /// </summary>
        Open = 1,

        /// <summary>
        /// приостановлена
        /// </summary>
        Hold = 2,

        /// <summary>
        /// Завершена
        /// </summary>
        Ended = 3,
    }
}