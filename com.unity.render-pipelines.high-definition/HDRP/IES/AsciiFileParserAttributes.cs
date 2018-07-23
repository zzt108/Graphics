using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.ComponentModel;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline.AsciiParser
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LineAttribute : Attribute
    {
        public int      lineNumber;
        public Regex    match;
        public Regex    startMatch;
        public Regex    stopMatch;
        public bool     required;
        public int      maxLines;
        public int      lineCount;
        public bool     split;

        public bool     matchMode;

        public LineAttribute(int lineNumber, string match = @"(.*)", bool required = true, int maxLines = 1, bool split = true)
        {
            this.lineNumber = lineNumber;
            this.match = new Regex(match);
            this.required = required;
            this.maxLines = maxLines;
            this.split = split;
            this.lineCount = 0;

            matchMode = true;
        }

        public LineAttribute(int lineNumber, string start, string stop)
        {
            this.lineNumber = lineNumber;
            this.startMatch = new Regex(start);
            this.stopMatch = new Regex(stop);
            
            matchMode = false;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SkipIfEqualAttribute : Attribute
    {
        public string   fieldName;
        public string   value;

        public SkipIfEqualAttribute(string fieldName, string value)
        {
            this.fieldName = fieldName;
            this.value = value;
        }
    }
}