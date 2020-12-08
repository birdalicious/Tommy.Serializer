﻿// ----------------------------------------------------------------------------
// -- Project : https://github.com/instance-id/TommyExtensions               --
// -- instance.id 2020 | http://github.com/instance-id | http://instance.id  --
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tommy;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PatternAlwaysOfType
namespace instance.id.TommyExtensions
{
    public static class TommyExtensions
    {
        /// <summary>
        /// Reflectively determines the property types and values of the passed class instance and outputs a Toml file
        /// </summary>
        /// <param name="data">The class instance in which the properties will be used to create a Toml file </param>
        /// <param name="path">The destination path in which to create the Toml file</param>
        /// <param name="debug">If enabled, shows property values - Will be removed when development is completed</param>
        public static void ToTomlFile(object data, string path, bool debug = false)
        {
            try
            {
                List<SortNode> tomlData = new List<SortNode>();
                TomlTable tomlTable = new TomlTable();
                TomlTable tomlDataTable = new TomlTable();
                Type t = data.GetType();

                // -- Check object for table name attribute ------------------
                var tableName = t.GetCustomAttribute<TommyTableName>()?.TableName;

                // -- Iterate the properties of the object -------------------
                PropertyInfo[] props = t.GetProperties();
                foreach (var prop in props)
                {
                    // -- Check if property is to be ignored -----------------
                    // -- If so, continue on to the next property ------------
                    if (Attribute.IsDefined(prop, typeof(TommyIgnore))) continue;

                    // -- Check if property has comment attribute ------------
                    var comment = prop.GetCustomAttribute<TommyComment>()?.Value;
                    var sortOrder = prop.GetCustomAttribute<TommySortOrder>()?.SortOrder;

                    if (debug) Console.WriteLine($"Prop: Name {prop.Name} Type: {prop.PropertyType} Value: {data.GetPropertyValue(prop.Name)}");
                    var propValue = data.GetPropertyValue(prop.Name);

                    // -- Check each property type in order to
                    // -- determine which type of TomlNode to create
                    if (prop.PropertyType == typeof(bool))
                    {
                        tomlData.Add(new SortNode
                        {
                            Name = prop.Name,
                            SortOrder = sortOrder ?? -1,
                            Value = new TomlBoolean
                            {
                                Comment = comment,
                                Value = (bool) prop.GetValue(data)
                            }
                        });
                        continue;
                    }

                    if (prop.PropertyType == typeof(string))
                    {
                        tomlData.Add(new SortNode
                        {
                            Name = prop.Name,
                            SortOrder = sortOrder ?? -1,
                            Value = new TomlString
                            {
                                Comment = comment,
                                Value = prop.GetValue(data)?.ToString() ?? ""
                            }
                        });
                        continue;
                    }

                    if (prop.PropertyType.IsNumerical())
                    {
                        switch (prop.PropertyType)
                        {
                            case Type a when a == typeof(int):
                                tomlData.Add(new SortNode
                                {
                                    Name = prop.Name,
                                    SortOrder = sortOrder ?? -1,
                                    Value = new TomlInteger
                                    {
                                        Comment = comment,
                                        Value = Convert.ToInt32(propValue ?? 0)
                                    }
                                });
                                break;
                            case Type a when a == typeof(ulong):
                                tomlData.Add(new SortNode
                                {
                                    Name = prop.Name,
                                    SortOrder = sortOrder ?? -1,
                                    Value = new TomlInteger
                                    {
                                        Comment = comment,
                                        Value = Convert.ToInt64(propValue ?? 0)
                                    }
                                });
                                break;
                            case Type a when a == typeof(float):
                                var floatValue = (float) propValue;
                                tomlData.Add(new SortNode
                                {
                                    Name = prop.Name,
                                    SortOrder = sortOrder ?? -1,
                                    Value = new TomlFloat
                                    {
                                        Comment = comment,
                                        Value = Convert.ToDouble(floatValue.ToString(formatter))
                                    }
                                });
                                break;
                            case Type a when a == typeof(double):
                                tomlData.Add(new SortNode
                                {
                                    Name = prop.Name,
                                    SortOrder = sortOrder ?? -1,
                                    Value = new TomlFloat
                                    {
                                        Comment = comment,
                                        Value = Convert.ToDouble(propValue ?? 0)
                                    }
                                });
                                break;
                            case Type a when a == typeof(decimal):
                                tomlData.Add(new SortNode
                                {
                                    Name = prop.Name,
                                    SortOrder = sortOrder ?? -1,
                                    Value = new TomlFloat
                                    {
                                        Comment = comment,
                                        Value = Convert.ToDouble(propValue ?? 0)
                                    }
                                });
                                break;
                        }

                        continue;
                    }

                    if (!prop.PropertyType.IsClass || !prop.PropertyType.GetInterfaces().Contains(typeof(IEnumerable))) continue;

                    var val = propValue as IList;
                    var tomlArray = new TomlArray {Comment = comment};


                    if (val != null)
                        for (var i = 0; i < val.Count; i++)
                        {
                            if (val[i] == null) throw new ArgumentNullException($"Error: collection value cannot be null");
                            if (debug) Console.WriteLine($"    CollectionValue index:{i.ToString()}: {val[i]}");

                            var valueType = val[i].GetType();

                            if (valueType.IsNumerical())
                                tomlArray.Add(new TomlInteger {Value = (int) val[i]});

                            if (valueType == typeof(string))
                                tomlArray.Add(new TomlString {Value = val[i] as string});
                        }

                    tomlData.Add(new SortNode
                    {
                        Name = prop.Name,
                        SortOrder = sortOrder ?? -1,
                        Value = tomlArray
                    });
                }

                // -- Check if sorting needs to be done to properties ----
                var maxSortInt = (from l in tomlData select l.SortOrder).Max();
                if (maxSortInt > -1)
                {
                    for (var i = 0; i < tomlData.Count; i++)
                    {
                        var n = tomlData[i];
                        if (n.SortOrder > -1) continue;
                        tomlData[i] = new SortNode {SortOrder = maxSortInt + 1, Value = n.Value, Name = n.Name};
                    }

                    tomlData = tomlData.OrderBy(n => n.SortOrder).ToList();
                }

                tomlData.ForEach(n => { tomlDataTable[n.Name] = n.Value; });

                if (!string.IsNullOrEmpty(tableName)) tomlTable[tableName] = tomlDataTable;
                else tomlTable = tomlDataTable;

                if (debug) Console.WriteLine(tomlTable.ToString());


                // -- Writes the Toml file to disk ---------------------------
                using (StreamWriter writer = new StreamWriter(File.OpenWrite(path)))
                {
                    tomlTable.WriteTo(writer);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region Extension Methods

        private static readonly string formatter = "0." + new string('#', 60);

        private static bool IsNumerical(this Type type)
        {
            return // @formatter:off
                type == typeof(sbyte)  ||
                type == typeof(byte)   ||
                type == typeof(short)  ||
                type == typeof(ushort) ||
                type == typeof(int)    ||
                type == typeof(uint)   ||
                type == typeof(long)   ||
                type == typeof(ulong)  ||
                type == typeof(float)  ||
                type == typeof(double) ||
                type == typeof(decimal);
        } // @formatter:on

        private static object GetPropertyValue(
            this object src,
            string propName,
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public)
        {
            return src.GetType().GetProperty(propName, bindingAttr)?.GetValue(src, null);
        }

        #endregion
    }


    #region Helper Classes

    public struct SortNode
    {
        public string Name { get; set; }
        public TomlNode Value { get; set; }
        public int SortOrder { get; set; }
    }

    #endregion

    #region Attribute Classes

    [AttributeUsage(AttributeTargets.Class)]
    public class TommyTableName : Attribute
    {
        public string TableName { get; }

        /// <summary>
        /// Designates a class as a Toml Table and applies all contained properties as children of that table
        /// </summary>
        /// <param name="tableName">String value which will be used as the Toml Table name</param>
        public TommyTableName(string tableName) => TableName = tableName;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TommyComment : Attribute
    {
        public string Value { get; }

        /// <summary>
        /// Adds a toml comment to a property or field
        /// </summary>
        /// <param name="comment">String value which will be used as a comment for the property/field</param>
        public TommyComment(string comment) => Value = comment;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TommySortOrder : Attribute
    {
        public int SortOrder { get; }

        /// <summary>
        /// Determines the order in which the properties will be written to file, sorted by numeric value with 0 being the first entry but below the table name (if applicable).
        /// </summary>
        /// <param name="sortOrder">Int value representing the order in which this item will appear in the Toml file</param>
        public TommySortOrder(int sortOrder = -1) => SortOrder = sortOrder;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TommyIgnore : Attribute
    {
    }

    #endregion
}
