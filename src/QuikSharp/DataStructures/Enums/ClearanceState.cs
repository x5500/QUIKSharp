namespace QUIKSharp.DataStructures

{
    public enum ClearanceState
    {
        /// <summary>
        /// «0» – назначен пр.;
        /// </summary>
        PromDefined = 0,

        /// <summary>
        /// «1» – основная сессия;
        /// </summary>
        MainSession = 1,

        /// <summary>
        /// «2» – начался промклиринг;
        /// </summary>
        PromClearingBegan = 2,

        /// <summary>
        /// «3» – завершился промклиринг;
        /// </summary>
        PromClearingEnded = 3,

        /// <summary>
        /// «4» – начался основной клиринг;
        /// </summary>
        MainClearingBegan = 4,

        /// <summary>
        /// «5» – основной клиринг: новая сессия назначена;
        /// </summary>
        MainClearingNewSession = 5,

        /// <summary>
        /// «6» – завершился основной клиринг;
        /// </summary>
        MainClearingEnded = 6,

        /// <summary>
        /// «7» – завершилась вечерняя сессия
        /// </summary>
        EveningClearingEnded = 7,
    }
}