﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Hagar.Utilities
{
    internal static class TypeCodeExtensions
    {
        public static void x(TypeCode x)
        {
            switch (x)
            {
                case TypeCode.Boolean:
                    break;
                case TypeCode.Byte:
                    break;
                case TypeCode.Char:
                    break;
                case TypeCode.DateTime:
                    break;
                case TypeCode.Decimal:
                    break;
                case TypeCode.Double:
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.Int16:
                    break;
                case TypeCode.Int32:
                    break;
                case TypeCode.Int64:
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    break;
                case TypeCode.Single:
                    break;
                case TypeCode.String:
                    break;
                case TypeCode.UInt16:
                    break;
                case TypeCode.UInt32:
                    break;
                case TypeCode.UInt64:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
            }
        }
    }
}