/****************************************************************************
Copyright (c) 2010-2012 cocos2d-x.org
Copyright (c) 2008-2009 Jason Booth
Copyright (c) 2011-2012 openxlive.com

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Cocos2D;

public enum CCSAXState
{
    None = 0,
    Key,
    Dict,
    Int,
    Real,
    String,
    Array
};
public class CCDictMaker : ICCSAXDelegator
{
    public Dictionary<string, Object>? m_pRootDict;
    public Dictionary<string, Object>? m_pCurDict;
    public Stack<Dictionary<string, Object>> m_tDictStack=new Stack<Dictionary<string,object>>();
    /// <summary>
    /// The parsed key.
    /// </summary>
    public string? m_sCurKey;
    public CCSAXState m_tState;
    public List<Object>? m_pArray;

    Stack<List<Object>> m_tArrayStack = new Stack<List<object>>();
    Stack<CCSAXState> m_tStateStack = new Stack<CCSAXState>();

    public CCDictMaker()
    {
        m_tState = CCSAXState.None;
    }

    public Dictionary<string, Object>? DictionaryWithContentsOfFile(string pFileName)
    {
        CCSAXParser parser = new CCSAXParser();

        if (false == parser.Init("UTF-8"))
        {
            return null;
        }
        parser.SetDelegator(this);

        parser.ParseContentFile(pFileName);
        return m_pRootDict;
    }

    public List<Object>? ArrayWithContentsOfFile(string pFileName)
    {
        CCSAXParser parser = new CCSAXParser();

        if (false == parser.Init("UTF-8"))
        {
            return null;
        }
        parser.SetDelegator(this);

        //byte[] root;

        StartElement(parser, "dict", null);
        StartElement(parser, "key", null);
        TextHandler(parser, System.Text.UTF8Encoding.UTF8.GetBytes("root"), 4);
        EndElement(parser, "key");
        
        parser.ParseContentFile(pFileName);

        EndElement(parser, "dict");

        Debug.Assert(m_pRootDict != null, "The root dictionary was made before parsing.");
        return m_pRootDict["root"] as List<Object>;
    }

    public void StartElement(object ctx, string name, string[]? atts)
    {

        string sName = name;
        if (sName == "dict")
        {
            m_pCurDict = new Dictionary<string, Object>();
            if (m_pRootDict == null)
            {
                m_pRootDict = m_pCurDict;
            }
            m_tState = CCSAXState.Dict;

            CCSAXState preState = CCSAXState.None;
            if (m_tStateStack.Count != 0)
            {
                preState = m_tStateStack.FirstOrDefault();
            }

            if (CCSAXState.Array == preState)
            {
                // add the dictionary into the array
                Debug.Assert(m_pArray != null, "The state is wrong!");
                m_pArray.Add(m_pCurDict);
            }
            else if (CCSAXState.Dict == preState)
            {

                // add the dictionary into the pre dictionary
                Debug.Assert(m_tDictStack.Count > 0, "The state is wrong!");
                Dictionary<string, Object> pPreDict = m_tDictStack.Peek();
                pPreDict.Add(CurrentKey, m_pCurDict);
            }
            //m_pCurDict->autorelease();

            // record the dict state
            m_tStateStack.Push(m_tState);
            m_tDictStack.Push(m_pCurDict);
        }
        else if (sName == "key")
        {
            m_tState = CCSAXState.Key;
        }
        else if (sName == "integer")
        {
            m_tState = CCSAXState.Int;
        }
        else if (sName == "real")
        {
            m_tState = CCSAXState.Real;
        }
        else if (sName == "string")
        {
            m_tState = CCSAXState.String;
        }
        else if (sName == "array")
        {
            m_tState = CCSAXState.Array;
            m_pArray = new List<Object>();

            CCSAXState preState = m_tStateStack.Count == 0 ? CCSAXState.Dict : m_tStateStack.FirstOrDefault();
            if (preState == CCSAXState.Dict)
            {
                Debug.Assert(m_pCurDict != null, "The state is wrong!");
                m_pCurDict.Add(CurrentKey, m_pArray);
            }
            else if (preState == CCSAXState.Array)
            {
                Debug.Assert(m_tArrayStack.Count > 0, "The state is worng!");
                List<Object> pPreArray = m_tArrayStack.Peek();
                pPreArray.Add(m_pArray);
            }
            //m_pArray->release();
            // record the array state
            m_tStateStack.Push(m_tState);
            m_tArrayStack.Push(m_pArray);
        }
        else
        {
            m_tState = CCSAXState.None;
        }
    }

    public void EndElement(object ctx, string name)
    {
        CCSAXState curState = m_tStateStack.Count > 0 ? CCSAXState.Dict : m_tStateStack.FirstOrDefault();
        string sName = name;
        if (sName == "dict")
        {
            m_tStateStack.Pop();
            m_tDictStack.Pop();
            if (m_tDictStack.Count > 0)
            {
                m_pCurDict = m_tDictStack.Peek();
            }
        }
        else if (sName == "array")
        {
            m_tStateStack.Pop();
            m_tArrayStack.Pop();
            if (m_tArrayStack.Count > 0)
            {
                m_pArray = m_tArrayStack.Peek();
            }
        }
        else if (sName == "true")
        {
            string str = "1";
            if (CCSAXState.Array == curState)
            {
                Debug.Assert(m_pArray != null, "The state is wrong!");
                m_pArray.Add(str);
            }
            else if (CCSAXState.Dict == curState)
            {
                Debug.Assert(m_pCurDict != null, "The state is wrong!");
                m_pCurDict.Add(CurrentKey, str);
            }
            //str->release();
        }
        else if (sName == "false")
        {
            string str = "0";
            if (CCSAXState.Array == curState)
            {
                Debug.Assert(m_pArray != null, "The state is wrong!");
                m_pArray.Add(str);
            }
            else if (CCSAXState.Dict == curState)
            {
                Debug.Assert(m_pCurDict != null, "The state is wrong!");
                m_pCurDict.Add(CurrentKey, str);
            }
            //str->release();
        }
        m_tState = CCSAXState.None;
    }

    public void TextHandler(object ctx, byte[] s, int len)
    {
        if (m_tState == CCSAXState.None)
        {
            return;
        }

        CCSAXState curState = m_tStateStack.Count == 0 ? CCSAXState.Dict : m_tStateStack.FirstOrDefault();
        string m_sString = string.Empty;
        m_sString = System.Text.UTF8Encoding.UTF8.GetString(s, 0, len);

        switch (m_tState)
        {
            case CCSAXState.Key:
                m_sCurKey = m_sString;
                break;
            case CCSAXState.Int:
            case CCSAXState.Real:
            case CCSAXState.String:
                Debug.Assert(!string.IsNullOrEmpty(m_sCurKey), "not found key : <integet/real>");

                if (CCSAXState.Array == curState)
                {
                    Debug.Assert(m_pArray != null, "The state is wrong!");
                    m_pArray.Add(m_sString);
                }
                else if (CCSAXState.Dict == curState)
                {
                    Debug.Assert(m_pCurDict != null, "The state is wrong!");
                    m_pCurDict.Add(CurrentKey, m_sString);
                }
                break;
            default:
                break;
        }
        //pText->release();
    }

    // The key that the value being read belongs to. In a well-formed plist, a key comes before
    // each value in a dictionary.
    private string CurrentKey
    {
        get
        {
            Debug.Assert(m_sCurKey != null, "not found key");
            return m_sCurKey;
        }
    }
}
