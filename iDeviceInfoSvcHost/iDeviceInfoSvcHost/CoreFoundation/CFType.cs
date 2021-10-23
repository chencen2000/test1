// Software License Agreement (BSD License)
// 
// Copyright (c) 2011, iSn0wra1n <isn0wra1ne@gmail.com>
// All rights reserved.
// 
// Redistribution and use of this software in source and binary forms, with or without modification are
// permitted without the copyright owner's permission provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above
//   copyright notice, this list of conditions and the
//   following disclaimer.
// 
// * Redistributions in binary form must reproduce the above
//   copyright notice, this list of conditions and the
//   following disclaimer in the documentation and/or other
//   materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
// TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
/*
CFTypeID   1: CFType
CFTypeID   2: CFAllocator
CFTypeID   3: CFDictionary
CFTypeID   4: CFArray
CFTypeID   5: CFData
CFTypeID   6: CFSet
CFTypeID   7: CFString
CFTypeID   8: CFNull
CFTypeID   9: CFBoolean
CFTypeID  10: CFNumber
CFTypeID  11: CFDate
CFTypeID  12: CFTimeZone
CFTypeID  13: CFBinaryHeap
CFTypeID  14: CFBitVector
CFTypeID  15: CFBag
CFTypeID  16: CFCharacterSet
CFTypeID  17: CFStorage
CFTypeID  18: CFTree
CFTypeID  19: CFURL
CFTypeID  20: CFXMLNode
CFTypeID  21: CFXMLParser
CFTypeID  22: CFBundle
CFTypeID  23: CFPlugInInstance
CFTypeID  24: CFUUID
CFTypeID  25: CFWeakSet
CFTypeID  26: CFWeakMap
CFTypeID  27: CFLocale
CFTypeID  28: CFCalendar
CFTypeID  29: CFNumberFormatter
CFTypeID  30: CFDateFormatter
CFTypeID  31: CFMessagePort
CFTypeID  32: CFMachPort
CFTypeID  33: CFRunLoop
CFTypeID  34: CFRunLoopMode
CFTypeID  35: CFRunLoopObserver
CFTypeID  36: CFRunLoopSource
CFTypeID  37: CFRunLoopTimer
CFTypeID  38: CFSocket
CFTypeID  39: CFReadStream
CFTypeID  40: CFWriteStream
CFTypeID  41: CFPreferencesDomain
CFTypeID  42: CFNotificationCenter
CFTypeID  43: CFPasteboard
CFTypeID  44: CFUserNotification
CFTypeID  45: CFKeyedArchiverUID
CFTypeID  46: CFRunArray
CFTypeID  47: CFAttributedString
 */
namespace CoreFoundation
{
    public class CFType
    {
        static internal int _CFArray = CFLibrary.CFArrayGetTypeID(); //18;
        static internal int _CFBoolean = CFLibrary.CFBooleanGetTypeID();//21;
        static internal int _CFData = CFLibrary.CFDataGetTypeID();//19;
        static internal int _CFXMLNode = CFLibrary.CFXMLNodeGetTypeID();//20;
        static internal int _CFNumber = CFLibrary.CFNumberGetTypeID();//22;
        static internal int _CFDictionary = CFLibrary.CFDictionaryGetTypeID();//17;
        static internal int _CFString = CFLibrary.CFStringGetTypeID();//7;

        internal IntPtr typeRef;

        public CFType() { }
        
        public CFType(IntPtr handle){this.typeRef = handle;}
        
        /// <summary>
        /// Returns the unique identifier of an opaque type to which a CoreFoundation object belongs
        /// </summary>
        /// <returns></returns>
        public int GetTypeID()
        {
            return CFLibrary.CFGetTypeID(typeRef);
        }
        /// <summary>
        /// Returns a textual description of a CoreFoundation object
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return new CFString(CFLibrary.CFCopyDescription(typeRef)).ToString();
        }       
                
