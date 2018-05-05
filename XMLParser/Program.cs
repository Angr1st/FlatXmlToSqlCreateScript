using System;


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
