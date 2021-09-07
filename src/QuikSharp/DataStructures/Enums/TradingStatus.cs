namespace QUIKSharp.DataStructures

{
    public enum TradingStatus
    {
        /// <summary>
        /// закрыта
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Открыта
        /// </summary>
        Open = 1,

        /// <summary>
        /// Закрытие
        /// </summary>
        Closing = 2,

        /// <summary>
        /// Открытие
        /// </summary>
        Opening = 3,

        /// <summary>
        /// аукцион
        /// </summary>
        Auction = 4,

        /// <summary>
        /// ЦАЗ
        /// </summary>
        AuctionClosePrice = 5,

        /// <summary>
        ///
        /// </summary>
        DiscreteAuction = 6,
    }
}