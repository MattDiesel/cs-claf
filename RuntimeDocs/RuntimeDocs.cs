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
        Type[] T;

        public RuntimeDocs(params Type[] T)
        {
            XDocument d = XDocument.Load(Path.GetFileNameWithoutExtension(T[0].Assembly.Location) + ".xml");

            for (int i = 1; i < T.Length; i++ )
            {
                XDocument d2 = XDocument.Load(Path.GetFileNameWithoutExtension(T[i].Assembly.Location) + ".xml");
                d.Root.Element("members").Add(d2.Root.Element("members").Elements());
            }

            this.T = T;

            this.members = d.Root.Element("members");
        }

        public XElement this[string s]
        {
            get
            {
                string memberId = Utils.GetMemberId( T[0].GetMethod( s ) );

                foreach (XElement x in this.members.Elements("member"))
                    Console.WriteLine(x.Attribute("name"));
         
                return this.members.Elements("member")
                    .Where(e => e.Attribute("name").Value == memberId)
                    .First();
            }
        }
    }

    static class Utils
    {
        public static string GetMemberId( MemberInfo member )
        {
            char memberKindPrefix = GetMemberPrefix( member );
            string memberName = GetMemberFullName( member );
            string memberParams = GetMemberParamsString(member);
            return memberKindPrefix + ":" + memberName + memberParams;
        }

        public static string GetMemberParamsString(MemberInfo member)
        {
            if (member.MemberType != MemberTypes.Method)
                return "";

            if (((MethodInfo)member).GetParameters().Length > 0)
            {
                string ret = "(";

                foreach (ParameterInfo p in ((MethodInfo)member).GetParameters())
                    ret += p.ParameterType.FullName + ", ";

                return ret.Remove(ret.Length - 2) + ")";
            }

            return "";
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
