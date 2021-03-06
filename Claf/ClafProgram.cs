﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Claf
{
    /// <summary>
    /// A function that converts a string to another type.
    /// </summary>
    public delegate object ConvertToHandler( string s );

    /// <summary>
    /// CLAF application base class. Programs wanting to implement a command
    /// line interface should create a class that inherits from this one.
    /// </summary>
    public class ClafProgram
    {
        /// <summary>
        /// The default prompt string. This is set to an IDLE prompt:
        /// >>> TYPE HERE
        /// </summary>
        public static readonly string DefaultPrompt = ">>> ";

        /// <summary>
        /// Initialises a CLAF class with the default prompt.
        /// </summary>
        public ClafProgram()
            : this(ClafProgram.DefaultPrompt)
        {}

        /// <summary>
        /// Initialises a CLAF program with the given prompt.
        /// </summary>
        public ClafProgram(string prompt)
        {
            castHandlers = new Dictionary<Type, ConvertToHandler>( );
            this.helpString = "";
            this.Prompt = prompt;

            this.docs = new RuntimeDocs(this.GetType(), typeof(ClafProgram));
        }

        private RuntimeDocs docs;

        /// <summary>
        /// Gets the single line summary of a function.
        /// </summary>
        /// <param name="m">The MethodInfo for the method.</param>
        protected void getSummary(MethodInfo m)
        {
            this.getSummary(m.Name);
        }
        
        /// <summary>
        /// Gets the single line summary of a function.
        /// </summary>
        /// <param name="method">The name of the method.</param>
        protected void getSummary(string method)
        {
            Console.WriteLine(this.docs[method].Element("summary").Value.Trim());
        }

        /// <summary>
        /// Mapping of types to their conversion handlers.
        /// </summary>
        private Dictionary<Type, ConvertToHandler> castHandlers;

        /// <summary>
        /// Adds a conversion function for a type that does not handle casts from strings.
        /// </summary>
        protected void SetCastHandler(Type T, ConvertToHandler handler)
        {
            castHandlers[ T ] = handler;
        }

        /// <summary>
        /// Internal variable dictating whether we have been asked to close.
        /// </summary>
        private bool alive;

        /// <summary>
        /// Help string cached value that lists all functions.
        /// </summary>
        private string helpString;

        /// <summary>
        /// The prompt string.
        /// </summary>
        private string prompt;

        /// <summary>
        /// The string to print before asking for user input.
        /// </summary>
        /// <remarks>
        /// For example an IDLE like processor would use ">>> ".
        /// </remarks>
        protected string Prompt
        {
            get
            {

                return prompt;
            }
            set
            {
                prompt = value;
            }
        }

        /// <summary>
        /// Runs the main command line program loop, reading lines from the console
        /// until told to exit.
        /// </summary>
        public void Run()
        {
            string s;

            this.alive = true;
            do
            {
                Console.Write( this.Prompt );
                s = Console.ReadLine( );
                this.RunOnce( s );
            } while ( this.alive );

            return;
        }

        /// <summary>
        /// Process an input line.
        /// </summary>
        public void RunOnce(string s)
        {
            string[ ] parts = s.Split( ' ' );

            MethodInfo m = this.GetType( ).GetMethod( parts[ 0 ] );

            if (!IsClafVisible(m))
            {
                Console.WriteLine( "No command '{0}' exists! Try `help`.", parts[ 0 ] );
                return;
            }

            try
            {
                List<string> p = parts.ToList<string>( );
                List<object> par = new List<object>( p.Count - 1 );
                p.RemoveAt( 0 );

                foreach ( ParameterInfo t in m.GetParameters( ) )
                {
                    object val = Convert.DBNull;

                    if ( par.Count >= p.Count )
                    {
                        if ( Convert.IsDBNull( t.DefaultValue ) )
                            throw new TargetParameterCountException( "Too few args." );

                        val = t.DefaultValue;
                    }
                    else
                    {
                        try
                        {
                            val = Convert.ChangeType( p[ par.Count ], t.ParameterType );
                        }
                        catch
                        {
                            if ( this.castHandlers.ContainsKey( t.ParameterType ) )
                                val = this.castHandlers[ t.ParameterType ]( p[ par.Count ] );
                        }

                        if ( Convert.IsDBNull( val ) )
                            throw new ArgumentException( string.Format( "Incorrect parameter type for parameter {0}. Expected a {1} but got '{2}'.", par.Count + 1, t.ParameterType.Name, p[ par.Count ] ) );

                    }

                    par.Add( val );
                }

                if ( par.Count < p.Count )
                {
                    throw new TargetParameterCountException( string.Format( "'{0}' takes at most {1} arguments, {2} were given.", m.Name, par.Count, p.Count ) );
                }

                m.Invoke( this, par.ToArray( ) );
            }
            catch ( TargetParameterCountException e )
            {
                Console.WriteLine( "Incorrect number of arguments: {0}", e.Message );
                Console.WriteLine( "Usage:\n\t{0}", this.getUsage( m ) );
            }
            catch ( ArgumentException e )
            {
                Console.WriteLine( "Error in arguments: {0}", e.Message );
                Console.WriteLine( "Usage:\n\t{0}", this.getUsage( m ) );
            }
            catch ( Exception e )
            {
                Console.WriteLine( e.Message );
            }
        }

        /// <summary>
        /// Returns the usage string for a command.
        /// </summary>
        private string getUsage(string method)
        {
            return this.getUsage( this.GetType( ).GetMethod( method ) );
        }

        /// <summary>
        /// Returns the usage string for a command.
        /// </summary>
        private string getUsage(MethodInfo method)
        {
            StringBuilder ret = new StringBuilder( );

            ret.Append( method.Name );

            foreach ( ParameterInfo p in method.GetParameters( ) )
            {
                ret.AppendFormat( " {0}", this.parameterToUsageString( p ) );
            }

            return ret.ToString( );
        }

        /// <summary>
        /// Generates the usage string for a single parameter based on its parameter info.
        /// </summary>
        private string parameterToUsageString(ParameterInfo p)
        {
            if ( Convert.IsDBNull(p.DefaultValue) )
                return p.Name;
            else
                return "[" + p.Name + "]";
        }

        /// <summary>
        /// Returns the short help string (description line) for a command.
        /// </summary>
        private string getDescr(string method)
        {
            return this.getDescr( this.GetType( ).GetMethod( method ) );
        }

        /// <summary>
        /// Returns the short help string (description line) for a command.
        /// </summary>
        private string getDescr(MethodInfo method)
        {
            return this.docs[method.Name].Element("summary").Value.Trim();
        }


        private string getParamHelp(string method)
        {
            MethodInfo m = this.GetType( ).GetMethod( method );

            if ( m != null )
                return this.getParamHelp( m );

            throw new MissingMethodException( this.GetType( ).Name, method );
        }

        private string getParamHelp(MethodInfo method)
        {
            return "";
        }

        /// <summary>
        /// Gets the long help string for a command.
        /// </summary>
        private string getLongHelp(string method)
        {
            MethodInfo m = this.GetType( ).GetMethod( method );

            if ( m != null )
                return this.getLongHelp( m );

            throw new MissingMethodException( this.GetType( ).Name, method );
        }

        /// <summary>
        /// Gets the long help string for a command.
        /// </summary>
        private string getLongHelp(MethodInfo method)
        {
            return this.docs["remarks"].Value;
        }

        /// <summary>
        /// Gets the full help string for a command. This includes description, usage and long help.
        /// </summary>
        private string getHelp(string method)
        {
            MethodInfo m = this.GetType( ).GetMethod( method );

            if (m != null)
                return this.getHelp( m );

            throw new MissingMethodException( this.GetType( ).Name, method );
        }

        /// <summary>
        /// Gets the full help string for a command. This includes description, usage and long help.
        /// </summary>
        private string getHelp(MethodInfo method)
        {
            string longHelp = this.getLongHelp( method );

            if (longHelp != "")
                return string.Format( "{0}\n  {1}\n\n\t{2}\n\n{3}\n\n{4}", method.Name, this.getDescr( method ), this.getUsage( method ), this.getParamHelp(method), this.getLongHelp(method) );

            return string.Format( "{0}\n  {1}\n\n\t{2}\n\n{3}", method.Name, this.getDescr( method ), this.getUsage( method ), this.getParamHelp( method ) );
        }

        /// <summary>
        /// Prints the help message, optionally for a specific function.
        /// </summary>
        public virtual void help( string func = "" )
        {
            if ( func != "" )
            {
                try
                {
                    Console.WriteLine( this.getHelp( func ) );
                }
                catch ( MissingMethodException )
                {
                    Console.WriteLine( "Command '{0}' does not exist.", func );
                }
            }
            else
            {
                if ( this.helpString == "" )
                    this.updateHelpCache( );

                Console.Write( this.helpString );
            }
        }

        /// <summary>
        /// Updates the internal string that stores the table of commands and descriptions.
        /// </summary>
        private void updateHelpCache()
        {
            string d;
            StringBuilder b = new StringBuilder( );

            foreach ( MethodInfo m in this.GetType( ).GetMethods( BindingFlags.Public | BindingFlags.Instance ) )
            {
                if (!IsClafVisible(m))
                    continue;

                d = this.getDescr( m );

                if ( d != "" )
                    b.AppendFormat( "\t{0}\t\t{1}\n", m.Name, d );
            }

            this.helpString = b.ToString( );
        }

        private bool IsClafVisible(MethodInfo m)
        {
            return (m != null && (m.IsVirtual || (m.IsPublic && !this.GetType().GetMethods().Contains(m) && !typeof(object).GetMethods().Contains(m))));
        }

        ///<summary>Displays usage information</summary>
        ///<remarks>
        ///The usage information is the command name followed by the arguments that can be
        ///given to it. Optional arguments are enclosed in square brackets, for example:
        ///>>> usage help
        ///Would print:
        ///>>> help [func]
        ///</remarks>
        public virtual void usage( string func )
        {
            Console.WriteLine( this.getUsage( func ) );
        }

        ///<summary>Exits the program</summary>
        public virtual void exit()
        {
            this.alive = false;
        }
    }
}
