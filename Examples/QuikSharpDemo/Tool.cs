﻿using System;
using QUIKSharp;
using QUIKSharp.DataStructures;

public class Tool   : ISecurity
{
    readonly Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
    readonly Quik _quik;
    string name;
    //string clientCode;
    string accountID;
    string firmID;
    int lot;
    int priceAccuracy;
    double guaranteeProviding;
    decimal step;
    decimal lastPrice;

    #region Свойства
    /// <summary>
    /// Краткое наименование инструмента (бумаги)
    /// </summary>
    public string Name { get { return name; } }
    /// <summary>
    /// Код инструмента (бумаги)
    /// </summary>
    public string SecCode { get; private set; }
    /// <summary>
    /// Код класса инструмента (бумаги)
    /// </summary>
    public string ClassCode { get; private set; }
    /// <summary>
    /// Счет клиента
    /// </summary>
    public string AccountID { get { return accountID; } }
    /// <summary>
    /// Код фирмы
    /// </summary>
    public string FirmID { get { return firmID; } }
    /// <summary>
    /// Количество акций в одном лоте
    /// Для инструментов класса SPBFUT = 1
    /// </summary>
    public int Lot { get { return lot; } }
    /// <summary>
    /// Точность цены (количество знаков после запятой)
    /// </summary>
    public int PriceAccuracy { get { return priceAccuracy; } }
    /// <summary>
    /// Шаг цены
    /// </summary>
    public decimal Step { get { return step; } }
    /// <summary>
    /// Гарантийное обеспечение (только для срочного рынка)
    /// для фондовой секции = 0
    /// </summary>
    public double GuaranteeProviding { get { return guaranteeProviding; } }
    /// <summary>
    /// Цена последней сделки
    /// </summary>
    public decimal LastPrice
    {
        get
        {
            lastPrice = Convert.ToDecimal(_quik.Trading.GetParamEx(this, ParamNames.LAST).Result.ParamValue.Replace('.', separator));
            return lastPrice;
        }
    }
    #endregion

    /// <summary>
    /// Конструктор класса
    /// </summary>
    /// <param name="_quik"></param>
    /// <param name="securityCode">Код инструмента</param>
    /// <param name="classCode">Код класса</param>
    public Tool(Quik quik, string securityCode_, string _classCode)
    {
        _quik = quik;
        GetBaseParam(quik, securityCode_, _classCode);
    }

    void GetBaseParam(Quik quik, string secCode, string _classCode)
    {
        try
        {
            SecCode = secCode;
            ClassCode = _classCode;
            if (quik != null)
            {
                if (ClassCode != null && ClassCode != "")
                {
                    try
                    {
                        name = quik.Class.GetSecurityInfo(ClassCode, SecCode).Result.ShortName;
                        accountID = quik.Class.GetTradeAccount(ClassCode).Result;
                        firmID = quik.Class.GetClassInfo(ClassCode).Result.FirmId;
                        //step = Convert.ToDecimal(quik.Trading.GetParamEx(classCode, securityCode, "SEC_PRICE_STEP").Result.ParamValue.Replace('.', separator));
                        //priceAccuracy = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "SEC_SCALE").Result.ParamValue.Replace('.', separator)));
                        step = Convert.ToDecimal(quik.Trading.GetParamEx(this, ParamNames.SEC_PRICE_STEP).Result.ParamValue.Replace('.', separator));
                        priceAccuracy = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.SEC_SCALE).Result.ParamValue.Replace('.', separator)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Tool.GetBaseParam. Ошибка получения наименования для " + SecCode + ": " + e.Message);
                    }

                    if (ClassCode == "SPBFUT")
                    {
                        Console.WriteLine("Получаем 'guaranteeProviding'.");
                        lot = 1;
                        //guaranteeProviding = Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "BUYDEPO").Result.ParamValue.Replace('.', separator));
                        guaranteeProviding = Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.BUYDEPO).Result.ParamValue.Replace('.', separator));
                    }
                    else
                    {
                        Console.WriteLine("Получаем 'lot'.");
                        //lot = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "LOTSIZE").Result.ParamValue.Replace('.', separator)));
                        lot = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.LOTSIZE).Result.ParamValue.Replace('.', separator)));
                        guaranteeProviding = 0;
                    }
                }
                else
                {
                    Console.WriteLine("Tool.GetBaseParam. Ошибка: classCode не определен.");
                    lot = 0;
                    guaranteeProviding = 0;
                }
            }
            else
            {
                Console.WriteLine("Tool.GetBaseParam. quik = null !");
            }
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("Ошибка NullReferenceException в методе GetBaseParam: " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка в методе GetBaseParam: " + e.Message);
        }
    }
}
