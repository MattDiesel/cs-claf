using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Claf;

namespace clafTest
{
    class Program
    {
        static void Main( string[ ] args )
        {
            MyCmdLineProgram m = new MyCmdLineProgram( );

            m.Prompt = ">>> ";
            m.Run( );
        }
    }

    class MyCmdLineProgram : ClafProgram
    {
        [ClafOpt( "This is a test" )]
        public void foo( int x )
        {
            Console.WriteLine( x * 2 );
        }
    }
}
