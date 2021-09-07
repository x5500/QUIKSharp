﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Наименования параметров для функции GetParamEx и GetParamEx2
    /// </summary>
    public enum ParamNames
    {
        /// <summary>
        /// Код бумаги
        /// </summary>
        CODE,

        /// <summary>
        /// Код класса
        /// </summary>
        CLASS_CODE,

        /// <summary>
        /// Краткое название бумаги
        /// </summary>
        SHORTNAME,

        /// <summary>
        /// Полное зназвание бумаги
        /// </summary>
        LONGNAME,

        /// <summary>
        /// Кратность лота
        /// </summary>
        LOT,

        /// <summary>
        /// Тип инструмента
        /// </summary>
        SECTYPESTATIC,

        /// <summary>
        /// Тип фьючерса
        /// </summary>
        FUTURETYPE,

        /// <summary>
        /// Минимальный шаг цены
        /// </summary>
        SEC_PRICE_STEP,

        /// <summary>
        /// Название класса
        /// </summary>
        CLASSNAME,

        /// <summary>
        /// Размер лота
        /// </summary>
        LOTSIZE,

        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        STEPPRICET,

        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        STEPPRICE,

        /// <summary>
        /// Стоимость шага цены для клиринга
        /// </summary>
        STEPPRICECL,

        /// <summary>
        /// Стоимость шага цены для промклиринга
        /// </summary>
        STEPPRICEPRCL,

        /// <summary>
        /// Точность цены
        /// </summary>
        SEC_SCALE,

        /// <summary>
        /// Цена закрытия
        /// </summary>
        PREVPRICE,

        /// <summary>
        /// Цена первой сделки в текущей сессии
        /// </summary>
        FIRSTOPEN,

        /// <summary>
        /// Цена последней сделки
        /// </summary>
        LAST,

        /// <summary>
        /// Цена последней сделки в текущей сессии
        /// </summary>
        LASTCLOSE,

        /// <summary>
        /// Время последней сделки
        /// </summary>
        TIME,

        /// <summary>
        /// Базовый актив
        /// </summary>
        OPTIONBASE,

        /// <summary>
        /// Класс базового актива
        /// </summary>
        OPTIONBASECLASS,

        /// <summary>
        /// Валюта номинала
        /// </summary>
        SEC_FACE_UNIT,

        /// <summary>
        /// Валюта шага цены
        /// </summary>
        CURSTEPPRICE,

        /// <summary>
        /// Лучшая цена предложения
        /// </summary>
        OFFER,

        /// <summary>
        /// Лучшая цена спроса
        /// </summary>
        BID,

        /// <summary>
        /// Количество заявок на покупку
        /// </summary>
        NUMBIDS,

        /// <summary>
        /// Количество заявок на продажу
        /// </summary>
        NUMOFFERS,

        /// <summary>
        /// Спрос по лучшей цене
        /// </summary>
        BIDDEPTH,

        /// <summary>
        /// Предложение по лучшей цене
        /// </summary>
        OFFERDEPTH,

        /// <summary>
        /// Суммарный спрос
        /// </summary>
        BIDDEPTHT,

        /// <summary>
        /// Суммарное предложение
        /// </summary>
        OFFERDEPTHT,

        /// <summary>
        /// Максимальная цена сделки
        /// </summary>
        HIGH,

        /// <summary>
        /// Минимальная цена сделки
        /// </summary>
        LOW,

        /// <summary>
        /// Максимально возможная цена
        /// </summary>
        PRICEMAX,

        /// <summary>
        /// Минимально возможная цена
        /// </summary>
        PRICEMIN,

        /// <summary>
        /// Количество открытых позиций
        /// </summary>
        NUMCONTRACTS,

        /// <summary>
        /// Гарантийное обеспечение покуптеля
        /// </summary>
        BUYDEPO,

        /// <summary>
        /// Гарантийное обеспечение продавца
        /// </summary>
        SELLDEPO,

        /// <summary>
        /// Номинал бумаги
        /// </summary>
        SEC_FACE_VALUE,

        /// <summary>
        /// Дата исполнения инструмента
        /// </summary>
        EXPDATE,

        /// <summary>
        /// Дата погашения
        /// </summary>
        MAT_DATE,

        /// <summary>
        /// Число дней до погашения
        /// </summary>
        DAYS_TO_MAT_DATE,

        /// <summary>
        /// Начало утренней сессии
        /// </summary>
        MONSTARTTIME,

        /// <summary>
        /// Окончание утренней сессии
        /// </summary>
        MONENDTIME,

        /// <summary>
        /// Начало вечерней сессии
        /// </summary>
        EVNSTARTTIME,

        /// <summary>
        /// Окончание вечерней сессии
        /// </summary>
        EVNENDTIME,

        /// <summary>
        /// Состояние сессии
        /// </summary>
        TRADINGSTATUS,

        /// <summary>
        /// Статус клиринга
        /// </summary>
        CLSTATE,

        /// <summary>
        /// Статус торговли инструментом
        /// </summary>
        STATUS,

        /// <summary>
        /// Дата торгов
        /// </summary>
        TRADE_DATE_CODE,

        /// <summary>
        /// Bloomberg ID
        /// </summary>
        BSID,

        /// <summary>
        /// CFI
        /// </summary>
        CFI_CODE,

        /// <summary>
        /// CUSIP
        /// </summary>
        CUSIP,

        /// <summary>
        /// ISIN
        /// </summary>
        ISINCODE,

        /// <summary>
        /// RIC
        /// </summary>
        RIC,

        /// <summary>
        /// SEDOL
        /// </summary>
        SEDOL,

        /// <summary>
        /// StockCode
        /// </summary>
        STOCKCODE,

        /// <summary>
        /// StockName
        /// </summary>
        STOCKNAME,

        /// <summary>
        /// Агрегированная ставка
        /// </summary>
        PERCENTRATE,

        /// <summary>
        /// Анонимная торговля
        /// </summary>
        ANONTRADE,

        /// <summary>
        /// Биржевой сбор (возможно, исключен из активных параметров)
        /// </summary>
        EXCH_PAY,

        /// <summary>
        /// Время начала аукциона
        /// </summary>
        STARTTIME,

        /// <summary>
        /// Время окончания аукциона
        /// </summary>
        ENDTIME,

        /// <summary>
        /// Время последнего изменения
        /// </summary>
        CHANGETIME,

        /// <summary>
        /// Дисконт1
        /// </summary>
        DISCOUNT1,

        /// <summary>
        /// Дисконт2
        /// </summary>
        DISCOUNT2,

        /// <summary>
        /// Дисконт3
        /// </summary>
        DISCOUNT3,

        /// <summary>
        /// Количество в последней сделке
        /// </summary>
        QTY,

        /// <summary>
        /// Количество во всех сделках
        /// </summary>
        VOLTODAY,

        /// <summary>
        /// Количество сделок за сегодня
        /// </summary>
        NUMTRADES,

        /// <summary>
        /// Комментарий
        /// </summary>
        SEC_COMMENT,

        /// <summary>
        /// Котировка последнего клиринга
        /// </summary>
        CLPRICE,

        /// <summary>
        /// Оборот в деньгах
        /// </summary>
        VALTODAY,

        /// <summary>
        /// Оборот в деньгах последней сделки
        /// </summary>
        VALUE,

        /// <summary>
        /// Пердыдущая оценка
        /// </summary>
        PREVWAPRICE,

        /// <summary>
        /// Подтип инструмента
        /// </summary>
        SECSUBTYPESTATIC,

        /// <summary>
        /// Предыдущая расчетная цена
        /// </summary>
        PREVSETTLEPRICE,

        /// <summary>
        /// Предыдущий расчетный объем
        /// </summary>
        PREVSETTLEVOL,

        /// <summary>
        /// Процент изменения от закрытия
        /// </summary>
        LASTCHANGE,

        /// <summary>
        /// Разница цены последней к предыдущей сделке
        /// </summary>
        TRADECHANGE,

        /// <summary>
        /// Разница цены последней к предыдущей сессии
        /// </summary>
        CHANGE,

        /// <summary>
        /// Расчетная цена
        /// </summary>
        SETTLEPRICE,

        /// <summary>
        /// Реальная расчетная цена
        /// </summary>
        R_SETTLEPRICE,

        /// <summary>
        /// Регистрационный номер (возможно, исключен из активных параметров)
        /// </summary>
        REGNUMBER,

        /// <summary>
        /// Средневзвешенная цена
        /// </summary>
        WAPRICE,

        /// <summary>
        /// Текущая рыночная котировка
        /// </summary>
        REALVMPRICE,

        /// <summary>
        /// Тип
        /// </summary>
        SECTYPE,

        /// <summary>
        /// Тип цены фьючерса
        /// </summary>
        ISPERCENT,

        /// <summary>
        /// Issuer
        /// </summary>
        FIRM_SHORT_NAME,

        /// <summary>
        /// Duration (дюрация)
        /// </summary>
        DURATION,

        /// <summary>
        /// YieldMaturity (Доходность к погашению)
        /// </summary>
        YIELD,

        /// <summary>
        /// Купон (размер/стоимость)
        /// </summary>
        COUPONVALUE,

        /// <summary>
        /// Периодичность выплаты купонов
        /// </summary>
        COUPONPERIOD,

        /// <summary>
        /// Дата ближайшей выплаты купона
        /// </summary>
        NEXTCOUPON,

        /// <summary>
        /// Точные кол-ва
        /// </summary>
        QTY_SCALE,

        /// <summary>
        /// Агент по размещению
        /// </summary>
        AGENT_ID,

        /// <summary>
        /// Макс.акт.точ.кол
        /// </summary>
        MAX_ACT_QTYSCALE,

        /// <summary>
        /// Cтоимость шага в валюте
        /// </summary>
        STEP_IN_CURRENCY,

        /// <summary>
        /// % изменения к открытию
        /// </summary>
        OPENPCTCHANGE,

        /// <summary>
        /// Огран.отриц.цен
        /// </summary>
        NEGATIVEPRC,

        /// <summary>
        /// Открытие
        /// </summary>
        OPEN,

        /// <summary>
        /// Лучший спрос
        /// </summary>
        HIGHBID,

        /// <summary>
        /// Лучшее предложение
        /// </summary>
        LOWOFFER,

        /// <summary>
        /// Закрытие
        /// </summary>
        CLOSEPRICE,

        /// <summary>
        /// Вчерашняя рыночная цена
        /// </summary>
        MARKETPRICE,

        /// <summary>
        /// Рыночная цена
        /// </summary>
        MARKETPRICETODAY,

        /// <summary>
        /// Объем обр.
        /// </summary>
        ISSUESIZE,

        /// <summary>
        /// Официальная текущая цена
        /// </summary>
        LCURRENTPRICE,

        /// <summary>
        /// Официальная цена закрытия
        /// </summary>
        LCLOSEPRICE,

        /// <summary>
        /// Тип цены
        /// </summary>
        QUOTEBASIS,

        /// <summary>
        /// Призн.котир.
        /// </summary>
        ADMITTEDQUOTE,

        /// <summary>
        /// Призн.кот.пред.
        /// </summary>
        PREVADMITTEDQUOT,

        /// <summary>
        /// Спрос сессии
        /// </summary>
        LASTBID,

        /// <summary>
        /// Предложение сессии
        /// </summary>
        LASTOFFER,

        /// <summary>
        /// Рыночная цена2
        /// </summary>
        MARKETPRICE2,

        /// <summary>
        /// Предыдущая цена закрытия
        /// </summary>
        PREVLEGALCLOSEPR,

        /// <summary>
        /// Цена предторг.
        /// </summary>
        OPENPERIODPRICE,

        /// <summary>
        /// Мининимальная тек цена
        /// </summary>
        MIN_CURR_LAST,

        /// <summary>
        /// Код расчетов
        /// </summary>
        SETTLECODE,

        /// <summary>
        /// Вр. изм.м.т.ц.
        /// </summary>
        MIN_CURR_LAST_TI,

        /// <summary>
        /// Объем в обращении
        /// </summary>
        ISSUESIZEPLACED,

        /// <summary>
        /// Дата расчетов
        /// </summary>
        SETTLEDATE,

        /// <summary>
        /// Сопр.валюта
        /// </summary>
        CURRENCYID,

        /// <summary>
        /// Листинг
        /// </summary>
        LISTLEVEL,

        /// <summary>
        /// Размещение IPO
        /// </summary>
        PRIMARYDIST,

        /// <summary>
        /// Квалифицированный инвестор
        /// </summary>
        QUALIFIED,

        /// <summary>
        /// Дополнительная сессия
        /// </summary>
        EV_SESS_ALLOWED,

        /// <summary>
        /// П.И.Р.
        /// </summary>
        HIGH_RISK,

        /// <summary>
        /// Дата последних торгов
        /// </summary>
        PREVDATE,

        /// <summary>
        /// Цена контраг.
        /// </summary>
        COUNTERPRICE,

        /// <summary>
        /// Начало аукциона план
        /// </summary>
        PLANNEDTIME,

        /// <summary>
        /// Цена аукциона
        /// </summary>
        AUCTPRICE,

        /// <summary>
        /// Объем аукциона
        /// </summary>
        AUCTVALUE,

        /// <summary>
        /// Количество аукциона
        /// </summary>
        AUCTVOLUME,

        /// <summary>
        /// Количество сд.аукц.
        /// </summary>
        AUCTNUMTRADES,

        /// <summary>
        /// Дисбаланс ПА
        /// </summary>
        IMBALANCE,

        /// <summary>
        /// Рын.пок.
        /// </summary>
        MARKETVOLB,

        /// <summary>
        /// Рын.прод.
        /// </summary>
        MARKETVOLS,

        /// <summary>
        /// БГОП
        /// </summary>
        BGOP,

        /// <summary>
        /// БГОНП
        /// </summary>
        BGONP,

        /// <summary>
        /// Страйк
        /// </summary>
        STRIKE,

        /// <summary>
        /// Тип опциона
        /// </summary>
        OPTIONTYPE,

        /// <summary>
        /// Волатильность
        /// </summary>
        VOLATILITY,

        /// <summary>
        /// Теоретическая цена
        /// </summary>
        THEORPRICE,

        /// <summary>
        /// Марж.
        /// </summary>
        MARG,

        /// <summary>
        /// Разн. опц.
        /// </summary>
        OPTIONKIND,

        /// <summary>
        /// Суммарный объем премии
        /// </summary>
        TOTALPREMIUMVOL,

        /// <summary>
        /// Базовая валюта
        /// </summary>
        FIRST_CUR,

        /// <summary>
        /// Котир.валюта
        /// </summary>
        SECOND_CUR,

        /// <summary>
        /// Минимальное количество
        /// </summary>
        MINQTY,

        /// <summary>
        /// Максимальное количество
        /// </summary>
        MAXQTY,

        /// <summary>
        /// Минимальный шаг объема
        /// </summary>
        STEPQTY,

        /// <summary>
        /// Измемение к предыдущей оценке
        /// </summary>
        PRICEMINUSPREVWA,

        /// <summary>
        /// Базовый курс
        /// </summary>
        BASEPRICE,

        /// <summary>
        /// Дата расчетов 1
        /// </summary>
        SETTLEDATE1,

        /// <summary>
        /// Биржевая Сессия
        /// </summary>
        TRADINGPHASE,

        /// <summary>
        /// Заявок покупателей АКП
        /// </summary>
        DPVALINDICATORBU,

        /// <summary>
        /// Заявок продавцов АКП
        /// </summary>
        DPVALINDICATORSE,

        /// <summary>
        /// Курс
        /// </summary>
        CROSSRATE,

        /// <summary>
        /// Значение
        /// </summary>
        CURRENTVALUE,

        /// <summary>
        /// Значение закрытия
        /// </summary>
        LASTVALUE,

        /// <summary>
        /// Минимум
        /// </summary>
        MIN,

        /// <summary>
        /// Максимум
        /// </summary>
        MAX,

        /// <summary>
        /// Открытие
        /// </summary>
        OPENVALUE,

        /// <summary>
        /// % изменение
        /// </summary>
        PCHANGE,

        /// <summary>
        /// Открытие
        /// </summary>
        IOPEN,

        /// <summary>
        /// Мин.
        /// </summary>
        LOWVAL,

        /// <summary>
        /// Макс.
        /// </summary>
        HIGHVAL,

        /// <summary>
        /// Капитал. бумаг
        /// </summary>
        ICAPITAL,

        /// <summary>
        /// Объем инд.сдел.
        /// </summary>
        IVOLUME,

        /// <summary>
        /// НКД
        /// </summary>
        ACCRUEDINT,

        /// <summary>
        /// Доходность пред.оц.
        /// </summary>
        YIELDATPREVWAPRI,

        /// <summary>
        /// Доходность оц.
        /// </summary>
        YIELDATWAPRICE,

        /// <summary>
        /// Доходность закр.
        /// </summary>
        CLOSEYIELD,

        /// <summary>
        /// Оферта
        /// </summary>
        BUYBACKPRICE,

        /// <summary>
        /// Дата расч.доход
        /// </summary>
        BUYBACKDATE,

        /// <summary>
        /// Тип цены обл.
        /// </summary>
        OBLPERCENT,

        /// <summary>
        /// Суборд инстр-т
        /// </summary>
        SUBORDINATEDINST,

        /// <summary>
        /// Неточ. параметры
        /// </summary>
        BONDSREMARKS,

        /// <summary>
        /// NUMERIC	Разница цены последней к предыдущей оценке
        /// </summary>
        PRICEMINUSPREVWAPRICE,

        /// <summary>
        /// NUMERIC	Разница цены последней к предыдущей сессии
        /// </summary>
        LASTTOPREVSTLPRC,

        /// <summary>
        /// 	NUMERIC	Лимит изменения цены
        /// </summary>
        PRICEMVTLIMIT,

        /// <summary>
        /// 	NUMERIC	Лимит изменения цены T1
        /// </summary>
        PRICEMVTLIMITT1,

        /// <summary>
        /// 	NUMERIC	Лимит объема активных заявок (в контрактах)
        /// </summary>
        MAXOUTVOLUME,

        /// <summary>
        /// NUMERIC	Оборот внесистемных в деньгах
        /// </summary>
        NEGVALTODAY,

        /// <summary>
        /// NUMERIC	Количество внесистемных сделок за сегодня
        /// </summary>
        NEGNUMTRADES,

        /// <summary>
        /// STRING	Время закрытия предыдущих торгов (для индексов РТС)
        /// </summary>
        CLOSETIME,

        /// <summary>
        /// NUMERIC	Значение индекса РТС на момент открытия торгов
        /// </summary>
        OPENVAL,

        /// <summary>
        /// 	NUMERIC	Изменение текущего индекса РТС по сравнению со значением открытия
        /// </summary>
        CHNGOPEN,

        /// <summary>
        /// NUMERIC	Изменение текущего индекса РТС по сравнению со значением закрытия
        /// </summary>
        CHNGCLOSE,

        /// <summary>
        /// NUMERIC	Доходность продажи
        /// </summary>
        SELLPROFIT,

        /// <summary>
        /// 	NUMERIC	Доходность покупки
        /// </summary>
        BUYPROFIT,

        /// <summary>
        /// NUMERIC	Номинал (для инструментов СПВБ)
        /// </summary>
        FACEVALUE,

        /// <summary>
        /// 	NUMERIC	Официальная цена открытия
        /// </summary>
        LOPENPRICE,

        /// <summary>
        /// NUMERIC	Изменение (RTSIND)
        /// </summary>
        ICHANGE,

        /// <summary>
        /// DOUBLE	Предыдущее значение размера лота
        /// </summary>
        PREVLOTSIZE,

        /// <summary>
        /// 	DOUBLE	Дата последнего изменения размера лота
        /// </summary>
        LOTSIZECHANGEDAT,

        /// <summary>
        /// NUMERIC	Количество в сделках послеторгового аукциона
        /// </summary>
        CLOSING_AUCTION_VOLUME,
    }
}