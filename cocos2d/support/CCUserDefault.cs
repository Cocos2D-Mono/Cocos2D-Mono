#nullable disable
/****************************************************************************
Copyright (c) 2010-2011 cocos2d-x.org

http://www.cocos2d-x.org

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/
// root name of xml

using System;
using Cocos2D;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;

// Note: Something to use here http://msdn.microsoft.com/en-us/library/hh582102.aspx

namespace Cocos2D;

public class CCUserDefault 
{

	private static CCUserDefault _userDefault = null;
	private static string USERDEFAULT_ROOT_NAME = "userDefaultRoot";
	private static string XML_FILE_NAME = "UserDefault.xml";

    private static ICCUserDefaultStorage storage;

    /// <summary>
    /// The storage backend the settings file is persisted through. Defaults to a plain
    /// file on desktop and isolated storage elsewhere (the historical behavior). Assign a
    /// replacement BEFORE the first <see cref="SharedUserDefault"/> access to redirect
    /// persistence (e.g. console save-data); combine with
    /// <see cref="PurgeSharedUserDefault"/> when swapping mid-run.
    /// </summary>
    public static ICCUserDefaultStorage Storage
    {
        get
        {
            if (storage == null)
            {
#if WINDOWS || MACOS || LINUX
                storage = new CCFileUserDefaultStorage(XML_FILE_NAME);
#else
                storage = new CCIsolatedStorageUserDefaultStorage(XML_FILE_NAME);
#endif
            }
            return storage;
        }
        set { storage = value; }
    }

    private Dictionary<string, string> values = new Dictionary<string, string>();

    private bool parseXMLFile(Stream xmlFile)
	{
		values.Clear();

		string key = "";

		// Create an XmlReader
		using (XmlReader reader = XmlReader.Create(xmlFile)) {
				// Parse the file and display each of the nodes.
				while (reader.Read()) {
					switch (reader.NodeType) {
						case XmlNodeType.Element:
							key = reader.Name;
							break;
						case XmlNodeType.Text:
							values.Add(key, reader.Value);
							break;
						case XmlNodeType.XmlDeclaration:
						case XmlNodeType.ProcessingInstruction:
							break;
						case XmlNodeType.Comment:
							break;
						case XmlNodeType.EndElement:
							break;
					}
				}
		}
		return true;
	}

	private string getValueForKey(string key)
	{
		string value = null;
		if (! values.TryGetValue(key, out value)) {
			value = null;
		}

		return value;
	}

	private void setValueForKey(string key, string value)
	{
		values[key] = value;
	}

	/**
	 * implements of CCUserDefault
	 */
	private CCUserDefault()
	{
		// only create xml file once if it doesnt exist
		if ((!isXMLFileExist())) {
			createXMLFile();
		}
		using (Stream fileStream = Storage.OpenRead()){
			parseXMLFile(fileStream);
		}
    }

	public static void PurgeSharedUserDefault()
	{
		_userDefault = null;
	}

    public bool GetBoolForKey(string pKey)
    {
        return GetBoolForKey(pKey, false);
    }

	public bool GetBoolForKey(string pKey, bool defaultValue)
	{
		string value = getValueForKey(pKey);
		bool ret = defaultValue;

		if (value != null)
		{
			ret = bool.Parse(value);
		}

		return ret;
	}

    public int GetIntegerForKey(string pKey)
    {
        return GetIntegerForKey(pKey, 0);
    }

	public int GetIntegerForKey(string pKey, int defaultValue)
	{
		string value = getValueForKey(pKey);
		int ret = defaultValue;

		if (value != null)
		{
			ret = CCUtils.CCParseInt(value);
		}

		return ret;
	}

	public float GetFloatForKey(string pKey, float defaultValue)
	{
		float ret = (float)GetDoubleForKey(pKey, (double)defaultValue);
 
		return ret;
	}

	public double GetDoubleForKey(string pKey, double defaultValue)
	{
		string value = getValueForKey(pKey);
		double ret = defaultValue;

		if (value != null)
		{
			ret = double.Parse(value);
		}

		return ret;
	}

	public string GetStringForKey(string pKey, string defaultValue)
	{
		string value = getValueForKey(pKey);
		string ret = defaultValue;

		if (value != null)
		{
			ret = value;
		}

		return ret;
	}

	public void SetBoolForKey(string pKey, bool value)
	{
		// check key
		if (pKey == null) {
			return;
		}

		// save bool value as string
		SetStringForKey(pKey, value.ToString());
	}

	public void SetIntegerForKey(string pKey, int value)
	{
		// check key
		if (pKey == null)
		{
			return;
		}

		// convert to string
		setValueForKey(pKey, value.ToString());
	}

	public void SetFloatForKey(string pKey, float value)
	{
		SetDoubleForKey(pKey, value);
	}

	public void SetDoubleForKey(string pKey, double value)
	{
		// check key
		if (pKey == null)
		{
			return;
		}

		// convert to string
		setValueForKey(pKey, value.ToString());
	}

	public void SetStringForKey(string pKey, string value)
	{
		// check key
		if (pKey == null)
		{
			return;
		}

		// convert to string
		setValueForKey(pKey, value.ToString());
	}

	public static CCUserDefault SharedUserDefault
	{
        get {
    		if (_userDefault == null)
    		{
    			_userDefault = new CCUserDefault();
    		}

    		return _userDefault;
        }
	}

	private bool isXMLFileExist()
	{
		return Storage.Exists();
	}

	// create new xml file
	private void createXMLFile()
	{
		using (StreamWriter writeFile = new StreamWriter(Storage.OpenWrite()))
        {
            string someTextData = "<?xml version=\"1.0\" encoding=\"utf-8\"?><userDefaultRoot>";
            writeFile.WriteLine(someTextData);
            // Do not write anything here. This just creates the temporary xml save file.
            writeFile.WriteLine("</userDefaultRoot>");
        }
	}

	public void Flush()
	{
		using (StreamWriter stream = new StreamWriter(Storage.OpenWrite()))
		{
			//create xml doc
			XmlWriterSettings ws = new XmlWriterSettings();
			ws.Encoding = Encoding.UTF8;
			ws.Indent = true;
			using (XmlWriter writer = XmlWriter.Create(stream, ws)) 
			{
				writer.WriteStartDocument();
				writer.WriteStartElement(USERDEFAULT_ROOT_NAME);
				foreach (KeyValuePair<string, string> pair in values) 
				{
					writer.WriteStartElement(pair.Key);
					writer.WriteString(pair.Value);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}
	}

}
