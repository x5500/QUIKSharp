﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Параметры таблицы "Клиентский портфель", Возвращаемой функцией GetPortfolioInfoEx
    /// </summary>
    public class PortfolioInfoEx
    {
        /// <summary>
        /// Тип клиента
        /// Признак использования схемы кредитования с контролем текущей стоимости активов. Возможные значения:
        /// «МЛ» – используется схема ведения позиции «по плечу», «плечо» рассчитано по значению Входящего лимита
        /// «МП» – используется схема ведения позиции «по плечу», «плечо» указано явным образом
        /// «МОП» – используется схема ведения позиции «лимит на открытую позицию»
        /// «МД» – используется схема ведения позиции «по дисконтам»
        /// \пусто\ – используется схема ведения позиции «по лимитам»
        /// </summary>
        [JsonProperty("is_leverage")]
        public string IsLeverage { get; set; }

        /// <summary>
        /// Вход. активы
        /// Оценка собственных средств клиента до начала торгов
        /// </summary>
        [JsonProperty("in_assets")]
        public decimal InAssets { get; set; }

        /// <summary>
        /// Плечо
        /// Плечо. Если не задано явно, то отношение Входящего лимита к Входящим активам
        /// </summary>
        [JsonProperty("leverage")]
        public decimal Leverage { get; set; }

        /// <summary>
        /// Вход. лимит
        /// Оценка максимальной величины заемных средств до начала торгов
        /// </summary>
        [JsonProperty("open_limit")]
        public decimal OpenLimit { get; set; }

        /// <summary>
        /// Шорты
        /// Оценка стоимости коротких позиций. Значение всегда отрицательное
        /// </summary>
        [JsonProperty("val_short")]
        public decimal ValShort { get; set; }

        /// <summary>
        /// Лонги
        /// Оценка стоимости длинных позиций
        /// </summary>
        [JsonProperty("val_long")]
        public decimal ValLong { get; set; }

        /// <summary>
        /// Лонги МО
        /// Оценка стоимости длинных позиций по маржинальным бумагам, принимаемым в обеспечение
        /// </summary>
        [JsonProperty("val_long_margin")]
        public string ValLongMargin { get; set; }

        /// <summary>
        /// Лонги О
        /// Оценка стоимости длинных позиций по немаржинальным бумагам, принимаемым в обеспечение
        /// </summary>
        [JsonProperty("val_long_asset")]
        public decimal ValLongAsset { get; set; }

        /// <summary>
        /// Тек. активы
        /// Оценка собственных средств клиента по текущим позициям и ценам
        /// </summary>
        [JsonProperty("assets")]
        public decimal Assets { get; set; }

        /// <summary>
        /// Текущее плечо
        /// </summary>
        [JsonProperty("cur_leverage")]
        public decimal CurLeverage { get; set; }

        /// <summary>
        /// Ур. маржи
        /// Уровень маржи, в процентах
        /// </summary>
        [JsonProperty("margin")]
        public decimal Margin { get; set; }

        /// <summary>
        /// Тек. лимит
        /// Текущая оценка максимальной величины заемных средств
        /// </summary>
        [JsonProperty("lim_all")]
        public string LimAll { get; set; }

        /// <summary>
        /// ДостТекЛимит
        /// Оценка величины заемных средств, доступных для дальнейшего открытия позиций
        /// </summary>
        [JsonProperty("av_lim_all")]
        public decimal AvLimAll { get; set; }

        /// <summary>
        /// Блок. покупка
        /// Оценка стоимости активов в заявках на покупку
        /// </summary>
        [JsonProperty("locked_buy")]
        public decimal LockedBuy { get; set; }

        /// <summary>
        /// Блок. пок. маржин.
        /// Оценка стоимости активов в заявках на покупку маржинальных бумаг, принимаемых в обеспечение
        /// </summary>
        [JsonProperty("locked_buy_margin")]
        public decimal LockedBuyMargin { get; set; }

        /// <summary>
        /// Блок.пок. обесп.
        /// Оценка стоимости активов в заявках на покупку немаржинальных бумаг, принимаемых в обеспечение
        /// </summary>
        [JsonProperty("locked_buy_asset")]
        public decimal LockedBuyAsset { get; set; }

        /// <summary>
        /// Блок. продажа
        /// Оценка стоимости активов в заявках на продажу маржинальных бумаг
        /// </summary>
        [JsonProperty("locked_sell")]
        public decimal LockedSell { get; set; }

        /// <summary>
        /// Блок. пок. немарж.
        /// Оценка стоимости активов в заявках на покупку немаржинальных бумаг
        /// </summary>
        [JsonProperty("locked_value_coef")]
        public decimal LockedValueCoef { get; set; }

        /// <summary>
        /// ВходСредства
        /// Оценка стоимости всех позиций клиента в ценах закрытия предыдущей торговой сессии, включая позиции по немаржинальным бумагам
        /// </summary>
        [JsonProperty("in_all_assets")]
        public decimal InAllAssets { get; set; }

        /// <summary>
        /// ТекСредства
        /// Текущая оценка стоимости всех позиций клиента
        /// </summary>
        [JsonProperty("all_assets")]
        public decimal AllAssets { get; set; }

        /// <summary>
        /// Прибыль/убытки
        /// Абсолютная величина изменения стоимости всех позиций клиента
        /// </summary>
        [JsonProperty("profit_loss")]
        public decimal ProfitLoss { get; set; }

        /// <summary>
        /// ПроцИзмен
        /// Относительная величина изменения стоимости всех позиций клиента
        /// </summary>
        [JsonProperty("rate_change")]
        public decimal RateChange { get; set; }

        /// <summary>
        /// На покупку
        /// Оценка денежных средств, доступных для покупки маржинальных бумаг
        /// </summary>
        [JsonProperty("lim_buy")]
        public decimal LimBuy { get; set; }

        /// <summary>
        /// На продажу
        /// Оценка стоимости маржинальных бумаг, доступных для продажи
        /// </summary>
        [JsonProperty("lim_sell")]
        public decimal LimSell { get; set; }

        /// <summary>
        /// НаПокупНеМаржин
        /// Оценка денежных средств, доступных для покупки немаржинальных бумаг
        /// </summary>
        [JsonProperty("lim_non_margin")]
        public decimal LimNonMargin { get; set; }

        /// <summary>
        /// НаПокупОбесп
        /// Оценка денежных средств, доступных для покупки бумаг, принимаемых в обеспечение
        /// </summary>
        [JsonProperty("lim_buy_asset")]
        public decimal LimBuyAsset { get; set; }

        /// <summary>
        /// Шорты (нетто)
        /// Оценка стоимости коротких позиций. При расчете не используется коэффициент дисконтирования
        /// </summary>
        [JsonProperty("val_short_net")]
        public decimal ValShortNet { get; set; }

        /// <summary>
        /// Сумма ден. остатков
        /// Сумма остатков по денежным средствам по всем лимитам, без учета средств, заблокированных под исполнение обязательств, выраженная в выбранной валюте расчета
        /// </summary>
        [JsonProperty("total_money_bal")]
        public decimal TotalMoneyBal { get; set; }

        /// <summary>
        /// Суммарно заблок.
        /// Cумма заблокированных средств со всех денежных лимитов клиента, пересчитанная в валюту расчетов через кросс-курсы на сервере
        /// </summary>
        [JsonProperty("total_locked_money")]
        public decimal TotalLockedMoney { get; set; }

        /// <summary>
        /// Сумма дисконтов
        /// Сумма дисконтов стоимости длинных (только по бумагам обеспечения) и коротких бумажных позиций, дисконтов корреляции между инструментами, а также дисконтов на задолженности по валютам, не покрытые бумажным обеспечением в этих же валютах
        /// </summary>
        [JsonProperty("haircuts")]
        public string Haircuts { get; set; }

        /// <summary>
        /// ТекАктБезДиск
        /// Суммарная величина денежных остатков, стоимости длинных позиций по бумагам обеспечения и стоимости коротких позиций, без учета дисконтирующих коэффициентов, без учета неттинга стоимости бумаг в рамках объединенной бумажной позиции и без учета корреляции между инструментами
        /// </summary>
        [JsonProperty("assets_without_hc")]
        public decimal AssetsWithoutHC { get; set; }

        /// <summary>
        /// Статус счета
        /// Отношение суммы дисконтов к текущим активам без учета дисконтов
        /// </summary>
        [JsonProperty("status_coef")]
        public decimal StatusCoef { get; set; }

        /// <summary>
        /// Вариац. маржа
        /// Текущая вариационная маржа по позициям клиента, по всем инструментам
        /// </summary>
        [JsonProperty("varmargin")]
        public decimal VarMargin { get; set; }

        /// <summary>
        /// ГО поз.
        /// Размер денежных средств, уплаченных под все открытые позиции на срочном рынке
        /// </summary>
        [JsonProperty("go_for_positions")]
        public decimal GOForPositions { get; set; }

        /// <summary>
        /// ГО заяв.
        /// Оценка стоимости активов в заявках на срочном рынке
        /// </summary>
        [JsonProperty("go_for_orders")]
        public decimal GOForOrders { get; set; }

        /// <summary>
        /// Активы/ГО
        /// Отношение ликвидационной стоимости портфеля к ГО по срочному рынку
        /// </summary>
        [JsonProperty("rate_futures")]
        public decimal RateFutures { get; set; }

        /// <summary>
        /// ПовышУрРиска
        /// Признак «квалифицированного» клиента, которому разрешено кредитование заемными средствами с плечом 1:3.
        /// Возможные значения: «ПовышУрРиска» – квалифицированный, /пусто/ – нет
        /// </summary>
        [JsonProperty("is_qual_client")]
        public string IsQualClient { get; set; }

        /// <summary>
        /// Сроч. счет
        /// Счет клиента на FORTS, в случае наличия объединенной позиции, иначе поле остается пустым
        /// </summary>
        [JsonProperty("is_futures")]
        public string IsFutures { get; set; }

        /// <summary>
        /// Парам. расч.
        /// Актуальные текущие параметры расчета для данной строки в формате «/Валюта/-/Идентификатор торговой сессии/». Пример: «SUR-EQTV»
        /// </summary>
        [JsonProperty("curr_TAG")]
        public string CurrTAG { get; set; }

        /// <summary>
        /// Нач.маржа
        /// Значение начальной маржи. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("init_margin")]
        public decimal InitMargin { get; set; }

        /// <summary>
        /// Мин.маржа
        /// Значение минимальной маржи. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("min_margin")]
        public decimal MinMargin { get; set; }

        /// <summary>
        /// Скор.маржа
        /// Значение скорректированной маржи. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("corrected_margin")]
        public decimal CorrectedMargin { get; set; }

        /// <summary>
        /// Тип клиента
        /// </summary>
        [JsonProperty("client_type")]
        public string ClientType { get; set; }

        /// <summary>
        /// Стоимость портфеля
        /// Стоимость портфеля. Для клиентов типа «МД» возвращается значение для строк с максимальным видом лимита limit_kind
        /// </summary>
        [JsonProperty("portfolio_value")]
        public decimal PortfolioValue { get; set; }

        /// <summary>
        /// ЛимОткрПозНачДня
        /// Лимит открытых позиций на начало дня
        /// </summary>
        [JsonProperty("start_limit_open_pos")]
        public decimal StartLimitOpenPos { get; set; }

        /// <summary>
        /// ЛимОткрПоз
        /// Лимит открытых позиций
        /// </summary>
        [JsonProperty("total_limit_open_pos")]
        public decimal TotalLimitOpenPos { get; set; }

        /// <summary>
        /// ПланЧистПоз
        /// Планируемые чистые позиции
        /// </summary>
        [JsonProperty("limit_open_pos")]
        public decimal LimitOpenPos { get; set; }

        /// <summary>
        /// ТекЧистПоз
        /// Текущие чистые позиции
        /// </summary>
        [JsonProperty("used_lim_open_pos")]
        public decimal UsedLimOpenPos { get; set; }

        /// <summary>
        /// НакопВарМаржа
        /// Накопленная вариационная маржа
        /// </summary>
        [JsonProperty("acc_var_margin")]
        public decimal AccVarMargin { get; set; }

        /// <summary>
        /// ВарМаржаПромклир
        /// Вариационная маржа по итогам промклиринга
        /// </summary>
        [JsonProperty("cl_var_margin")]
        public decimal ClVarMargin { get; set; }

        /// <summary>
        /// ЛиквСтоимОпционов
        /// Ликвидационная стоимость опционов
        /// </summary>
        [JsonProperty("opt_liquid_cost")]
        public decimal OptLiquidCost { get; set; }

        /// <summary>
        /// СумАктивовНаСрчРынке
        /// Сумма оценки средств клиента на срочном рынке
        /// </summary>
        [JsonProperty("fut_asset")]
        public decimal FutAsset { get; set; }

        /// <summary>
        /// ПолнСтоимостьПортфеля
        /// Сумма оценки собственных средств клиента на фондовом и срочном рынках
        /// </summary>
        [JsonProperty("fut_total_asset")]
        public decimal FutTotalAsset { get; set; }

        /// <summary>
        /// ТекЗадолжНаСрчРынке
        /// Текущая задолженность на срочном рынке
        /// </summary>
        [JsonProperty("fut_debt")]
        public decimal FutDebt { get; set; }

        /// <summary>
        /// Дост. Средств
        /// Достаточность средств
        /// </summary>
        [JsonProperty("fut_rate_asset")]
        public decimal FutRateAsset { get; set; }

        /// <summary>
        /// Дост. Средств (ОткрПоз)
        /// Достаточность средств (под открытые позиции)
        /// </summary>
        [JsonProperty("fut_rate_asset_open")]
        public decimal FutRateAssetOpen { get; set; }

        /// <summary>
        /// КоэффЛикв ГО
        /// Коэффициент ликвидности ГО
        /// </summary>
        [JsonProperty("fut_rate_go")]
        public decimal FutRateGO { get; set; }

        /// <summary>
        /// Ожид. КоэффЛикв ГО
        /// Ожидаемый коэффициент ликвидности ГО
        /// </summary>
        [JsonProperty("planed_rate_go")]
        public decimal PlanedRateGO { get; set; }

        /// <summary>
        /// Cash Leverage
        /// </summary>
        [JsonProperty("cash_leverage")]
        public decimal CashLeverage { get; set; }

        /// <summary>
        /// ТипПозНаСрчРынке
        /// Тип позиции на срочном рынке. Возможные значения
        /// «0» – нет позиции;
        /// «1» – фьючерсы;
        /// «2» – опционы;
        /// «3» – фьючерсы и опционы
        /// </summary>
        [JsonProperty("fut_position_type")]
        public decimal FutPositionType { get; set; }

        /// <summary>
        /// НакопДоход
        /// Накопленный доход с учётом премии по опционам и биржевым сборам
        /// </summary>
        [JsonProperty("fut_accured_int")]
        public decimal FutAccuredInt { get; set; }
    }
}