using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Claf
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ClafOpt : Attribute
    {
        private string helpString;

        public string HelpString
        {
            get
            {
                return helpString;
            }
            set
            {
                helpString = value;
            }
        }

        public ClafOpt(string helpstr)
        {
            this.helpString = helpstr;
        }
    }

    public class ClafProgram
    {
        /// <summary>
        /// Internal variable dictating whether we have been asked to close.
        /// </summary>
        private bool alive;

        /// <summary>
        /// Help string cached value that lists all functions.
        /// </summary>
        private string helpString = "";

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
        public string Prompt
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
                this.process( s );
            } while ( this.alive );

            return;
        }

        /// <summary>
        /// Process an input line.
        /// </summary>
        private void process(string s)
        {
            string[ ] parts = s.Split( ' ' );

            MethodInfo m = null;
            ClafOpt a = null;

            try
            {
                m = this.GetType( ).GetMethod( parts[ 0 ] );
                a = ( ClafOpt )Attribute.GetCustomAttribute( m, typeof( ClafOpt ), false );
            }
            catch
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
                    if ( par.Count >= p.Count )
                    {
                        if ( Convert.IsDBNull( t.DefaultValue ) )
                        {
                            throw new TargetParameterCountException( "Too few args." );
                        }

                        par.Add( t.DefaultValue );
                    }
                    else
                    {
                        try
                        {
                            par.Add( Convert.ChangeType( p[ par.Count ], t.ParameterType ) );
                        }
                        catch (FormatException)
                        {
                            throw new ArgumentException( string.Format( "Incorrect parameter type for parameter {0}. Expected a {1} but got '{2}'.", par.Count + 1, t.ParameterType.Name, p[ par.Count ] ) );
                        }
                    }
                }

                if ( par.Count < p.Count )
                {
                    throw new TargetParameterCountException( string.Format("'{0}' takes at most {1} arguments, {2} were given.", m.Name, par.Count, p.Count ) );
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
            try
            {
                ClafOpt a = ( ClafOpt )Attribute.GetCustomAttribute( method, typeof( ClafOpt ), false );

                return a.HelpString;
            }
            catch
            {
                return "";
            }
        }

        private string getHelp(string method)
        {
            MethodInfo m = this.GetType( ).GetMethod( method );

            if (m != null)
                return this.getHelp( m );

            throw new MissingMethodException( this.GetType( ).Name, method );
        }

        private string getHelp(MethodInfo method)
        {
            return string.Format( "{0}\n  {1}\n\n\t{2}", method.Name, this.getDescr( method ), this.getUsage( method ) );
        }

        /// <summary>
        /// Prints the help message, optionally for a specific function.
        /// </summary>
        [ClafOpt( "Displays a help message." )]
        public void help( string func = "" )
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

        private void updateHelpCache()
        {
            string d;
            StringBuilder b = new StringBuilder( );

            foreach ( MethodInfo m in this.GetType( ).GetMethods( BindingFlags.Public | BindingFlags.Instance ) )
            {
                d = this.getDescr( m );

                if ( d != "" )
                    b.AppendFormat( "\t{0}\t\t{1}\n", m.Name, d );
            }

            this.helpString = b.ToString( );
        }

        [ClafOpt( "Displays usage information" )]
        public void usage( string func )
        {
            Console.WriteLine( this.getUsage( func ) );
        }

        [ClafOpt("Exits the program")]
        public void exit()
        {
            this.alive = false;
        }
    }
}
