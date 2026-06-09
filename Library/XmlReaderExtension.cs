using System;
using System.Globalization;
using System.Xml;

namespace Jannesen.Tools.DBTools.Library;

internal static class XmlReaderExtension
{
    public  static      bool            HasChildren(this XmlReader xmlReader)
    {
        return !xmlReader.IsEmptyElement;
    }
    public  static      void            ReadRootNode(this XmlReader xmlReader, string rootName)
    {
        do {
            xmlReader.ReadNext();
        } while (xmlReader.NodeType != XmlNodeType.Element);

        if (xmlReader.Name != rootName)
            throw new XmlReaderException("Invalid XML: Invalid root element expect '" + rootName + "'.");
    }
    public  static      bool            ReadNextElement(this XmlReader xmlReader)
    {
        for(;;) {
            switch(xmlReader.ReadNext()) {
            case XmlNodeType.Element:
                return true;

            case XmlNodeType.EndElement:
                return false;
            }
        }
    }
    public  static      string          ReadContent(this XmlReader xmlReader)
    {
        if (xmlReader.IsEmptyElement)
            throw new XmlReaderException("Invalid XML: missing content in element '" + xmlReader.Name + "'.");

        string      rtn = null;

        for(;;) {
            switch(xmlReader.ReadNext()) {
            case XmlNodeType.Comment:
            case XmlNodeType.Whitespace:
                break;
            case XmlNodeType.CDATA:
            case XmlNodeType.Text:
                if (rtn != null)
                    throw new XmlReaderException("Invalid XML: multiple text-nodes.");

                rtn = xmlReader.Value;
                break;

            case XmlNodeType.EndElement:
                return (rtn != null) ? rtn.Replace("\r\n", "\n", StringComparison.Ordinal) : "";

            default:
                throw new XmlReaderException("Invalid XML: Unexpected node "+xmlReader.NodeType+".");
            }
        }
    }
    public  static      void            SkipCommentWhitespace(this XmlReader xmlReader)
    {
        while (xmlReader.NodeType == XmlNodeType.Comment || xmlReader.NodeType == XmlNodeType.Whitespace) {
            xmlReader.ReadNext();
        }
    }
    public  static      void            NoChildElements(this XmlReader xmlReader)
    {
        if (!xmlReader.IsEmptyElement) {
            if (xmlReader.ReadNextElement())
                throw new XmlReaderException("Invalid XML: element is not empty.");
        }
    }
    public  static      void            UnexpectedElement(this XmlReader xmlReader)
    {
        throw new XmlReaderException("Invalid XML: unexpected element '" + xmlReader.Name + "'.");
    }
    public  static      XmlNodeType     ReadNext(this XmlReader xmlReader)
    {
        if (!xmlReader.Read()) {
            throw new XmlReaderException("Invalid XML: reading EOF.");
        }

        return xmlReader.NodeType;
    }

    public  static      bool            HasValue(this XmlReader xmlReader, string name)
    {
        return xmlReader.GetAttribute(name) != null;
    }
    public  static      string          GetValueString(this XmlReader xmlReader, string name)
    {
        return xmlReader.GetAttribute(name)
                    ?? throw new XmlReaderException("Invalid XML: missing attribute '" + name + "' in element '" + xmlReader.Name + "'.");
    }
    public  static      string          GetValueStringNullable(this XmlReader xmlReader, string name)
    {
        return xmlReader.GetAttribute(name);
    }
    public  static      bool            GetValueBool(this XmlReader xmlReader, string name)
    {
        var value = xmlReader.GetValueString(name);

        switch(value) {
        case "0":
        case "false":
            return false;

        case "1":
        case "true":
            return true;

        default:
            throw new XmlReaderException("Invalid XML: attribute '" + name + "' in element '" + xmlReader.Name + "' has a invalid boolean value '" + value + "'.");
        }
    }
    public  static      bool            GetValueBool(this XmlReader xmlReader, string name, bool defaultValue)
    {
        var value = xmlReader.GetAttribute(name);

        if (value == null)
            return defaultValue;

        switch(value) {
        case "0":
        case "false":
            return false;

        case "1":
        case "true":
            return true;

        default:
            throw new XmlReaderException("Invalid XML: attribute '" + name + "' in element '" + xmlReader.Name + "' has a invalid boolean value '" + value + "'.");
        }
    }
    public  static      int?            GetValueIntNullable(this XmlReader xmlReader, string name)
    {
        var value = xmlReader.GetAttribute(name);

        if (value == null)
            return null;

        try {
            return int.Parse(value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
        catch(Exception) {
            throw new XmlReaderException("Invalid XML: attribute '" + name + "' in element '" + xmlReader.Name + "' has a invalid boolean value '" + value + "'.");
        }
    }
}

internal sealed class XmlReaderException: Exception
{
    public                              XmlReaderException(string message): base(message)
    {
    }
    public                              XmlReaderException(string message, Exception innerException): base(message, innerException)
    {
    }
}
