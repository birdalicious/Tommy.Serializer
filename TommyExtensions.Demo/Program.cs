﻿// ----------------------------------------------------------------------------
// -- Project : https://github.com/instance-id/TommyExtensions               --
// -- instance.id 2020 | http://github.com/instance-id | http://instance.id  --
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace instance.id.TommyExtensions.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "TestData.toml".DeterminePath();
            var pathCombined = "TestDataCombined.toml".DeterminePath();

            var testData = new TestData();
            var testData2 = new TestData2();

            // -- Takes the TestData class and writes it's default values to disk.
            TommyExtensions.ToTomlFile(testData, path);

            // -- Write both TestData and TestData2 values to single file.
            TommyExtensions.ToTomlFile(new object[] {testData, testData2}, pathCombined);

            // -- Reads the file created from TestData and displays the values in the console.
            TestDataNoDefault loadTestData  = TommyExtensions.FromTomlFile<TestDataNoDefault>(path);

            string classData = null;
            var props = loadTestData.GetType().GetProperties();
            foreach (var prop in props)
                classData += $"Name: {prop.Name} Value: {loadTestData.GetPropertyValue(prop.Name)}\n";

            Console.WriteLine(classData);
         }
    }

    #region Demo Helpers

    public static class Utilities
    {
        public static object GetPropertyValue(
            this object src,
            string propName,
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public)
        {
            return src.GetType().GetProperty(propName, bindingAttr)?.GetValue(src, null);
        }

        /// <summary>
        /// Check whether the application is running in debug mode in order to determine where to export the file
        /// </summary>
        /// <param name="config">The string name of the output file to create</param>
        /// <returns>The full path in which the file will be created</returns>
        public static string DeterminePath(this string config)
        {
            var path = "";
            try
            {
                path = Debugger.IsAttached
                    ? Path.Combine(Directory.GetCurrentDirectory(), "../../../", config)
                    : Path.Combine(Directory.GetCurrentDirectory(), config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return Path.GetFullPath(path);
        }
    }

    #endregion
}
