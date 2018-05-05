using System;
using XMLParser.XML;

namespace XMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlAnalyser xmlAnalyser = new XmlAnalyser();
            xmlAnalyser.Execute(args);
        }
    }
}
