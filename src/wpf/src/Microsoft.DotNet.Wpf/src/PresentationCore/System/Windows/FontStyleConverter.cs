﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// FontStyleConverter class parses a font style string.
    /// </summary>
    public sealed class FontStyleConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) 
        {
            if (destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string)) 
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }
        
        /// <summary>
        /// ConvertFrom - attempt to convert to a FontStyle from the given object
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a FontStyle.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object value)
        {
            if (null == value)
            {
                throw GetConvertFromException(value);
            }

            String s = value as string;

            if (null == s)
            {
                throw new ArgumentException(SR.Format(SR.General_BadType, "ConvertFrom"), nameof(value));
            }
            
            FontStyle fontStyle = new FontStyle();
            if (!FontStyles.FontStyleStringToKnownStyle(s, ci, ref fontStyle))
                throw new FormatException(SR.Parsers_IllegalToken);

            return fontStyle;
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// An NotSupportedException is thrown if the example object is null or is not a FontStyle,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && value is FontStyle)
            {
                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(FontStyle).GetConstructor(new Type[]{typeof(int)});
                    int c = ((FontStyle)value).GetStyleForInternalConstruction();
                    return new InstanceDescriptor(ci, new object[]{c});
                }
                else if (destinationType == typeof(string))
                {
                    FontStyle c = (FontStyle)value;
                    return ((IFormattable)c).ToString(null, culture);
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
