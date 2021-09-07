// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;

namespace QUIKSharp.TestQuik
{
    public static class TestUtils
    {
        static public void CopyObject(object src, object dst)
        {
            var type = src.GetType();
            if (type != dst.GetType())
            {
                throw new Exception($"Different types {type.Name} and {dst.GetType().Name}");
            }

            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead) continue;
                if (!prop.CanWrite) continue;

                object value = prop.GetValue(src);
                Type prop_type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(dst, value);
            }

            foreach (var field in type.GetFields())
            {
                if (!field.IsPublic) continue;

                object value = field.GetValue(src);
                Type prop_type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                field.SetValue(dst, value);
            }
        }

        static public bool CompareIsSameObj(object obj1, object obj2)
        {
            if (obj1.GetType() != obj2.GetType())
            {
                Console.WriteLine($"Different types {obj1.GetType().Name} and {obj2.GetType().Name}");
                return false;
            }

            bool areSame = true;
            foreach (var property in obj1.GetType().GetProperties())
            {
                if (!property.CanRead) continue;
                var value1 = property.GetValue(obj1, null);
                var value2 = property.GetValue(obj2, null);

                if (value1 == value2)
                    continue;

                if (value1 == null || value2 == null || !value1.Equals(value2))
                {
                    Console.WriteLine($" Property: {property.Name} are not same: '{value1}' != '{value2}'");
                    areSame = false;
                }
            }
            return areSame;
        }
    }
}
