// XboxLiveTrace - A tool for analyzing Xbox Live traces
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace XboxLiveTrace
{
    /// <summary>
    /// Entry point for the XboxLiveTrace application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the XboxLiveTrace application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        [MTAThreadAttribute]
        public static void Main(string[] args)
        {
            // Register the AssemblyResolve event handler to allow loading assemblies from resources.
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;

            // Call the main entry point for the analyzer.
            AnalyzerMain.Main(args);
        }

        /// <summary>
        /// Event handler for the AssemblyResolve event. This allows loading assemblies from resources.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="args">The event arguments.</param>
        /// <returns>The loaded assembly.</returns>
        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            // Get the executing assembly and the name of the assembly being resolved.
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            // Construct the path to the assembly file based on the assembly name and culture.
            string path = assemblyName.Name + ".dll";

            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            // Attempt to load the assembly from a resource stream in the executing assembly.
            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
