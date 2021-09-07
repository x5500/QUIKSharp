// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QUIKSharp.Helpers
{
    public interface IGetParamEx2BulkItem : IStructClassSecParam
    {
        int group { get; }
        void SetValue(ParamTable value);
    }

    public class GetParamEx2Bulk
    {
        public GetParamEx2Bulk(ISecurity ins)
        {
            this.sec = ins;
        }

        public ISecurity sec { get; private set; }
        public List<IGetParamEx2BulkItem> Items { get; private set; } = new List<IGetParamEx2BulkItem>();

        public static void ParseResult(List<IGetParamEx2BulkItem> Items, List<ParamTable> paramTables)
        {
            if (paramTables.Count != Items.Count)
                throw new Exception("Fail to ParseResult GetParamEx2Bulk: paramTables.Count != Items.Count");

            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].SetValue(paramTables[i]);
            }
        }

        public GetParamEx2BulkItem<T> AddNew<T>(ParamNames paramName, int group = 0)
        {
            var item = new GetParamEx2BulkItem<T>(this.sec, paramName, group);
            this.Items.Add(item);
            return item;
        }
        /// <summary>
        /// Выборка элементов определенной группы + нулевая группа
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public List<IGetParamEx2BulkItem> Select(int group)
        {
            var list = new List<IGetParamEx2BulkItem>();
            Items.ForEach((item) =>
            {
                if (item.group == 0 || item.group == group)
                    list.Add(item);
            });
            return list;
        }

        public async Task RequestGroup(Quik quik, int group)
        {
            var list = this.Select(group);
            var Result = await quik.Trading.GetParamEx2Bulk(list).ConfigureAwait(false);
            GetParamEx2Bulk.ParseResult(list, Result);
        }
        public async Task RequestAll(Quik quik)
        {
            var list = this.Items;
            var Result = await quik.Trading.GetParamEx2Bulk(list).ConfigureAwait(false);
            GetParamEx2Bulk.ParseResult(list, Result);
        }
    }


    public class GetParamEx2BulkItem<T> : IGetParamEx2BulkItem
    {
        public readonly ISecurity ins;
        public string ClassCode { get => ins.ClassCode; }
        public string SecCode { get => ins.SecCode; }
        public ParamNames paramName { get; set; }
        public T Value { get; private set; }
        public bool Changed { get; private set; } = false;
        public int group { get; set; } = 0;

        public GetParamEx2BulkItem(ISecurity ins, ParamNames paramName, int group = 0)
        {
            this.ins = ins;
            this.paramName = paramName;
            this.group = group;
        }

        public void SetValue(ParamTable pt)
        {
            var paramType = pt.Value.GetType();
            var valueType = typeof(T);
            T new_value;

            if (paramType.IsPrimitive || pt.Value != null)
            {
                if (valueType == paramType)
                    new_value = (T)pt.Value;
                else
                {
                    if (valueType.IsEnum)
                        new_value = (T)Convert.ChangeType(pt.Value, typeof(Int32));
                    else
                        new_value = (T)Convert.ChangeType(pt.Value, valueType);
                }
            }
            else
            {
                new_value = (T)(valueType.IsPrimitive ? default : Activator.CreateInstance(valueType));
            }
            this.Changed = !new_value.Equals(Value);
            if (this.Changed)
                Value = new_value;
        }

        public override bool Equals(object obj)
        {
            return obj is GetParamEx2BulkItem<T> item &&
                   ClassCode == item.ClassCode &&
                   paramName == item.paramName &&
                   SecCode == item.SecCode &&
                   EqualityComparer<T>.Default.Equals(Value, item.Value);
        }

        public override int GetHashCode()
        {
            int hashCode = 1448477480;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ClassCode);
            hashCode = hashCode * -1521134295 + paramName.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SecCode);
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            return hashCode;
        }
    }
}