        private string CFString()
        {
            if (typeRef == IntPtr.Zero)
                return null;
            
                string str;
                int length = CFLibrary.CFStringGetLength(typeRef);        
                IntPtr u = CFLibrary.CFStringGetCharactersPtr(typeRef);
                IntPtr buffer = IntPtr.Zero;
                if (u == IntPtr.Zero)
                {
                    CFRange range = new CFRange(0, length);
                    buffer = Marshal.AllocCoTaskMem(length * 2);
                    CFLibrary.CFStringGetCharacters(typeRef, range, buffer);
                    u = buffer;
                }
                unsafe
                {
                    str = new string((char*)u, 0, length);
                }
                if (buffer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(buffer);
                return str;
        }       
        private string CFNumber()
        {
            IntPtr buffer = Marshal.AllocCoTaskMem(CFLibrary.CFNumberGetByteSize(typeRef));
            bool scs = CFLibrary.CFNumberGetValue(typeRef, CFLibrary.CFNumberGetType(typeRef), buffer);
            if (scs != true)
            {
                return string.Empty;
            }
            int type = (int)CFLibrary.CFNumberGetType(typeRef);
            switch (type)
            {
                case 1:
                case 7:
                    return Marshal.ReadInt16(buffer).ToString();
                case 2:
                case 8:
                    return Marshal.ReadInt16(buffer).ToString();
                case 3:
                case 9:
                    return Marshal.ReadInt32(buffer).ToString();
                case 4:
                case 10:
                case 11:
                    return Marshal.ReadInt64(buffer).ToString();
                case 5:
                case 12:
                    return ((float)Marshal.PtrToStructure(buffer, typeof(float))).ToString();
                case 6:
                case 13:
                    return ((double)Marshal.PtrToStructure(buffer, typeof(double))).ToString();
                default:
                    return Enum.GetName(typeof(CFNumber.CFNumberType), type) + " is not supported yet!";
            }
        }
        private string CFBoolean()
        {
            String sBool = new CFBoolean(typeRef).GetDescription();
            Match match = Regex.Match(sBool, @".*?\{value[\s]*=[\s]*(.*?)\}", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "N/A";
        }
        private string CFDictionary()
        {
            StringBuilder sb = new StringBuilder();
            string sxml = CFPropertyList();
            sxml = sxml.Replace("\n", "");
            sb.AppendLine(sxml);
            int nCount = CFLibrary.CFDictionaryGetCount(typeRef);
            IntPtr[] keys = new IntPtr[nCount];
            IntPtr[] values = new IntPtr[nCount];
            CFLibrary.CFDictionaryGetKeysAndValues(typeRef, keys, values);
            for (int i = 0; i < keys.Length; i++)
            {
                sb.AppendLine(new CFType(keys[i]).ToString() + "=" + new CFType(values[i]).ToString());
            }

            return sb.ToString();
        }

        private string CFData()
        {
            CFData typeArray = new CFData(typeRef);
            return Convert.ToBase64String(typeArray.ToByteArray());
        }
        private string CFPropertyList()
        {
            return Encoding.UTF8.GetString(new CFData(CFLibrary.CFPropertyListCreateXMLData(IntPtr.Zero, typeRef)).ToByteArray());
        }
        public string ToStringV1()
        {
            int ntype = CFLibrary.CFGetTypeID(typeRef);
            if (_CFString == ntype)
                return CFString();
            else if (_CFDictionary == ntype)
                return CFPropertyList();
            else if (_CFArray == ntype)
                return CFPropertyList();
            else if (_CFData == ntype)
                return CFData();
            else if (_CFBoolean == ntype)
                return CFBoolean();
            else if (_CFNumber == ntype)
                return CFNumber();
            return null;
        }

        public override string ToString()
        {
            int ntype=CFLibrary.CFGetTypeID(typeRef);
            if (_CFString == ntype)
                return CFString();
            else if (_CFDictionary == ntype)
                return CFDictionary();
            else if (_CFArray == ntype)
                return CFPropertyList();
            else if (_CFData == ntype)
                return CFData();
            else if (_CFBoolean == ntype)
                return CFBoolean();
            else if (_CFNumber == ntype)
                return CFNumber();
            return null;
        }
        public static implicit operator IntPtr(CFType value)
        {
            return value.typeRef;
        }
        public static implicit operator CFType(IntPtr value)
        {
            return new CFType(value);
        }
    }
}
