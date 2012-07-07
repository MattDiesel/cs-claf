using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Xml.Linq;

namespace Claf
{
    public class RuntimeDocs
    {
        XElement members;
        Type T;

        public RuntimeDocs(Type T)
        {
            XDocument d = XDocument.Load( Path.GetFileNameWithoutExtension( T.Assembly.Location ) + ".xml" );

            this.T = T;
            this.members = d.Root.Element( "members" );
        }

        public XElement this[string s]
        {
            get
            {
                string memberId = Utils.GetMemberId( T.GetMethod( s ) );
                return this.members.Elements( "member" )
                  .Where( e => e.Attribute( "name" ).Value == memberId )
                  .First( );
            }
        }
    }

    static class Utils
    {
        public static string GetMemberId( MemberInfo member )
        {
            char memberKindPrefix = GetMemberPrefix( member );
            string memberName = GetMemberFullName( member );
            return memberKindPrefix + ":" + memberName;
        }

        public static char GetMemberPrefix( MemberInfo member )
        {
            return member.GetType( ).Name
              .Replace( "Runtime", "" )[ 0 ];
        }

        public static string GetMemberFullName( MemberInfo member )
        {
            string memberScope = "";
            if ( member.DeclaringType != null )
                memberScope = GetMemberFullName( member.DeclaringType );
            else if ( member is Type )
                memberScope = ( ( Type )member ).Namespace;

            return memberScope + "." + member.Name;
        }
    }
}
