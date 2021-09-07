// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System;
using System.Data;

namespace QUIKSharp.TestQuik
{
    /// <summary>
    /// 
    /// </summary>
    public static class DataSetHelper
    {

        /// <summary>
        /// Создает в таблице столбцы с типами и именами свойств и значений обьекта, доступных для чтения
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="type"></param>
        public static void CreateDataTableforObject(DataTable dt, Type type)
        {
            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead) continue;
                if (!dt.Columns.Contains(property.Name))
                {
                    //                    var dc = dt.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                    Type column_type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (column_type == typeof(QuikDateTime))
                        column_type = typeof(DateTime);

                    if (!(column_type.IsPrimitive || column_type.IsEnum || column_type.IsValueType || (column_type == typeof(string)) || (column_type == typeof(DateTime)) || (column_type == typeof(TimeSpan))))
                        continue;

                    var dc = dt.Columns.Add(property.Name, column_type);
                    dc.AllowDBNull = true;
                    dc.DefaultValue = null;
                }
            }

            foreach (var field in type.GetFields())
            {
                if (!field.IsPublic) continue;
                Type column_type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                if (column_type == typeof(QuikDateTime))
                    column_type = typeof(DateTime);

                if (!(column_type.IsPrimitive || column_type.IsEnum || column_type.IsValueType || (column_type == typeof(string)) || (column_type == typeof(DateTime)) || (column_type == typeof(TimeSpan))))
                    continue;

                if (!dt.Columns.Contains(field.Name))
                {
                    var dc = dt.Columns.Add(field.Name, column_type);
                    dc.AllowDBNull = true;
                    dc.DefaultValue = null;
                }
            }
        }
        /// <summary>
        /// Создает новую строку таблицы и заполняет ее значениями полей и свойств обьекта
        /// 1. DataTable.NewRow()
        /// 2. fillDataRow
        /// 3. DataTable.Rows.Add
        /// 4. DataTable.AcceptChanges
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static DataRow NewDataRow(this DataTable dataTable, object o)
        {
            var row = dataTable.NewRow();
            FillDataRow(row, o);
            dataTable.Rows.Add(row);
            dataTable.AcceptChanges();
            return row;
        }
        /// <summary>
        /// Заполняет строку таблицы значениями полей и свойств обьекта
        /// 1. BeginEdit
        /// 2. fillDataRow
        /// 3. AcceptChanges
        /// </summary>
        /// <param name="row"></param>
        /// <param name="o"></param>
        public static void UpdateDataRow(this DataRow row, object o)
        {
            row.BeginEdit();
            FillDataRow(row, o);
            row.AcceptChanges();
        }
        /// <summary>
        /// Заполняет строку таблицы значениями полей и свойств обьекта
        /// </summary>
        /// <param name="row"></param>
        /// <param name="o"></param>
        public static void FillDataRow(this DataRow row, object o)
        {
            var type = o.GetType();
            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead) continue;
                if (!row.Table.Columns.Contains(prop.Name)) continue;
                var col = row.Table.Columns[prop.Name];

                object value = prop.GetValue(o);
                Type prop_type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (prop_type == typeof(QuikDateTime))
                {
                    value = ((QuikDateTime)value).ToDateTime();
                }
                row[col] = value;
            }

            foreach (var field in type.GetFields())
            {
                if (!field.IsPublic) continue;
                if (!row.Table.Columns.Contains(field.Name)) continue;
                var col = row.Table.Columns[field.Name];

                object value = field.GetValue(o);
                Type prop_type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                if (prop_type == typeof(QuikDateTime))
                {
                    value = ((QuikDateTime)value).ToDateTime();
                }
                row[col] = value;
            }
        }

        /// <summary>
        /// Заполняет значения полей и свойств обьекта из строки таблицы
        /// </summary>
        /// <param name="row"></param>
        /// <param name="obj"></param>
        public static bool FillObjectFromDataRow(this DataRow row, object obj)
        {
            var ObjectType = obj.GetType();
            if (row.Table.Columns.Contains("ClassType"))
            {
                DataColumn col = row.Table.Columns["ClassType"];
                var type_name = row[col].ToString();
                if (string.Compare(ObjectType.Name, type_name, true) != 0)
                    return false;
            }

            foreach (DataColumn col in row.Table.Columns)
            {
                var prop = ObjectType.GetProperty(col.ColumnName);
                if ((prop != null) && prop.CanWrite)
                {
                    var value = row[col];
                    if (prop.PropertyType == typeof(QUIKSharp.DataStructures.QuikDateTime))
                        value = new QuikDateTime((DateTime)value);
                    prop.SetValue(obj, value);

                }
                var field = ObjectType.GetField(col.ColumnName);
                if ((field != null) && field.IsPublic)
                {
                    var value = row[col];
                    if (field.FieldType == typeof(QUIKSharp.DataStructures.QuikDateTime))
                        value = new QuikDateTime((DateTime)value);
                    field.SetValue(obj, value);
                }
            }
            return true;
        }
        /// <summary>
        /// Загружает датасет из XML
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dataSet1"></param>
        ///        
        public static bool LoadData(this DataSet dataSet1, string filename)
        {
            try
            {
                dataSet1.Clear();
                // Read the XML document back in.
                // Create new FileStream to read schema with.
                System.IO.FileStream streamRead = new System.IO.FileStream(filename, System.IO.FileMode.Open);
                dataSet1.ReadXml(streamRead);
                streamRead.Close();
                return true;
            }
            catch 
            {
                return false;
            }
        }
        /// <summary>
        /// Загружает датасет из XML
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="datatable"></param>
        ///        
        public static bool LoadData(this DataTable datatable, string filename)
        {
            try
            {
                datatable.Clear();
                // Read the XML document back in.
                // Create new FileStream to read schema with.
                System.IO.FileStream streamRead = new System.IO.FileStream (filename, System.IO.FileMode.Open);
                datatable.ReadXml(streamRead);
                streamRead.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Сохраняет датасет в XML
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dataSet1"></param>
        public static void SaveData(this DataSet dataSet1, string filename)
        {
                // Create the FileStream to write with.
                System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Create);
                // Create an XmlTextWriter with the fileStream.
                System.Xml.XmlTextWriter xmlWriter = new System.Xml.XmlTextWriter(stream, System.Text.Encoding.Unicode);
                // Write to the file with the WriteXml method.
                dataSet1.WriteXml(xmlWriter);
                xmlWriter.Close();
        }

        /// <summary>
        /// Сохраняет DataTable в XML
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="datatable"></param>
        public static void SaveData(this DataTable datatable, string filename)
        {
            // Create the FileStream to write with.
            System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Create);

            // Create an XmlTextWriter with the fileStream.
            System.Xml.XmlTextWriter xmlWriter = new System.Xml.XmlTextWriter(stream, System.Text.Encoding.Unicode);

            // Write to the file with the WriteXml method.
            datatable.WriteXml(xmlWriter);
            xmlWriter.Close();
        }
    }

}
