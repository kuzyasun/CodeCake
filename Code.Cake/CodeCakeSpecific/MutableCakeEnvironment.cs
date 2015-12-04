﻿using System;
using System.Globalization;
using System.Reflection;
using Cake.Core.IO;
using System.Collections.Generic;
using Cake.Core;
using System.Linq;
using System.Runtime.Versioning;

namespace CodeCake
{
    /// <summary>
    /// Represents the environment Cake operates in. This mutable implementation allows the PATH environment variable
    /// to be dynamically modified. Except this new <see cref="EnvironmentPaths"/> this is the same as the <see cref="CakeEnvironment"/>
    /// provided by Cake.
    /// </summary>
    public class MutableCakeEnvironment : ICakeEnvironment
    {
        readonly List<string> _path;

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        /// <value>The working directory.</value>
        public DirectoryPath WorkingDirectory
        {
            get { return Environment.CurrentDirectory; }
            set { SetWorkingDirectory( value ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableCakeEnvironment"/> class.
        /// </summary>
        public MutableCakeEnvironment()
        {
            WorkingDirectory = new DirectoryPath( Environment.CurrentDirectory );
            var pathEnv = Environment.GetEnvironmentVariable( "PATH" );
            if( !string.IsNullOrEmpty( pathEnv ) )
            {
                _path = new List<string>( pathEnv.Split( new char[] { Machine.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.Trim() )
                    .Where( s => s.Length > 0 ) );
            }
            else
            {
                _path = new List<string>();
            }
        }

        /// <summary>
        /// Gets whether or not the current operative system is 64 bit.
        /// </summary>
        /// <returns>
        /// Whether or not the current operative system is 64 bit.
        /// </returns>
        public bool Is64BitOperativeSystem()
        {
            return Machine.Is64BitOperativeSystem();
        }

        /// <summary>
        /// Determines whether the current machine is running Unix.
        /// </summary>
        /// <returns>
        /// Whether or not the current machine is running Unix.
        /// </returns>
        public bool IsUnix()
        {
            return Machine.IsUnix();
        }

        /// <summary>
        /// Gets a special path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// A <see cref="DirectoryPath" /> to the special path.
        /// </returns>
        public DirectoryPath GetSpecialPath( SpecialPath path )
        {
            switch( path )
            {
                case SpecialPath.ApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) );
                case SpecialPath.CommonApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ) );
                case SpecialPath.LocalApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ) );
                case SpecialPath.ProgramFiles:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ) );
                case SpecialPath.ProgramFilesX86:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 ) );
                case SpecialPath.Windows:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.Windows ) );
                case SpecialPath.LocalTemp:
                    return new DirectoryPath( System.IO.Path.GetTempPath() );
            }
            const string format = "The special path '{0}' is not supported.";
            throw new NotSupportedException( string.Format( CultureInfo.InvariantCulture, format, path ) );
        }

        /// <summary>
        /// Gets the application root path.
        /// </summary>
        /// <returns>
        /// The application root path.
        /// </returns>
        public DirectoryPath GetApplicationRoot()
        {
            var path = System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            return new DirectoryPath( path );
        }

        /// <summary>
        /// Gets an environment variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>
        /// The value of the environment variable.
        /// </returns>
        public string GetEnvironmentVariable( string variable )
        {
            if( StringComparer.OrdinalIgnoreCase.Equals( variable, "PATH" ) ) return String.Join( Machine.IsUnix() ? ":" : ";", EnvironmentPaths );
            return Environment.GetEnvironmentVariable( variable );
        }

        /// <summary>
        /// Gets a mutable set of paths. This is initialized with the PATH environment variable but can be changed at any time.
        /// When getting the PATH variable with <see cref="GetEnvironmentVariable"/>, this set is returned as a joined string.
        /// </summary>
        public IList<string> EnvironmentPaths
        {
            get { return _path; }
        }

        private static void SetWorkingDirectory( DirectoryPath path )
        {
            if( path.IsRelative )
            {
                throw new CakeException( "Working directory can not be set to a relative path." );
            }
            Environment.CurrentDirectory = path.FullPath;
        }

        /// <summary>
        /// Gets all environment variables.
        /// </summary>
        /// <returns>The environment variables as IDictionary&lt;string, string&gt; </returns>
        public IDictionary<string, string> GetEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Select( e => StringComparer.OrdinalIgnoreCase.Equals( e.Key, "PATH" ) 
                                ? new System.Collections.DictionaryEntry( e.Key, GetEnvironmentVariable( "PATH" ) )
                                : e )
                .ToDictionary(
                key => (string)key.Key,
                value => value.Value as string,
                StringComparer.OrdinalIgnoreCase );
        }
        /// <summary>
        /// Gets the target .Net framework version that the current AppDomain is targeting.
        /// </summary>
        /// <returns>The target framework.</returns>
        public FrameworkName GetTargetFramework()
        {
            // Try to get the current framework name from the current application domain,
            // but if that is null, we default to .NET 4.5. The reason for doing this is
            // that this actually is what happens on Mono.
            var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            return new FrameworkName( frameworkName ?? ".NETFramework,Version=v4.5" );
        }

    }
}
