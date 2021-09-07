using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using QUIKSharp;
using QUIKSharp.DataStructures;
using QUIKSharp.QOrders;

public class Tool : ISecurity
{
    Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
    Quik _quik;
    string name;
    int lot;
    int priceAccuracy;
    double guaranteeProviding;
    decimal priceStep;
    decimal step;
    decimal slip;
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
    public string AccountID { get; private set; }
    /// <summary>
    /// Код фирмы
    /// </summary>
    public string FirmID { get; private set; }
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
    /// Проскальзывание
    /// </summary>
    public decimal Slip { get { return slip; } }
    /// <summary>
    /// Гарантийное обеспечение (только для срочного рынка)
    /// для фондовой секции = 0
    /// </summary>
    public double GuaranteeProviding { get { return guaranteeProviding; } }
    /// <summary>
    /// Стоимость шага цены
    /// </summary>
    public decimal PriceStep { get { return priceStep; } }
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
    /// <param name="koefSlip">Коэффициент проскальзывания</param>
    public Tool(Quik quik, string securityCode_, string _classCode, int koefSlip)
    {
        _quik = quik;
        GetBaseParam(quik, securityCode_, _classCode, koefSlip);
    }

    void GetBaseParam(Quik quik, string secCode, string classCode, int _koefSlip)
    {
        try
        {
            this.SecCode = secCode;
            this.ClassCode = classCode;
            if (quik != null)
            {
                if (classCode != null && classCode != "")
                {
                    try
                    {
                        name = quik.Class.GetSecurityInfo(this).Result.ShortName;
                        this.AccountID = quik.Class.GetTradeAccount(classCode).Result;
                        FirmID = quik.Class.GetClassInfo(classCode).Result.FirmId;
                        step = Convert.ToDecimal(quik.Trading.GetParamEx(this, ParamNames.SEC_PRICE_STEP).Result.ParamValue.Replace('.', separator));
                        slip = _koefSlip * step;
                        priceAccuracy = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.SEC_SCALE).Result.ParamValue.Replace('.', separator)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Tool.GetBaseParam. Ошибка получения наименования для " + this.SecCode + ": " + e.Message);
                    }

                    if (classCode == "SPBFUT")
                    {
                        Console.WriteLine("Получаем 'guaranteeProviding'.");
                        lot = 1;
                        guaranteeProviding = Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.BUYDEPO).Result.ParamValue.Replace('.', separator));
                    }
                    else
                    {
                        Console.WriteLine("Получаем 'lot'.");
                        lot = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(this, ParamNames.LOTSIZE).Result.ParamValue.Replace('.', separator)));
                        guaranteeProviding = 0;
                    }
                    try
                    {
                        priceStep = Convert.ToDecimal(quik.Trading.GetParamEx(this, ParamNames.STEPPRICET).Result.ParamValue.Replace('.', separator));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Instrument.GetBaseParam. Ошибка получения priceStep для " + this.SecCode + ": " + e.Message);
                        priceStep = 0;
                    }
                    if (priceStep == 0) priceStep = step;
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
